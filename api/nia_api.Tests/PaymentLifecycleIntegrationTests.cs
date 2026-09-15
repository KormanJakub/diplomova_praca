using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using nia_api.Controllers;
using nia_api.Domain.Orders;
using nia_api.Domain.Payments;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Security;
using nia_api.Services;

namespace nia_api.Tests;

public class PaymentLifecycleIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public PaymentLifecycleIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateSession_ValidOrder_CreatesPaymentAttemptAndReturnsUrl()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var rawToken = CapabilityToken.Create();
        var hashedToken = CapabilityToken.Hash(rawToken);
        var orderId = new Random().Next(300000, 399999);

        var order = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            TotalPrice = 45.00m,
            PaymentMethod = "Stripe",
            PaymentStatus = "Pending",
            StatusOrder = EStatus.PRIJATA,
            CancellationToken = hashedToken,
            CancellationTokenExpiresAt = DateTime.UtcNow.AddHours(12),
            FollowToken = CapabilityToken.Hash(CapabilityToken.Create()),
            FollowTokenExpiresAt = DateTime.UtcNow.AddDays(180),
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(order);

        var expectedSessionId = $"cs_test_{Guid.NewGuid():N}";
        var expectedUrl = $"https://checkout.stripe.com/pay/{expectedSessionId}";

        _factory.PaymentGatewayMock
            .Setup(g => g.CreateSessionAsync(It.Is<GatewaySessionRequest>(r => r.OrderId == orderId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewaySessionResult(true, expectedSessionId, expectedUrl));

        var client = _factory.CreateAnonymousClient();

        // Act
        var response = await client.PostAsJsonAsync("/payment/create-checkout-session", new PaymentRequest
        {
            OrderId = orderId,
            CancellationToken = rawToken
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SessionUrlResponse>();
        Assert.NotNull(body);
        Assert.Equal(expectedUrl, body.url);

        // Verify PaymentAttempt was created with status Pending
        var attempt = await db.PaymentAttempts.Find(a => a.OrderId == orderId && a.ExternalReferenceId == expectedSessionId).FirstOrDefaultAsync();
        Assert.NotNull(attempt);
        Assert.Equal(PaymentAttemptStatus.Pending, attempt.Status);
        Assert.Equal(45.00m, attempt.Amount);
        Assert.Equal(1, attempt.AttemptNumber);

        // Verify order PaymentId was updated
        var updatedOrder = await db.Orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        Assert.NotNull(updatedOrder);
        Assert.Equal(expectedSessionId, updatedOrder.PaymentId);
    }

    [Fact]
    public async Task CreateSession_CancelledOrPaidOrder_Rejects()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var rawToken = CapabilityToken.Create();
        var hashedToken = CapabilityToken.Hash(rawToken);
        var orderId = new Random().Next(300000, 399999);

        var cancelledOrder = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            TotalPrice = 30.00m,
            PaymentMethod = "Stripe",
            StatusOrder = EStatus.ZRUSENA, // Cancelled!
            CancellationToken = hashedToken,
            CancellationTokenExpiresAt = DateTime.UtcNow.AddHours(12),
            FollowToken = CapabilityToken.Hash(CapabilityToken.Create()),
            FollowTokenExpiresAt = DateTime.UtcNow.AddDays(180),
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(cancelledOrder);

        var client = _factory.CreateAnonymousClient();

        // Act
        var response = await client.PostAsJsonAsync("/payment/create-checkout-session", new PaymentRequest
        {
            OrderId = orderId,
            CancellationToken = rawToken
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VerifyPayment_PendingOrder_TransitionsToPaidAndCompletesAttempt()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var orderId = new Random().Next(400000, 499999);
        var sessionId = $"cs_test_{Guid.NewGuid():N}";
        var paymentIntentId = $"pi_test_{Guid.NewGuid():N}";

        var order = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            TotalPrice = 50.00m,
            PaymentMethod = "Stripe",
            PaymentStatus = "Pending",
            StatusOrder = EStatus.PRIJATA,
            PaymentId = sessionId,
            CancellationToken = CapabilityToken.Hash(CapabilityToken.Create()),
            FollowToken = CapabilityToken.Hash(CapabilityToken.Create()),
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(order);

        var attempt = new PaymentAttempt
        {
            OrderId = orderId,
            AttemptNumber = 1,
            Provider = "Stripe",
            PaymentMethod = "Stripe",
            Amount = 50.00m,
            Currency = "EUR",
            Status = PaymentAttemptStatus.Pending,
            ExternalReferenceId = sessionId,
            CreatedAt = DateTime.UtcNow
        };
        await db.PaymentAttempts.InsertOneAsync(attempt);

        _factory.PaymentGatewayMock
            .Setup(g => g.VerifySessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayVerificationResult(
                IsSuccess: true,
                SessionId: sessionId,
                PaymentStatus: "paid",
                Currency: "eur",
                AmountTotal: 5000, // 50.00 EUR in cents
                ClientReferenceId: orderId.ToString(),
                PaymentIntentId: paymentIntentId));

        var client = _factory.CreateAnonymousClient();

        // Act
        var response = await client.PostAsync($"/payment/verify-payment?sessionId={sessionId}", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Order is now ZAPLATENA and Paid
        var updatedOrder = await db.Orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        Assert.NotNull(updatedOrder);
        Assert.Equal(EStatus.ZAPLATENA, updatedOrder.StatusOrder);
        Assert.Equal("Paid", updatedOrder.PaymentStatus);

        // PaymentAttempt is Paid
        var updatedAttempt = await db.PaymentAttempts.Find(a => a.Id == attempt.Id).FirstOrDefaultAsync();
        Assert.NotNull(updatedAttempt);
        Assert.Equal(PaymentAttemptStatus.Paid, updatedAttempt.Status);
        Assert.Equal(paymentIntentId, updatedAttempt.PaymentIntentId);
        Assert.NotNull(updatedAttempt.CompletedAt);
    }

    [Fact]
    public async Task VerifyPayment_LatePaymentOnCancelledOrder_EntersRefundNeededReconciliation()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var orderId = new Random().Next(500000, 599999);
        var sessionId = $"cs_test_late_{Guid.NewGuid():N}";
        var paymentIntentId = $"pi_test_late_{Guid.NewGuid():N}";
        var refundId = $"re_test_{Guid.NewGuid():N}";

        var cancelledOrder = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            TotalPrice = 60.00m,
            PaymentMethod = "Stripe",
            PaymentStatus = "Pending",
            StatusOrder = EStatus.ZRUSENA, // Cancelled!
            PaymentId = sessionId,
            CancellationToken = CapabilityToken.Hash(CapabilityToken.Create()),
            FollowToken = CapabilityToken.Hash(CapabilityToken.Create()),
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(cancelledOrder);

        var attempt = new PaymentAttempt
        {
            OrderId = orderId,
            AttemptNumber = 1,
            Provider = "Stripe",
            PaymentMethod = "Stripe",
            Amount = 60.00m,
            Currency = "EUR",
            Status = PaymentAttemptStatus.Pending,
            ExternalReferenceId = sessionId,
            CreatedAt = DateTime.UtcNow
        };
        await db.PaymentAttempts.InsertOneAsync(attempt);

        _factory.PaymentGatewayMock
            .Setup(g => g.VerifySessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayVerificationResult(
                IsSuccess: true,
                SessionId: sessionId,
                PaymentStatus: "paid",
                Currency: "eur",
                AmountTotal: 6000,
                ClientReferenceId: orderId.ToString(),
                PaymentIntentId: paymentIntentId));

        _factory.PaymentGatewayMock
            .Setup(g => g.RefundPaymentAsync(It.Is<GatewayRefundRequest>(r => r.PaymentReference == paymentIntentId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayRefundResult(true, refundId, "succeeded"));

        var client = _factory.CreateAnonymousClient();

        // Act
        var response = await client.PostAsync($"/payment/verify-payment?sessionId={sessionId}", null);

        // Assert: Succeeded in reconciling
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // INVARIANT: Order must NOT be resurrected; remains ZRUSENA
        var updatedOrder = await db.Orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        Assert.NotNull(updatedOrder);
        Assert.Equal(EStatus.ZRUSENA, updatedOrder.StatusOrder);

        // INVARIANT: PaymentAttempt entered refund flow and executed refund
        var updatedAttempt = await db.PaymentAttempts.Find(a => a.Id == attempt.Id).FirstOrDefaultAsync();
        Assert.NotNull(updatedAttempt);
        Assert.Equal(PaymentAttemptStatus.Refunded, updatedAttempt.Status);
        Assert.Equal(refundId, updatedAttempt.RefundReference);
        Assert.Contains(updatedAttempt.AuditNotes, n => n.Contains("cancelled"));
    }

    [Fact]
    public async Task Webhook_DuplicateEvent_ProcessesOnceIdempotently()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var orderId = new Random().Next(600000, 699999);
        var sessionId = $"cs_test_wh_{Guid.NewGuid():N}";
        var eventId = $"evt_test_{Guid.NewGuid():N}";

        var order = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            TotalPrice = 25.00m,
            PaymentMethod = "Stripe",
            PaymentStatus = "Pending",
            StatusOrder = EStatus.PRIJATA,
            PaymentId = sessionId,
            CancellationToken = CapabilityToken.Hash(CapabilityToken.Create()),
            FollowToken = CapabilityToken.Hash(CapabilityToken.Create()),
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(order);

        var attempt = new PaymentAttempt
        {
            OrderId = orderId,
            AttemptNumber = 1,
            Provider = "Stripe",
            PaymentMethod = "Stripe",
            Amount = 25.00m,
            Currency = "EUR",
            Status = PaymentAttemptStatus.Pending,
            ExternalReferenceId = sessionId,
            CreatedAt = DateTime.UtcNow
        };
        await db.PaymentAttempts.InsertOneAsync(attempt);

        _factory.PaymentGatewayMock
            .Setup(g => g.VerifySessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayVerificationResult(
                IsSuccess: true,
                SessionId: sessionId,
                PaymentStatus: "paid",
                Currency: "eur",
                AmountTotal: 2500,
                ClientReferenceId: orderId.ToString(),
                PaymentIntentId: "pi_wh_1"));

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<PaymentService>();

        // Act 1: First event delivery
        var firstResult = await service.ProcessWebhookEventAsync(eventId, "checkout.session.completed", "{}", sessionId);
        Assert.True(firstResult);

        // Act 2: Duplicate delivery of the exact same event
        var duplicateResult = await service.ProcessWebhookEventAsync(eventId, "checkout.session.completed", "{}", sessionId);
        Assert.True(duplicateResult);

        // Assert: WebhookInbox has exactly 1 record for this event with status Processed
        var inboxCount = await db.WebhookInbox.CountDocumentsAsync(m => m.EventId == eventId);
        Assert.Equal(1, inboxCount);

        var inboxEntry = await db.WebhookInbox.Find(m => m.EventId == eventId).FirstOrDefaultAsync();
        Assert.NotNull(inboxEntry);
        Assert.Equal(WebhookStatus.Processed, inboxEntry.Status);
    }

    [Fact]
    public async Task Concurrent_PaymentAttempts_SecondPaidAttemptEntersRefundNeeded()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var orderId = new Random().Next(700000, 799999);
        var session1 = $"cs_test_c1_{Guid.NewGuid():N}";
        var session2 = $"cs_test_c2_{Guid.NewGuid():N}";
        var pi2 = $"pi_test_c2_{Guid.NewGuid():N}";
        var refundId = $"re_dup_{Guid.NewGuid():N}";

        var order = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            TotalPrice = 35.00m,
            PaymentMethod = "Stripe",
            PaymentStatus = "Pending",
            StatusOrder = EStatus.PRIJATA,
            PaymentId = session1,
            CancellationToken = CapabilityToken.Hash(CapabilityToken.Create()),
            FollowToken = CapabilityToken.Hash(CapabilityToken.Create()),
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(order);

        var attempt1 = new PaymentAttempt
        {
            OrderId = orderId,
            AttemptNumber = 1,
            Amount = 35.00m,
            Status = PaymentAttemptStatus.Pending,
            ExternalReferenceId = session1,
            CreatedAt = DateTime.UtcNow
        };
        var attempt2 = new PaymentAttempt
        {
            OrderId = orderId,
            AttemptNumber = 2,
            Amount = 35.00m,
            Status = PaymentAttemptStatus.Pending,
            ExternalReferenceId = session2,
            CreatedAt = DateTime.UtcNow
        };
        await db.PaymentAttempts.InsertManyAsync(new[] { attempt1, attempt2 });

        _factory.PaymentGatewayMock
            .Setup(g => g.VerifySessionAsync(session1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayVerificationResult(true, session1, "paid", "eur", 3500, orderId.ToString(), "pi_1"));

        _factory.PaymentGatewayMock
            .Setup(g => g.VerifySessionAsync(session2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayVerificationResult(true, session2, "paid", "eur", 3500, orderId.ToString(), pi2));

        _factory.PaymentGatewayMock
            .Setup(g => g.RefundPaymentAsync(It.Is<GatewayRefundRequest>(r => r.PaymentReference == pi2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayRefundResult(true, refundId, "succeeded"));

        var client = _factory.CreateAnonymousClient();

        // Act 1: Verify session 1 -> order becomes Paid
        var res1 = await client.PostAsync($"/payment/verify-payment?sessionId={session1}", null);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        // Act 2: Verify session 2 (duplicate concurrent payment)
        var res2 = await client.PostAsync($"/payment/verify-payment?sessionId={session2}", null);
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);

        // Assert: Attempt 1 is Paid, Attempt 2 is Refunded
        var updated1 = await db.PaymentAttempts.Find(a => a.Id == attempt1.Id).FirstOrDefaultAsync();
        var updated2 = await db.PaymentAttempts.Find(a => a.Id == attempt2.Id).FirstOrDefaultAsync();

        Assert.NotNull(updated1);
        Assert.NotNull(updated2);
        Assert.Equal(PaymentAttemptStatus.Paid, updated1.Status);
        Assert.Equal(PaymentAttemptStatus.Refunded, updated2.Status);
        Assert.Equal(refundId, updated2.RefundReference);
    }

    [Fact]
    public async Task StaffRefund_PaidOrder_ExecutesGatewayRefundAndRecordsAudit()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var orderId = new Random().Next(800000, 899999);
        var paymentIntentId = $"pi_staff_refund_{Guid.NewGuid():N}";
        var refundId = $"re_staff_{Guid.NewGuid():N}";

        var order = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            TotalPrice = 70.00m,
            PaymentMethod = "Stripe",
            PaymentStatus = "Paid",
            StatusOrder = EStatus.ZAPLATENA,
            PaymentId = "cs_staff_refund",
            CancellationToken = CapabilityToken.Hash(CapabilityToken.Create()),
            FollowToken = CapabilityToken.Hash(CapabilityToken.Create()),
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(order);

        var attempt = new PaymentAttempt
        {
            OrderId = orderId,
            AttemptNumber = 1,
            Amount = 70.00m,
            Status = PaymentAttemptStatus.Paid,
            PaymentIntentId = paymentIntentId,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        await db.PaymentAttempts.InsertOneAsync(attempt);

        _factory.PaymentGatewayMock
            .Setup(g => g.RefundPaymentAsync(It.Is<GatewayRefundRequest>(r => r.PaymentReference == paymentIntentId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayRefundResult(true, refundId, "succeeded"));

        var admin = await _factory.SeedUserAsync(isAdmin: true);
        var adminClient = _factory.CreateAuthenticatedClient(admin);

        // Act
        var response = await adminClient.PostAsJsonAsync($"/payment/refund/{orderId}", new RefundRequest
        {
            Reason = "Customer changed mind"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Attempt is now Refunded
        var updatedAttempt = await db.PaymentAttempts.Find(a => a.Id == attempt.Id).FirstOrDefaultAsync();
        Assert.NotNull(updatedAttempt);
        Assert.Equal(PaymentAttemptStatus.Refunded, updatedAttempt.Status);
        Assert.Equal(refundId, updatedAttempt.RefundReference);

        // Order is now ZRUSENA
        var updatedOrder = await db.Orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        Assert.NotNull(updatedOrder);
        Assert.Equal(EStatus.ZRUSENA, updatedOrder.StatusOrder);
    }

    private sealed class SessionUrlResponse
    {
        public string? url { get; set; }
    }
}
