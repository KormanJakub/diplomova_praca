# Architecture Audit for KorSoft ESHOP

Date: 2026-09-15. Method: static review of the current working tree, including earlier uncommitted hardening changes. This is not a production penetration test, performance benchmark, or commercial-readiness certification.

## Executive assessment

The application is a useful single-store starting point. It has controller authorization, shared order logic, database-backed session checks, token hashing, and basic input protections. It is not yet a reusable commerce platform: transport, persistence, policy, branding, and integrations remain coupled, with incomplete lifecycle and integration tests.

The recommended next step is a modular monolith with thin controllers and two frontend applications. Microservices would add deployment and consistency costs before module boundaries have been established. The dependency direction follows the principles in [Microsoft's application architecture guidance](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures); the concrete module design is a project-specific recommendation.

## Evidence register

Paths are repository-relative. Line numbers are snapshots and will drift. Find the named class or method when reviewing later.

| ID / priority | Observed evidence | Consequence / proposed action |
| --- | --- | --- |
| A01 / High | `api/nia_api/Controllers/AdminController.cs` is 901 lines; it handles catalog, inventory, orders, KPI, users, and settings using Mongo collections directly. `PublicController` is 515 lines. | Changes have many unrelated dependencies. Split endpoints by business capability and extract application use cases with explicit ports. |
| A02 / High | `UserController.Custom` and `GuestUserController.MakeCustomizationWithoutRegister` repeat product/design lookups, variant validation, and pricing. | Rules can drift between guest and registered checkout. Share a pricing/customization application service with an explicit customer context. |
| A03 / Critical release blocker | `AdminController.UpdateOrder` directly assigns `StatusOrder` (around line 638); increase/decrease use enum arithmetic and document replacement; removal directly deletes an order. | An authorized administrator can bypass the service's lifecycle checks and inventory effects. Use named transition commands, optimistic concurrency, reasoned audit entries, and cancellation/refund rules. DTO allowlisting alone does not solve this. |
| A04 / High | `OrderService.CreateAsync` claims customizations, decrements stock, inserts an order, and compensates selected failures in separate operations. Cancellation sets the order state before restoring stock. | Process/database failure can strand claims or inventory; not every thrown exception is compensated. Define an atomic transaction boundary plus durable recovery. |
| A05 / High | `PaymentService` creates `SessionService` directly; `Program.cs` sets process-global `StripeConfiguration.ApiKey`; the order retains only one `PaymentId`. | Multiple concurrent sessions can overwrite the recorded payment attempt. Shared-process customer credentials would be unsafe. Use scoped provider clients, payment attempts, idempotency, reconciliation, and a durable webhook inbox. |
| A06 / High | `Order` references live customizations; detail endpoints re-read current catalog and customer documents. | Historical orders can change when catalog/profile data changes. Capture immutable line, address, monetary, tax, delivery, and merchant snapshots. |
| A07 / High | `StoreSettings.Id` is always `store_settings`; DB context has no tenant scope; branding, EUR, personalization surcharge, and a placeholder bank account are embedded in source/templates. | The code assumes one merchant. Start with isolated installations and validated merchant configuration; do not simulate shared tenancy by adding a header. |
| A08 / High | `web/src/app/app.routes.ts` statically imports storefront and admin into one application. `web/firebase.json` has one SPA destination. | A DNS alias alone still serves the same application. Create independent entry points, build artifacts, deployments, and allowed API surfaces. |
| A09 / High | `Program.cs` uses one cookie and one allowed web origin. Its origin check runs only for authenticated unsafe requests with a present Origin header. | Adding an admin origin does not establish staff-session isolation or complete CSRF protection. Design separate sessions and framework antiforgery, including login/logout. |
| A10 / High | `SecurityDataInitializer.StartAsync` loads all users and orders and performs migration work at every startup. `CapabilityToken.IsHash` checks only length/hex format. | Startup cost grows with data and multiple replicas can overlap migration. Raw new tokens and hashes have the same shape, so format is not a general migration marker. Introduce schema versions, migration checkpoints/locking, bounded batches, and preflight duplicate checks. |
| A11 / Medium | Models contain BSON attributes and use `LocalTimeService`; controllers use `DateTime.Now` alongside UTC. | Domain and storage are coupled; expiration and historic timestamps are difficult to test. Use UTC/time abstraction and infrastructure document mappings. |
| A12 / High | Admin list/KPI endpoints use broad `ToListAsync`; many mutations use read-modify-`ReplaceOneAsync`. | Catalog size increases memory/query costs; stale replacements can overwrite concurrent stock/payment edits. Add bounded pagination/projections and conditional field updates. |
| A13 / Medium | `EmailSenderService` contains duplicated inline HTML and synchronous workflow dependence on SMTP; `FileController` writes into local `wwwroot/Files`. | Branding and infrastructure are difficult to swap. Introduce notification templates/durable jobs and a storage adapter. |
| A14 / High | `UserControllerIntegrationTests.Test1` is empty and imports the test-platform `Program`; public-controller tests contain old token and account-enumeration expectations. `OrderServiceTests` mostly cover early validation exits. | The previously reported 14 passing tests do not demonstrate HTTP middleware, transactions, cross-customer isolation, or payment correctness. Rebuild integration fixtures against the actual application host. |
| A15 / Medium | `AdminMiddleware` is unregistered and uses an obsolete claim; the frontend JWT decoder and `jwt-decode` remain; `crypto-js` has no source reference in the inspected `web/src`. | Dead paths and unnecessary dependencies confuse future work. Verify imports, providers, tests, and generated assets before removal. |
| A16 / High | `nia_api.csproj` targets .NET 8. | Commercial support needs a runtime-upgrade plan. Microsoft lists .NET 8 support ending 2026-11-10; assess .NET 10 LTS before launch. See the [official support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). |

## Important qualifications

- Admin role protection exists. A03 concerns authorized operations violating domain rules, not an anonymous admin bypass.
- Hashing/HttpOnly controls reduce token exposure; neither prevents a malicious same-origin script from performing permitted actions.
- Customer isolation, reusable packages, separate admin deployment, complete refunds, and contractual entitlements are proposals, not current features.
- Existing file assets, fonts, designs, logos, and historical source contributions need an ownership/license inventory before redistribution. No redistribution clearance was established by this audit.
- The inspected checkout bank value is a placeholder, not a verified merchant payout account. Do not copy it into a customer installation.

## Audit-to-plan mapping

- A01–A06, A10–A14: [backend design](03-backend-architecture.md) and roadmap E01–E05.
- A07: [product strategy](02-product-and-reuse.md), E06.
- A08–A09: [domain separation](04-admin-and-domains.md), E07–E08.
- A11, A13, A15–A16: [library assessment](05-libraries-and-platform.md), E02/E09.

Before a commercial pilot, demonstrate that all order-changing routes use one lifecycle policy, inventory survives retries/failures, staff access is isolated, and a second customer can be configured without a source fork.
