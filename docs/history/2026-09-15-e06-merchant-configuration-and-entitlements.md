# 2026-09-15: E06 - Merchant Configuration, Optional Personalization, and Entitlement Policy

## Summary
Milestone **E06** from `docs/planning/06-delivery-roadmap.md` has been implemented and verified. The KorSoft ESHOP platform now provides robust multi-merchant configuration and entitlement enforcement from a single codebase and shared build artifacts without source forks:

1. **Two Distinct Merchant Personas from Identical Artifacts**:
   - **Clothing Merchant (e.g. WAFFL)**: Personalization enabled (designs, paired designs, personalization text, surcharges), Stripe + Dobierka (COD) + Packeta pickup points enabled.
   - **Plain Catalog Merchant (e.g. Nordic Hardware)**: Personalization disabled, Bank Transfer (wire/IBAN) + Home Delivery enabled, Stripe & Packeta disabled.
2. **Fail-Closed Payment Method Policy**:
   - If an unconfigured or disabled payment method is requested (e.g., selecting `IBAN` when `BankAccountIban` is unconfigured, or selecting Stripe when disabled or missing credentials), the order creation request is strictly rejected (`OrderCreationError.InvalidItems`).
3. **Fail-Closed Delivery Carrier Policy**:
   - If an unconfigured or disabled carrier is requested (e.g., selecting `Packeta` when `PacketaApiKey` is null/empty or Packeta is disabled), the order creation request is strictly rejected (`OrderCreationError.InvalidItems`).
4. **Server-Side Entitlement Policy Enforcement (Forbidden on Disabled Modules)**:
   - When `EnablePersonalization == false`, direct access to design queries and mutations (e.g., `GET /public/all-designs`, `GET /admin/design/getAll`, `POST /admin/design/create`, `PUT /admin/design/update`, etc.) returns `403 Forbidden`.
   - Customer and guest cart creation requests specifying `DesignId` or `UserDescription` are strictly rejected with `403 Forbidden` when personalization is disabled.
5. **Safe Public Store Profile Separation**:
   - `GET /public/store-settings` returns safe store metadata (`StoreName`, `ContactEmail`, currency, active module flags, public bank account IBAN, and public Packeta widget API key) while keeping all private server secrets (e.g. Stripe secret keys, database credentials) unexposed.
   - Backward compatibility is preserved for legacy checkout components through `CashOnDeliveryFee`, `PacketaApiKey`, and `UpdatedAt` properties on the profile payload.
6. **Flexible Catalog Items (Designs Optional)**:
   - `CustomizationRequest.DesignId` is now optional, allowing stores without personalization (or clothing customers buying plain garments) to order items without requiring mock or dummy designs.

---

## Architecture & Implementation

### 1. Domain Configuration
- `nia_api.Domain.Configuration.MerchantConfiguration`: Models store profile, module entitlements (`EnablePersonalization`, `EnableReviews`, `EnableCoupons`, `EnableAdvancedReporting`), payment methods (`EnableStripe`, `EnableCashOnDelivery`, `CashOnDeliveryFee`, `EnableBankTransfer`, `BankAccountIban`, `BankAccountBic`, `BankTransferInstructions`), delivery methods (`EnableHomeDelivery`, `HomeDeliveryFee`, `EnablePacketa`, `PacketaApiKey`, `PacketaFee`), and merchant tax/operational settings.
- `nia_api.Domain.Configuration.PublicStoreProfileResponse`: DTO mapping public configuration for storefront consumption without leaking secrets.
- `nia_api.Domain.Configuration.IMerchantConfigurationService` & `MerchantConfigurationService`: Core service handling thread-safe retrieval, public profile projection, updates, and fail-closed validation (`ValidatePaymentMethodAllowedAsync`, `ValidateDeliveryMethodAllowedAsync`, `IsPersonalizationEnabledAsync`).

### 2. Domain & Application Services Integration
- `nia_api.Data.NiaDbContext`: Added `MerchantSettings` collection property mapped to MongoDB `StoreSettings`.
- `nia_api.Services.OrderService`: Injected `IMerchantConfigurationService` (with backward-compatible constructor fallback). Validates payment method, delivery method, and personalization entitlements prior to claiming customizations or reserving stock. Computes dynamic payment and delivery fees according to active merchant rules.

### 3. Controller Hardening & Entitlement Checks
- `nia_api.Controllers.PublicController`:
  - `GET /public/store-settings`: Returns `PublicStoreProfileResponse`.
  - `GET /public/all-designs`: Returns `403 Forbidden` if personalization is disabled.
- `nia_api.Controllers.AdminController`:
  - Enforces `403 Forbidden` on all design query and mutation endpoints when personalization is disabled.
  - `GET /admin/settings`: Returns full `MerchantConfiguration`.
  - `PUT /admin/settings`: Updates arbitrary merchant settings via `UpdateSettingsRequest` with validation (rejecting negative fees).
- `nia_api.Controllers.UserController` & `GuestUserController`:
  - `POST /user/make-customization` & `POST /guest/make-customization-without-register`: Return `403 Forbidden` if personalization is disabled and request contains design or custom text. Supports buying plain products without designs.

---

## Affected Files and Contracts
- `api/nia_api/Domain/Configuration/MerchantConfiguration.cs` [NEW]
- `api/nia_api/Domain/Configuration/IMerchantConfigurationService.cs` [NEW]
- `api/nia_api/Domain/Configuration/MerchantConfigurationService.cs` [NEW]
- `api/nia_api/Data/NiaDbContext.cs` [MODIFIED]
- `api/nia_api/Program.cs` [MODIFIED]
- `api/nia_api/Models/Order.cs` [MODIFIED]
- `api/nia_api/Services/OrderService.cs` [MODIFIED]
- `api/nia_api/Requests/CustomizationRequest.cs` [MODIFIED]
- `api/nia_api/Controllers/PublicController.cs` [MODIFIED]
- `api/nia_api/Controllers/AdminController.cs` [MODIFIED]
- `api/nia_api/Controllers/UserController.cs` [MODIFIED]
- `api/nia_api/Controllers/GuestUserController.cs` [MODIFIED]
- `api/nia_api.Tests/MerchantConfigurationIntegrationTests.cs` [NEW]
- `docs/planning/06-delivery-roadmap.md` [MODIFIED]

---

## Verification Performed
1. **Full Automated Test Suite**:
   - `dotnet test api/nia_api.Tests/nia_api.Tests.csproj`: **115/115 tests passed** in 2 seconds (0 failures).
   - All 8 new integration tests verified:
     - `StoreProfile_PublicEndpoint_ReturnsSafeBrandingWithoutSecrets`: Correct branding and safe credentials returned.
     - `Unconfigured_BankTransfer_FailsClosed`: Fails closed when IBAN is missing.
     - `Configured_BankTransfer_Succeeds`: Order placed with IBAN settlement.
     - `Unconfigured_Packeta_FailsClosed`: Fails closed when Packeta API key is missing.
     - `Disabled_Personalization_Blocks_Design_Queries`: Public and admin endpoints return `403 Forbidden`.
     - `Disabled_Personalization_Blocks_Customization_Creation`: Customization attempts with designs return `403 Forbidden`.
     - `Plain_Catalog_Merchant_Checkout_Succeeds_Without_Personalization`: Complete guest checkout lifecycle with plain hardware item, IBAN, and home delivery without designs.
     - `Admin_Settings_Update_Persists_And_Enforces_New_Policies`: Dynamic updates to merchant policies immediately take effect and enforce fail-closed rules.
2. **Frontend Compilation**:
   - `npm run build` in `web/`: **Build succeeded with 0 errors**.
