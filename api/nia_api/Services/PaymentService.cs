using MongoDB.Driver;
using nia_api.Data;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
using Stripe.Checkout;

namespace nia_api.Services;

public class PaymentService
{
    private readonly IMongoCollection<Order> _orders;
    private readonly string _frontendBaseUrl;

    public PaymentService(IConfiguration configuration, NiaDbContext context)
    {
        _orders = context.Orders;
        _frontendBaseUrl = configuration["Hosting:Web-Url"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Missing frontend URL.");
    }

    public async Task<string?> CreateSessionAsync(PaymentRequest request)
    {
        if (request.OrderId <= 0 || string.IsNullOrWhiteSpace(request.CancellationToken)) return null;

        var order = await _orders.Find(o => o.Id == request.OrderId &&
            o.CancellationToken == request.CancellationToken).FirstOrDefaultAsync();
        if (order == null || order.TotalPrice <= 0 || order.PaymentStatus == "Paid" ||
            order.StatusOrder == EStatus.ZRUSENA || order.StatusOrder == EStatus.ZAPLATENA)
            return null;

        var amountInCents = decimal.ToInt64(decimal.Round(order.TotalPrice * 100, 0));
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Objednávka {order.Id}"
                        },
                        UnitAmount = amountInCents
                    },
                    Quantity = 1
                }
            },
            ClientReferenceId = order.Id.ToString(),
            Mode = "payment",
            SuccessUrl = $"{_frontendBaseUrl}/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{_frontendBaseUrl}/cancel?cancellationToken={Uri.EscapeDataString(order.CancellationToken)}"
        };

        var session = await new SessionService().CreateAsync(options);
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(o => o.Id, order.Id),
            Builders<Order>.Filter.Eq(o => o.CancellationToken, order.CancellationToken),
            Builders<Order>.Filter.Ne(o => o.StatusOrder, EStatus.ZRUSENA),
            Builders<Order>.Filter.Ne(o => o.StatusOrder, EStatus.ZAPLATENA),
            Builders<Order>.Filter.Ne(o => o.PaymentStatus, "Paid"));
        var update = Builders<Order>.Update
            .Set(o => o.PaymentId, session.Id)
            .Set(o => o.PaymentStatus, "Pending")
            .Set(o => o.UpdatedAt, DateTime.UtcNow);
        var result = await _orders.UpdateOneAsync(filter, update);
        return result.MatchedCount == 0 ? null : session.Url;
    }

    public async Task<bool> VerifyPaymentAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        var session = await new SessionService().GetAsync(sessionId);
        if (session.PaymentStatus != "paid" || session.Currency != "eur" ||
            !int.TryParse(session.ClientReferenceId, out var orderId)) return false;

        var order = await _orders.Find(o => o.Id == orderId && o.PaymentId == session.Id)
            .FirstOrDefaultAsync();
        if (order == null || order.StatusOrder == EStatus.ZRUSENA ||
            session.AmountTotal != decimal.ToInt64(decimal.Round(order.TotalPrice * 100, 0)))
            return false;
        if (order.PaymentStatus == "Paid") return true;
        if (order.StatusOrder != EStatus.PRIJATA) return false;

        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(o => o.Id, order.Id),
            Builders<Order>.Filter.Eq(o => o.PaymentId, session.Id),
            Builders<Order>.Filter.Eq(o => o.StatusOrder, EStatus.PRIJATA));
        var update = Builders<Order>.Update
            .Set(o => o.StatusOrder, EStatus.ZAPLATENA)
            .Set(o => o.PaymentStatus, "Paid")
            .Set(o => o.UpdatedAt, DateTime.UtcNow);
        var result = await _orders.UpdateOneAsync(filter, update);
        return result.MatchedCount > 0;
    }
}
