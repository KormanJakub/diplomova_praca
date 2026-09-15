# 2026-09-15: E04 - Inventory Reservations, Atomic Numbering, Idempotency, and Immutable Snapshots

## Summary
Milestone **E04** from `docs/planning/06-delivery-roadmap.md` has been implemented and verified. The KorSoft ESHOP order acceptance pipeline now guarantees:
1. **Atomic order sequence and human-readable order numbering** (`ORD-YYYY-XXXXX`) via a dedicated MongoDB atomic counter collection with thread-safe initialization.
2. **Immutable embedded snapshots** (`OrderLineSnapshot`, `OrderCustomerSnapshot`, `OrderDeliverySnapshot`, `OrderPricingSnapshot`) capturing accepted prices, item names, descriptions, color file paths, and buyer details upon checkout.
3. **Snapshot-first detail queries** across `AdminController`, `UserController`, and `GuestUserController`, rendering historical order data immune to catalog edits, price adjustments, or design deletions, while maintaining backward-compatible fallback for legacy records.
4. **Resilient multi-item inventory reservation** with automated compensation (rollback) if partial out-of-stock or mid-flight insertion errors occur.
5. **Idempotent checkout handling** via `Idempotency-Key` preventing duplicate charges and double-decremented inventory.

## Motivation
Previously:
- Order IDs were computed via `(last?.Id ?? 0) + 1` with an optimistic retry loop that was prone to race conditions and lock contention.
- Order details dynamically queried live `Products`, `Designs`, and `Customizations` collections, meaning if a merchant updated a price or deleted an archived design, historical orders retroactively altered or crashed with `NullReferenceException` / missing assets.
- If an order with multiple items encountered an out-of-stock item after decrementing prior items, stock was leaked without rollback.
- Duplicate checkout submissions (e.g. user double-clicking "Pay") caused duplicate inventory deductions and multiple created orders.

## Implementation
1. **Domain Models & Snapshots**:
   - `nia_api.Domain.Orders.OrderSnapshots`: Defines `OrderLineSnapshot`, `OrderCustomerSnapshot`, `OrderDeliverySnapshot`, `OrderPricingSnapshot`.
   - `nia_api.Models.Order`: Added `OrderNumber`, `Lines`, `CustomerSnapshot`, `DeliverySnapshot`, `PricingSnapshot`, and `IdempotencyKey` annotated with `[BsonIgnoreIfNull]`.
2. **Sequence Store**:
   - `IOrderSequenceStore` and `MongoOrderSequenceStore`: Employs MongoDB `FindOneAndUpdate` with `$inc` on `Counters` collection.
   - Initialized `order_id` safely against existing orders (`Math.Max(highestOrder?.Id ?? 100000, 100000)`).
   - Generates display order numbers formatted as `ORD-{year}-{seq:D5}`.
3. **Order Lifecycle & Reservation**:
   - Updated `OrderService.CreateAsync` with transactional compensation for multi-item checkouts.
   - Added idempotency key lookup before reservation to return existing orders without duplicate reservations.
   - Captures comprehensive line, customer, delivery, and pricing snapshots directly into the order.
4. **Snapshot Presentation**:
   - Created `OrderSnapshotPresentation` helper to materialize legacy DTO shapes (`Customizations`, `Products`, `Designs`, `User`) directly from embedded snapshots.
   - Updated `AdminController.GetOrderInformation`, `UserController.GetOrdersById`, `GuestUserController.ConfirmPayment` (`follow-order`), and `GuestUserController.OrderInformationById` to serve snapshot data first and fall back to live collection queries for historical records.

## Affected files and contracts
- `api/nia_api/Domain/Orders/OrderSnapshots.cs` [NEW]
- `api/nia_api/Domain/Orders/IOrderSequenceStore.cs` [NEW]
- `api/nia_api/Domain/Orders/MongoOrderSequenceStore.cs` [NEW]
- `api/nia_api/Domain/Orders/OrderSnapshotPresentation.cs` [NEW]
- `api/nia_api/Models/Order.cs` [MODIFIED]
- `api/nia_api/Data/NiaDbContext.cs` [MODIFIED]
- `api/nia_api/Services/OrderService.cs` [MODIFIED]
- `api/nia_api/Controllers/AdminController.cs` [MODIFIED]
- `api/nia_api/Controllers/UserController.cs` [MODIFIED]
- `api/nia_api/Controllers/GuestUserController.cs` [MODIFIED]
- `api/nia_api/Requests/GuestOrderRequest.cs` [MODIFIED]
- `api/nia_api/Program.cs` [MODIFIED]
- `api/nia_api.Tests/OrderAcceptanceAndSnapshotTests.cs` [NEW]
- `docs/planning/06-delivery-roadmap.md` [MODIFIED]

## Data and deployment impact
- **Zero breaking database changes**: All new snapshot fields on `Order` use `[BsonIgnoreIfNull]`, allowing seamless coexistence of existing and new documents.
- **Frontend compatibility preserved**: Response envelopes for `{ order, customizations, products, designs, user }` are identical to legacy shapes, while being backed by immutable snapshot data.
- **Zero external side-effects**: Stripe and email remain outside atomic database updates.

## Verification performed
- `dotnet test api/nia_api.Tests/nia_api.Tests.csproj` executed: **100/100 tests passed** (0 failures).
- Verified concurrent inventory checkout race conditions: 2 checkouts competing for 1 unit allowed exactly 1 order, failed 1 with `OutOfStock`, and left stock at exactly 0.
- Verified compensation: multi-item order failing on item 2 restored item 1 stock and released customizations.
- Verified idempotency: replaying checkout with identical `Idempotency-Key` returned existing order without double-decrementing inventory.
- Verified catalog immunity: mutated product prices and deleted design in MongoDB; order detail endpoint retained original prices, product name, design name, and images.
- Verified legacy fallback: orders without snapshots continue to query legacy collections smoothly.
- Verified frontend build: `npm run build` completed with 0 errors.

## Remaining risks or follow-up
- Milestone **E05** will build upon this foundation to introduce payment attempts, webhook inbox/outbox, and provider reconciliation.
