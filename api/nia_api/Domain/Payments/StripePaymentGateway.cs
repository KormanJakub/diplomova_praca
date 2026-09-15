using Stripe;
using Stripe.Checkout;

namespace nia_api.Domain.Payments;

public class StripePaymentGateway : IPaymentGateway
{
    private readonly IConfiguration _configuration;

    public StripePaymentGateway(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GatewaySessionResult> CreateSessionAsync(GatewaySessionRequest request, CancellationToken ct = default)
    {
        try
        {
            var amountInCents = decimal.ToInt64(decimal.Round(request.Amount * 100, 0));
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = request.Currency.ToLowerInvariant(),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Objednávka {request.OrderId}"
                            },
                            UnitAmount = amountInCents
                        },
                        Quantity = 1
                    }
                },
                ClientReferenceId = request.OrderId.ToString(),
                CustomerEmail = request.CustomerEmail,
                Mode = "payment",
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Metadata = request.Metadata != null ? new Dictionary<string, string>(request.Metadata) : null
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: ct);
            return new GatewaySessionResult(true, session.Id, session.Url);
        }
        catch (StripeException ex)
        {
            return new GatewaySessionResult(false, ErrorMessage: ex.Message);
        }
        catch (Exception ex)
        {
            return new GatewaySessionResult(false, ErrorMessage: ex.Message);
        }
    }

    public async Task<GatewayVerificationResult> VerifySessionAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId, cancellationToken: ct);
            return new GatewayVerificationResult(
                true,
                SessionId: session.Id,
                PaymentStatus: session.PaymentStatus,
                Currency: session.Currency,
                AmountTotal: session.AmountTotal ?? 0,
                ClientReferenceId: session.ClientReferenceId,
                PaymentIntentId: session.PaymentIntentId ?? session.PaymentIntent?.Id
            );
        }
        catch (StripeException ex)
        {
            return new GatewayVerificationResult(false, ErrorMessage: ex.Message);
        }
        catch (Exception ex)
        {
            return new GatewayVerificationResult(false, ErrorMessage: ex.Message);
        }
    }

    public async Task<GatewayRefundResult> RefundPaymentAsync(GatewayRefundRequest request, CancellationToken ct = default)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = request.PaymentReference,
                Reason = request.Reason switch
                {
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    _ => "requested_by_customer"
                }
            };

            if (request.Amount.HasValue && request.Amount.Value > 0)
            {
                options.Amount = decimal.ToInt64(decimal.Round(request.Amount.Value * 100, 0));
            }

            var service = new RefundService();
            var refund = await service.CreateAsync(options, cancellationToken: ct);
            return new GatewayRefundResult(true, refund.Id, refund.Status);
        }
        catch (StripeException ex)
        {
            return new GatewayRefundResult(false, ErrorMessage: ex.Message);
        }
        catch (Exception ex)
        {
            return new GatewayRefundResult(false, ErrorMessage: ex.Message);
        }
    }
}
