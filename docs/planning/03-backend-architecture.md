# Backend Architecture and Clean-Code Design

Status: proposed target; no projects or code have been created.

## Architectural choice

Use a modular monolith with domain and application layers independent of ASP.NET, MongoDB, Stripe, and SMTP. Keep one deployable business application initially. Organize features by business capability within layers so that extracting a module later has a defined contract.

Proposed initial solution layout (names are illustrative):

```text
src/
  KorSoft.EShop.Domain/          # Orders/, Catalog/, Pricing/, Inventory/...
  KorSoft.EShop.Application/     # Feature use cases, ports, validation
  KorSoft.EShop.Infrastructure/  # Mongo documents, providers, persistence
  KorSoft.EShop.Api/             # HTTP contracts, endpoints, composition
  KorSoft.EShop.Worker/          # Optional separate host once jobs justify it
tests/
  Domain.Tests/
  Application.Tests/
  Integration.Tests/
  Architecture.Tests/
```

Dependencies: Application references Domain; Infrastructure implements Application ports; Api invokes Application and wires Infrastructure at startup. Domain references no outer project. The optional worker invokes the same application workflows. Do not create dozens of empty projects: split module assemblies when independent ownership or extraction warrants it. Enforce cross-module access through published application contracts, not collections or internal entities.

## Responsibilities

| Layer | Allowed responsibilities | Excluded responsibilities |
| --- | --- | --- |
| HTTP | Binding, authentication/permission metadata, invoking one use case, mapping result/status | Mongo queries, totals, inventory rules, payment transitions, SMTP |
| Application | Orchestration, actor/merchant authorization, validation, transaction boundary, ports | HttpContext, IActionResult, Mongo filters, Stripe SDK objects |
| Domain | Order lifecycle, money and line invariants, reservation rules, immutable facts | BSON attributes, database clients, email, local clock, HTTP |
| Infrastructure | Mongo mapping/queries, provider clients, durable jobs, file storage, migrations | Independent bypass paths around domain transitions |

Use one implementation per actual use case, not one method for every action in a giant `AdminService`. Suggested names include `PlaceOrder`, `CancelOrder`, `ChangeDeliveryDetails`, `StartProduction`, `RecordManualPayment`, `RequestRefund`, `CreateCheckoutAttempt`, `VerifyProviderPayment`, `PublishProduct`, and `GetOrderDetails`.

## First extraction slice

Extract order lifecycle before cosmetic folder changes:

1. Characterize current HTTP request/response behavior with tests, explicitly identifying unsafe state transitions that must change.
2. Introduce `CancelOrder` and named transition policies with an explicit actor (customer, staff, provider callback) and merchant scope.
3. Route guest, customer, and admin cancellation through the same policy; actor permissions differ but inventory invariants do not.
4. Move Mongo operations into a purpose-specific order store and inventory boundary.
5. Make every transition conditional on expected version/state and report conflicts consistently.
6. Remove direct order-state assignment, enum increment/decrement, and generic hard-delete from normal HTTP operations.

Keep legacy routes as adapters temporarily. Do not preserve an unsafe behavior just to keep an old test green. Record the intended contract change and frontend migration.

## Domain and persistence design

- `Order` contains an immutable merchant/customer/address snapshot and immutable accepted lines: product/variant identity, display description, quantity, currency, unit amount, tax/discount policy outcome, total, and personalization snapshot if enabled.
- Define separate order, payment, and fulfillment states. Use named transitions, allowed actor, preconditions, side effects, reason, and expected version; enum ordering never drives behavior.
- Define a `Money` value with currency and rounding rules. Keep the first release explicitly EUR-only if that is the approved scope; do not claim multi-currency by adding a string field.
- Use stable variant IDs instead of color/size strings as storage identities. Preserve original display labels on order lines.
- Use a stable internal order ID and a separate unique merchant-scoped display number allocated atomically.
- Infrastructure maps storage documents to domain/application types. Never return documents directly from APIs. Existing order responses may still expose stored token hashes and must receive explicit output DTOs.
- Prefer purpose-specific ports such as `IOrderStore`, `ICatalogReader`, `IInventoryReservations`, `IPaymentProvider`, `IMediaStorage`, and `INotificationSender`. Do not expose `IQueryable`, Mongo filter definitions, or a universal CRUD repository to the application.
- Query endpoints use bounded pagination, stable sorting, allowed filters, projections, and documented index requirements. Report counts/aggregates without loading all records into application memory.

