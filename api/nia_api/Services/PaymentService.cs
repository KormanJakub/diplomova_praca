using MongoDB.Driver;
using nia_api.Data;
using nia_api.Domain.Orders;
using nia_api.Domain.Payments;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Security;

namespace nia_api.Services;

public class PaymentService
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly IOrderStore _orderStore;
    private readonly IOrderLifecycleService _orderLifecycleService;
    private readonly IMongoCollection<Order> _orders;
    private readonly IMongoCollection<PaymentAttempt> _paymentAttempts;
    private readonly IMongoCollection<WebhookInboxMessage> _webhookInbox;
    private readonly string _frontendBaseUrl;

    public PaymentService(
        IConfiguration configuration,
        NiaDbContext context,
        IPaymentGateway paymentGateway,
        IOrderStore orderStore,
        IOrderLifecycleService orderLifecycleService)
    {
        _paymentGateway = paymentGateway;
        _orderStore = orderStore;
        _orderLifecycleService = orderLifecycleService;
        _orders = context.Orders;
        _paymentAttempts = context.PaymentAttempts;
        _webhookInbox = context.WebhookInbox;
        _frontendBaseUrl = configuration["Hosting:Web-Url"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Missing frontend URL.");
    }

    public async Task<string?> CreateSessionAsync(PaymentRequest request, string? idempotencyKey = null, CancellationToken ct = default)
    {
        if (request.OrderId <= 0 || string.IsNullOrWhiteSpace(request.CancellationToken)) return null;

        var tokenHash = CapabilityToken.Hash(request.CancellationToken);
        var order = await _orders.Find(o => o.Id == request.OrderId &&
            o.CancellationToken == tokenHash &&
            o.CancellationTokenExpiresAt > DateTime.UtcNow).FirstOrDefaultAsync(ct);

        if (order == null || order.TotalPrice <= 0 || order.PaymentStatus == "Paid" ||
            order.StatusOrder == EStatus.ZRUSENA || order.StatusOrder == EStatus.ZAPLATENA ||
            order.PaymentMethod != "Stripe")
            return null;

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _paymentAttempts.Find(a => a.OrderId == order.Id && a.IdempotencyKey == idempotencyKey).FirstOrDefaultAsync(ct);
            if (existing != null && !string.IsNullOrEmpty(existing.SessionUrl) && existing.Status != PaymentAttemptStatus.Failed)
            {
                return existing.SessionUrl;
            }
        }

        var attemptCount = await _paymentAttempts.CountDocumentsAsync(a => a.OrderId == order.Id, cancellationToken: ct);
        var attempt = new PaymentAttempt
        {
            OrderId = order.Id,
            AttemptNumber = (int)attemptCount + 1,
            Provider = "Stripe",
            PaymentMethod = "Stripe",
            Amount = order.TotalPrice,
            Currency = "EUR",
            Status = PaymentAttemptStatus.Initiated,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow
        };
        await _paymentAttempts.InsertOneAsync(attempt, cancellationToken: ct);

        var sessionRequest = new GatewaySessionRequest(
            OrderId: order.Id,
            Amount: order.TotalPrice,
            Currency: "EUR",
            CancellationToken: request.CancellationToken,
            SuccessUrl: $"{_frontendBaseUrl}/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl: $"{_frontendBaseUrl}/cancel#{Uri.EscapeDataString(request.CancellationToken)}",
            Metadata: new Dictionary<string, string>
            {
                ["OrderId"] = order.Id.ToString(),
                ["AttemptId"] = attempt.Id.ToString(),
                ["UserId"] = order.UserId.ToString()
            }
        );

        var sessionResult = await _paymentGateway.CreateSessionAsync(sessionRequest, ct);
        if (!sessionResult.IsSuccess || string.IsNullOrEmpty(sessionResult.Url) || string.IsNullOrEmpty(sessionResult.SessionId))
        {
            attempt.Status = PaymentAttemptStatus.Failed;
            attempt.FailureReason = sessionResult.ErrorMessage ?? "Failed to create gateway session";
            await _paymentAttempts.ReplaceOneAsync(a => a.Id == attempt.Id, attempt, cancellationToken: ct);
            return null;
        }

        attempt.ExternalReferenceId = sessionResult.SessionId;
        attempt.SessionUrl = sessionResult.Url;
        attempt.Status = PaymentAttemptStatus.Pending;
        await _paymentAttempts.ReplaceOneAsync(a => a.Id == attempt.Id, attempt, cancellationToken: ct);

        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(o => o.Id, order.Id),
            Builders<Order>.Filter.Eq(o => o.CancellationToken, tokenHash),
            Builders<Order>.Filter.Ne(o => o.StatusOrder, EStatus.ZRUSENA),
            Builders<Order>.Filter.Ne(o => o.StatusOrder, EStatus.ZAPLATENA),
            Builders<Order>.Filter.Ne(o => o.PaymentStatus, "Paid"));
        var update = Builders<Order>.Update
            .Set(o => o.PaymentId, sessionResult.SessionId)
            .Set(o => o.PaymentStatus, "Pending")
            .Set(o => o.UpdatedAt, DateTime.UtcNow);
        var updateResult = await _orders.UpdateOneAsync(filter, update, cancellationToken: ct);

        return updateResult.MatchedCount == 0 ? null : sessionResult.Url;
    }

    public async Task<bool> VerifyPaymentAsync(string sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;

        var verification = await _paymentGateway.VerifySessionAsync(sessionId, ct);
        if (!verification.IsSuccess || verification.PaymentStatus != "paid" ||
            !string.Equals(verification.Currency, "eur", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(verification.ClientReferenceId, out var orderId))
        {
            return false;
        }

        var order = await _orderStore.GetByIdAsync(orderId, ct);
        if (order == null) return false;

        var attempt = await _paymentAttempts.Find(a => a.ExternalReferenceId == sessionId).FirstOrDefaultAsync(ct);

        var expectedCents = decimal.ToInt64(decimal.Round(order.TotalPrice * 100, 0));
        if (verification.AmountTotal != expectedCents)
        {
            if (attempt != null)
            {
                attempt.Status = PaymentAttemptStatus.Failed;
                attempt.FailureReason = $"Amount mismatch: expected {expectedCents}, got {verification.AmountTotal}";
                await _paymentAttempts.ReplaceOneAsync(a => a.Id == attempt.Id, attempt, cancellationToken: ct);
            }
            return false;
        }

        // 1. Reconciliation: Order was cancelled before payment completed!
        if (order.StatusOrder == EStatus.ZRUSENA)
        {
            if (attempt != null)
            {
                attempt.Status = PaymentAttemptStatus.RefundNeeded;
                attempt.PaymentIntentId = verification.PaymentIntentId;
                attempt.CompletedAt = DateTime.UtcNow;
                attempt.AuditNotes.Add("Payment received after order was already cancelled. Initiating refund.");

                if (!string.IsNullOrEmpty(verification.PaymentIntentId))
                {
                    var refundResult = await _paymentGateway.RefundPaymentAsync(new GatewayRefundRequest(
                        PaymentReference: verification.PaymentIntentId,
                        Amount: order.TotalPrice,
                        Reason: "Order was already cancelled before payment completed"
                    ), ct);

                    if (refundResult.IsSuccess)
                    {
                        attempt.Status = PaymentAttemptStatus.Refunded;
                        attempt.RefundedAt = DateTime.UtcNow;
                        attempt.RefundReference = refundResult.RefundId;
                        attempt.AuditNotes.Add($"Automated refund executed: {refundResult.RefundId}");
                    }
                    else
                    {
                        attempt.AuditNotes.Add($"Automated refund attempt failed: {refundResult.ErrorMessage}. Flagged for staff action.");
                    }
                }
                await _paymentAttempts.ReplaceOneAsync(a => a.Id == attempt.Id, attempt, cancellationToken: ct);
            }
            return true;
        }

        // 2. Reconciliation: Order is already paid (e.g. concurrent session succeeded first)
        if (order.PaymentStatus == "Paid" || order.StatusOrder == EStatus.ZAPLATENA)
        {
            if (attempt != null && attempt.Status == PaymentAttemptStatus.Paid)
            {
                return true;
            }

            if (attempt != null)
            {
                attempt.Status = PaymentAttemptStatus.RefundNeeded;
                attempt.PaymentIntentId = verification.PaymentIntentId;
                attempt.CompletedAt = DateTime.UtcNow;
                attempt.AuditNotes.Add("Duplicate payment received for already paid order. Initiating refund.");

                if (!string.IsNullOrEmpty(verification.PaymentIntentId))
                {
                    var refundResult = await _paymentGateway.RefundPaymentAsync(new GatewayRefundRequest(
                        PaymentReference: verification.PaymentIntentId,
                        Amount: order.TotalPrice,
                        Reason: "duplicate"
                    ), ct);

                    if (refundResult.IsSuccess)
                    {
                        attempt.Status = PaymentAttemptStatus.Refunded;
                        attempt.RefundedAt = DateTime.UtcNow;
                        attempt.RefundReference = refundResult.RefundId;
                        attempt.AuditNotes.Add($"Duplicate payment automatically refunded: {refundResult.RefundId}");
                    }
                    else
                    {
                        attempt.AuditNotes.Add($"Refund attempt failed: {refundResult.ErrorMessage}. Flagged for staff action.");
                    }
                }
                await _paymentAttempts.ReplaceOneAsync(a => a.Id == attempt.Id, attempt, cancellationToken: ct);
            }
            return true;
        }

        // 3. Standard successful flow: order is PRIJATA
        if (order.StatusOrder != EStatus.PRIJATA)
            return false;

        var markPaidResult = await _orderLifecycleService.MarkPaidAsync(order.Id, OrderActor.System("Stripe"), ct);
        if (!markPaidResult.IsSuccess)
            return false;

        if (attempt != null)
        {
            attempt.Status = PaymentAttemptStatus.Paid;
            attempt.PaymentIntentId = verification.PaymentIntentId;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.AuditNotes.Add("Payment successfully verified and order marked paid.");
            await _paymentAttempts.ReplaceOneAsync(a => a.Id == attempt.Id, attempt, cancellationToken: ct);

            await _paymentAttempts.UpdateManyAsync(
                a => a.OrderId == order.Id && a.Id != attempt.Id && a.Status == PaymentAttemptStatus.Pending,
                Builders<PaymentAttempt>.Update.Set(a => a.Status, PaymentAttemptStatus.Cancelled),
                cancellationToken: ct);
        }

        await _orders.UpdateOneAsync(
            o => o.Id == order.Id,
            Builders<Order>.Update
                .Set(o => o.PaymentId, sessionId)
                .Set(o => o.PaymentStatus, "Paid")
                .Set(o => o.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return true;
    }

    public async Task<bool> ProcessWebhookEventAsync(string eventId, string eventType, string payload, string? sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return false;

        var existing = await _webhookInbox.Find(m => m.EventId == eventId).FirstOrDefaultAsync(ct);
        if (existing != null && existing.Status == WebhookStatus.Processed)
        {
            return true;
        }

        if (existing == null)
        {
            existing = new WebhookInboxMessage
            {
                Provider = "Stripe",
                EventId = eventId,
                EventType = eventType,
                Payload = payload,
                Status = WebhookStatus.Processing,
                ReceivedAt = DateTime.UtcNow
            };
            await _webhookInbox.InsertOneAsync(existing, cancellationToken: ct);
        }
        else
        {
            await _webhookInbox.UpdateOneAsync(
                m => m.Id == existing.Id,
                Builders<WebhookInboxMessage>.Update.Set(m => m.Status, WebhookStatus.Processing),
                cancellationToken: ct);
        }

        bool result = false;
        if (eventType == "checkout.session.completed" || eventType == "checkout.session.async_payment_succeeded")
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                result = await VerifyPaymentAsync(sessionId, ct);
            }
        }
        else
        {
            result = true;
        }

        var finalStatus = result ? WebhookStatus.Processed : WebhookStatus.Failed;
        await _webhookInbox.UpdateOneAsync(
            m => m.Id == existing.Id,
            Builders<WebhookInboxMessage>.Update
                .Set(m => m.Status, finalStatus)
                .Set(m => m.ProcessedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return result;
    }

    public async Task<bool> RefundOrderAsync(int orderId, decimal? amount = null, string? reason = null, OrderActor? actor = null, CancellationToken ct = default)
    {
        var order = await _orderStore.GetByIdAsync(orderId, ct);
        if (order == null) return false;

        var paidAttempt = await _paymentAttempts.Find(a => a.OrderId == orderId && a.Status == PaymentAttemptStatus.Paid).FirstOrDefaultAsync(ct);
        var paymentReference = paidAttempt?.PaymentIntentId ?? paidAttempt?.ExternalReferenceId ?? order.PaymentId;

        if (string.IsNullOrEmpty(paymentReference)) return false;

        var refundRequest = new GatewayRefundRequest(
            PaymentReference: paymentReference,
            Amount: amount ?? order.TotalPrice,
            Reason: reason ?? "requested_by_customer"
        );

        var refundResult = await _paymentGateway.RefundPaymentAsync(refundRequest, ct);
        if (!refundResult.IsSuccess) return false;

        if (paidAttempt != null)
        {
            paidAttempt.Status = PaymentAttemptStatus.Refunded;
            paidAttempt.RefundedAt = DateTime.UtcNow;
            paidAttempt.RefundReference = refundResult.RefundId;
            paidAttempt.AuditNotes.Add($"Refunded by {actor?.StaffRole ?? "Staff"}: {reason} (Ref: {refundResult.RefundId})");
            await _paymentAttempts.ReplaceOneAsync(a => a.Id == paidAttempt.Id, paidAttempt, cancellationToken: ct);
        }

        if (!amount.HasValue || amount.Value >= order.TotalPrice)
        {
            await _orderLifecycleService.CancelOrderAsync(orderId, actor ?? OrderActor.Staff(), "Refunded", ct);
        }

        return true;
    }
}
