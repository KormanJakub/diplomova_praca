using nia_api.Requests;
using Stripe;
using Stripe.Checkout;

namespace nia_api.Services;

public class PaymentService
{
    private readonly IConfiguration _configuration;
    
    public PaymentService(IConfiguration configuration)
    {
        _configuration = configuration;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }
    
    public string CreateSession(PaymentRequest request)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.ProductName,
                        },
                        UnitAmount = (long)(request.Amount * 100),
                    },
                    Quantity = request.Quantity,
                },
            },
            Mode = "payment",
            SuccessUrl = "https://localhost:4200/success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = "https://localhost:4200/cancel",
        };

        var service = new SessionService();
        var session = service.Create(options);

        return session.Url;
    }
    
    public async Task<bool> VerifyPayment(string sessionId)
    {
        var service = new SessionService();
        var session = await service.GetAsync(sessionId);

        if (session.PaymentStatus == "paid")
        {
            return true;
        }

        return false;
    }
}
