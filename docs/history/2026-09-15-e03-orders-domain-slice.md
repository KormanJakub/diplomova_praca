# E03: Orders Application and Domain Slice Extraction — 2026-09-15

## Summary

Implemented milestone **E03**: extracted an isolated Orders application and domain slice with named lifecycle policies, replaced direct MongoDB collection access in controllers with dedicated store and lifecycle services, abolished hard order deletion in favor of cancellation, and added comprehensive unit and architectural dependency tests.

## Motivation

Audit items A01, A02, A03, A04, and A05 identified:
- Direct arithmetic transitions (`order.StatusOrder++` / `--`) without named lifecycle guards or payment verification allowed unverified state changes.
- Terminal states (e.g. `ZRUSENA`) could be bypassed or mutated.
- Uncontrolled hard deletion (`DELETE /admin/orders/{id}`) erased audit trails and financial history.
- API controllers (`AdminController`, `UserController`, `GuestUserController`) leaked direct `IMongoCollection<Order>` access and mixed domain logic with HTTP transport concerns.

## Implementation

1. **Domain Models and Policies (`api/nia_api/Domain/Orders/`)**:
   - `OrderActor`: strongly-typed actor abstraction (`Customer`, `Guest`, `Staff`, `SystemCallback`).
   - `OrderLifecyclePolicy`: pure domain rules governing:
     - Strict allowed transition sequences (`PRIJATA` -> `ZAPLATENA` / `VO_VYROBE` -> `PRIPRAVENA` -> `POSLANA`).
     - Payment gate enforcement: production (`VO_VYROBE`) strictly requires `PaymentStatus == "Paid"` or `PaymentMethod == "Dobierka"`.
     - Terminal state enforcement: orders in `ZRUSENA` cannot be mutated or cancelled again.
     - Role-based cancellation permissions (customer/guest can only cancel orders in `PRIJATA`; staff can cancel in `PRIJATA`, `ZAPLATENA`, `VO_VYROBE`).
     - `CanHardDelete(order) => false`: hard deletion abolished.
   - `OrderMutationResult`: structured mutation results with `OrderMutationStatus` (`Success`, `NotFound`, `Forbidden`, `Conflict`, `InvalidData`).
   - `OrderQueryModels`: record contracts for order summaries and KPI data.

2. **Persistence Abstraction**:
   - `IOrderStore`: clean read/write interface for orders queries, conditional status transitions, delivery detail updates, and aggregations (sales summary, KPI metrics).
   - `MongoOrderStore`: MongoDB implementation of `IOrderStore`.

3. **Lifecycle Orchestration**:
   - `IOrderLifecycleService` and `OrderLifecycleService`: orchestrates lifecycle state transitions, operational status stepping, actor authorization, cancellation, and payment confirmation.
   - Registered in DI container (`Program.cs`).

4. **Thin Controller Refactoring**:
   - `AdminController`: removed `IMongoCollection<Order> _orders`; all order queries and mutations routed through `IOrderStore` and `IOrderLifecycleService`. `DELETE /admin/orders/{orderId}` permanently rejects hard deletion.
   - `UserController`: removed `_orders`; orders retrieval and cancellation routed through `IOrderStore` and `IOrderLifecycleService`.
   - `GuestUserController`: removed `_orders`; guest order follow-up and token cancellations routed through `IOrderStore` and `IOrderLifecycleService`. Unreferenced dead `[NonAction]` methods removed.

5. **Exhaustive Testing**:
   - `OrderLifecyclePolicyTests.cs`: 23 unit tests verifying the full transition matrix, payment gates, role permissions, step-back operational transitions, and terminal state invariants.
   - `OrderArchitectureTests.cs`: architectural reflection tests asserting that `AdminController`, `UserController`, `GuestUserController`, and `PublicController` contain 0 fields, properties, or constructor parameters of type `IMongoCollection<Order>`.

## Affected files and contracts

- `api/nia_api/Program.cs`
- `api/nia_api/Controllers/AdminController.cs`
- `api/nia_api/Controllers/UserController.cs`
- `api/nia_api/Controllers/GuestUserController.cs`
- `api/nia_api/Requests/AdminUpdateOrderRequest.cs` (new)
- `api/nia_api/Domain/Orders/OrderActor.cs` (new)
- `api/nia_api/Domain/Orders/OrderLifecyclePolicy.cs` (new)
- `api/nia_api/Domain/Orders/OrderMutationResult.cs` (new)
- `api/nia_api/Domain/Orders/OrderQueryModels.cs` (new)
- `api/nia_api/Domain/Orders/IOrderStore.cs` (new)
- `api/nia_api/Domain/Orders/MongoOrderStore.cs` (new)
- `api/nia_api/Domain/Orders/IOrderLifecycleService.cs` (new)
- `api/nia_api/Domain/Orders/OrderLifecycleService.cs` (new)
- `api/nia_api.Tests/OrderLifecyclePolicyTests.cs` (new)
- `api/nia_api.Tests/OrderArchitectureTests.cs` (new)
- `docs/planning/06-delivery-roadmap.md`

## Data and deployment impact

No schema migration required; backward compatibility with existing MongoDB orders collection preserved. Legacy HTTP payloads and response contracts maintained for storefront and admin frontends.

## Verification performed

1. Backend: `dotnet test api/nia_api.Tests/nia_api.Tests.csproj` — **94 passed, 0 failed** in 2 seconds.
2. Architecture: `OrderArchitectureTests` verified 0 direct `IMongoCollection<Order>` references in API controllers.
3. Frontend: `npm run build` in `web/` — application bundle generation completed with **0 errors**.

## Remaining risks or follow-up

- Next milestone (**E04**) will implement inventory reservations, atomic order acceptance, serial order numbering, and immutable product/design snapshots at checkout.