## Transactions and external side effects

For order acceptance, atomically persist the accepted order, inventory reservation, deduplicated request key, and durable work records where possible. MongoDB supports multi-document transactions on replica sets/sharded clusters; this requires deployment and integration-fixture support. See [MongoDB transactions](https://www.mongodb.com/docs/manual/core/transactions/).

Keep Stripe calls and email outside the database transaction. A transaction cannot roll back an external payment:

1. Persist intent and an idempotency key with the order/payment attempt.
2. Execute the provider operation using that stable key.
3. Save the result conditionally; retry or reconcile if the response or save is lost.
4. Receive signed provider events into an inbox with a unique provider/account/event key.
5. Apply the business transition and mark the event processed atomically; queue notifications in an outbox in that same commit.

Outbox delivery is at-least-once: recipients/handlers must deduplicate, jobs must have leases/retry limits, and operators need dead-letter visibility. No promise of global exactly-once execution.

Payment attempts require their own identity, amount/currency, provider account, session reference, and state. Concurrent checkout requests must not overwrite an earlier payable attempt silently. Late successful payments after cancellation enter a reconciliation/refund workflow. Refunding and restoring stock are separate explicit decisions. Define reservation expiry and abandoned checkout recovery.

## Validation, errors, identity, and time

- Retain simple DTO DataAnnotations at the HTTP boundary; use explicit asynchronous application validation for complex cross-field rules. Domain constraints and unique indexes remain authoritative under concurrency.
- Use ASP.NET ProblemDetails for safe HTTP errors with stable error codes and correlation IDs. Do not pass provider errors or stack traces directly to clients.
- Pass cancellation tokens through async I/O. Avoid synchronous database calls and catch-all success responses.
- Use .NET `TimeProvider` for time-sensitive use cases and UTC storage. Convert to merchant timezone only for display/reporting; decide how legacy local timestamps will be migrated without guessing.
- Reassess identity using supported account/session infrastructure, staff MFA and permissions. A staff identity is not simply a customer with one boolean flag. Preserve existing hash migration and revocation until replacement is verified.
- Separate permissions such as catalog editing, order fulfillment, refunds, staff administration, and configuration management. Require reason/audit for sensitive mutations.

## Operational boundaries

- Replace per-start full-data migration with versioned explicit deployment migrations: preflight, lock/lease, checkpoints, bounded batches, metrics, compatibility checks, and recovery instructions.
- Use an explicit schema/hash version rather than inferring token encoding from length.
- Validate typed configuration at startup and distinguish readiness (dependencies/migrations ready) from liveness (process healthy).
- Record actor, merchant, action, entity, timestamp, result, and correlation in audit events; omit credentials, capability tokens, and unnecessary PII.
- Use request-scoped merchant provider clients; process-global Stripe credentials cannot support shared-process tenancy.
- Object storage adapters need durable storage, approved content types, authorization, quotas, and lifecycle policy. Image decoding/re-encoding and size/pixel limits are separate from magic-byte checks.

## Database decision

Keep MongoDB during the first architectural extraction. Run a bounded comparison against PostgreSQL only if order consistency, reporting, and relational constraints justify a migration. Compare transaction support, operations, reporting queries, data-volume assumptions, backup/restore, migrations, identity integration, and total maintenance cost. Do not change the database and public contracts in the same uncontrolled rewrite.

## Clean-code review rules

- A controller should be readable as bind -> invoke -> map. Do not use a rigid line limit as the definition of quality.
- Prefer explicit constructors and dependency injection over static service access or service locators.
- Do not add mediator, mapper, generic repository, or event-bus frameworks unless a measured use case justifies their cost.
- Keep business names in English in new code; preserve serialized legacy names through explicit compatibility mapping until migrated.
- Limit shared helpers to stable cross-cutting primitives; duplicate a small mapper rather than coupling unrelated modules.
- Architecture tests prohibit outer-layer references from Domain and raw persistence usage from HTTP handlers.
- Tests exercise permission, concurrency, idempotency, lifecycle, failure/recovery, and contract outcomes, not just implementation-shaped helper assertions.
