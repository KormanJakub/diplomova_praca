# 2026-09-15: E05 - Payment Attempts, Provider Adapters, Webhook Inbox, and Reconciliation

## Summary
Milestone **E05** from `docs/planning/06-delivery-roadmap.md` has been implemented and verified. The KorSoft ESHOP payment pipeline now guarantees:
1. **Decoupled Payment Gateway Abstraction (`IPaymentGateway`)**: Production uses `StripePaymentGateway` (wrapping the official `Stripe.net` SDK); automated tests use an in-memory mock/adapter, removing all requirements for live Stripe secret keys or network calls in integration tests.
2. **Multi-Attempt Payment Ledger (`PaymentAttempt`)**: Each checkout session creates an immutable ledger document (`OrderId`, `AttemptNumber`, `Provider`, `Amount`, `Status`, `SessionUrl`, `ExternalReferenceId`, `PaymentIntentId`, `IdempotencyKey`, `AuditNotes`), preventing data loss or overwrites when users initiate multiple checkouts.
3. **Durable Webhook Inbox (`WebhookInboxMessage`)**: Ingested webhook events are deduplicated by `EventId` (e.g. `evt_...`), ensuring idempotent processing and resilience against duplicate, retried, or out-of-order deliveries.
4. **Reconciliation for Late Payments on Cancelled Orders**: If an order was cancelled (`ZRUSENA`) but the customer subsequently paid an open Stripe session, the order is **not** resurrected (protecting stock from unauthorized claims); instead, the payment attempt transitions to `RefundNeeded` / `Refunded`, automated or staff refund is initiated, and an auditable trail is recorded.
5. **Duplicate Payment Reconciliation**: If concurrent checkout sessions both complete, the first session marks the order paid, and the second session automatically triggers a refund with `PaymentAttemptStatus.Refunded` and an audit note.
6. **Auditable Staff Refund Endpoint**: `POST /payment/refund/{orderId}` allows staff to issue partial or full refunds through the gateway adapter, updating the payment attempt ledger and transitioning the order to `ZRUSENA` on full refund.

## Motivation
Previously:
- `PaymentService` directly instantiated `new SessionService()`, making external Stripe network calls impossible to test cleanly or isolate in test environments.
- The `Order` document only maintained a single `PaymentId` string. Multiple clicks or concurrent checkout sessions silently overwrote this ID, causing race conditions and untracked payments.
- When an order was cancelled before Stripe payment succeeded, a late payment verification would either fail or leave the customer charged without fulfilling their order or recording the need for a refund.
- Webhooks were not persisted, risking duplicate processing on retries or lost state during service interruptions.

## Implementation
1. **Domain & Persistence**:
   - `nia_api.Domain.Payments.PaymentAttempt`: Models payment attempts and lifecycle states (`Initiated`, `Pending`, `Paid`, `Failed`, `Cancelled`, `RefundNeeded`, `Refunded`).
   - `nia_api.Domain.Payments.WebhookInboxMessage`: Models durable webhook messages and states (`Received`, `Processing`, `Processed`, `Failed`, `Ignored`).
   - `nia_api.Data.NiaDbContext`: Added `PaymentAttempts` and `WebhookInbox` collections.
2. **Provider Adapter**:
   - `nia_api.Domain.Payments.IPaymentGateway`: Defines contracts for session creation, verification, and refunds.
   - `nia_api.Domain.Payments.StripePaymentGateway`: Production implementation utilizing `Stripe.net` SDK.
3. **Application Services & Controllers**:
   - `nia_api.Services.PaymentService`: Coordinates payment attempts, gateway sessions, reconciliation logic, webhook deduplication, and refunds.
   - `nia_api.Controllers.PaymentController`: Exposes `create-checkout-session`, `verify-payment`, `stripe-webhook`, and the new admin `refund/{orderId}` endpoint.
   - `nia_api.Domain.Orders.OrderActor` & `OrderLifecycleService`: Enabled `OrderActor.System("Stripe")` to mark verified orders as paid through domain policy.

## Affected files and contracts
- `api/nia_api/Domain/Payments/PaymentAttempt.cs` [NEW]
- `api/nia_api/Domain/Payments/WebhookInboxMessage.cs` [NEW]
- `api/nia_api/Domain/Payments/IPaymentGateway.cs` [NEW]
- `api/nia_api/Domain/Payments/StripePaymentGateway.cs` [NEW]
- `api/nia_api/Requests/RefundRequest.cs` [NEW]
- `api/nia_api/Services/PaymentService.cs` [MODIFIED]
- `api/nia_api/Controllers/PaymentController.cs` [MODIFIED]
- `api/nia_api/Data/NiaDbContext.cs` [MODIFIED]
- `api/nia_api/Program.cs` [MODIFIED]
- `api/nia_api/Domain/Orders/OrderActor.cs` [MODIFIED]
- `api/nia_api/Domain/Orders/OrderLifecycleService.cs` [MODIFIED]
- `api/nia_api.Tests/ApiTestFixture.cs` [MODIFIED]
- `api/nia_api.Tests/PaymentLifecycleIntegrationTests.cs` [NEW]
- `docs/planning/06-delivery-roadmap.md` [MODIFIED]

## Data and deployment impact
- **Zero breaking changes to frontend API**: Existing routes `/payment/create-checkout-session`, `/payment/verify-payment`, and `/payment/stripe-webhook` maintain identical response payloads for Angular frontend.
- **Isolated persistence**: New collections `PaymentAttempts` and `WebhookInbox` operate independently without modifying existing document schemas.

## Verification performed
- `dotnet test api/nia_api.Tests/nia_api.Tests.csproj`: **107/107 tests passed** in 2 seconds (0 failures).
- Verified:
  1. `CreateSession_ValidOrder_CreatesPaymentAttemptAndReturnsUrl`: Payment attempt created in `Pending` status with correct amount and attempt number.
  2. `CreateSession_CancelledOrPaidOrder_Rejects`: Prevents new session creation on finalized orders.
  3. `VerifyPayment_PendingOrder_TransitionsToPaidAndCompletesAttempt`: Successful payment marks order `ZAPLATENA` and attempt `Paid`.
  4. `VerifyPayment_LatePaymentOnCancelledOrder_EntersRefundNeededReconciliation`: Order stays `ZRUSENA`, attempt transitions to `Refunded` with audit trail.
  5. `Webhook_DuplicateEvent_ProcessesOnceIdempotently`: Duplicate webhook delivery detected by `EventId` and acknowledged without re-executing state transitions.
  6. `Concurrent_PaymentAttempts_SecondPaidAttemptEntersRefundNeeded`: Concurrent sessions both paid -> first marks order `Paid`, second automatically triggers refund.
  7. `StaffRefund_PaidOrder_ExecutesGatewayRefundAndRecordsAudit`: Admin refund triggers gateway refund, updates attempt to `Refunded`, and transitions order to `ZRUSENA`.
- `npm run build` in `web/`: **Exit code 0, 0 errors**.

## Remaining risks or follow-up
- Milestone **E06** will establish merchant configuration, optional personalization, and entitlement policy.
