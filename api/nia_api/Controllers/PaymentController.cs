using Microsoft.AspNetCore.Mvc;
using nia_api.Domain.Orders;
using nia_api.Requests;
using nia_api.Services;
using Stripe;

namespace nia_api.Controllers;

[ApiController]
[Route("payment")]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public PaymentController(PaymentService paymentService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _configuration = configuration;
    }

    [HttpPost("create-checkout-session")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("sensitive")]
    public async Task<IActionResult> CreateCheckoutSession(
        [FromBody] PaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        if (request == null || request.OrderId <= 0 || string.IsNullOrWhiteSpace(request.CancellationToken))
            return BadRequest("Invalid request");

        var sessionUrl = await _paymentService.CreateSessionAsync(request, idempotencyKey);
        return sessionUrl == null ? BadRequest(new { error = "Invalid order." }) : Ok(new { url = sessionUrl });
    }

    [HttpPost("verify-payment")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public-read")]
    public async Task<IActionResult> VerifyPayment(string sessionId)
    {
        var isVerified = await _paymentService.VerifyPaymentAsync(sessionId);

        if (!isVerified)
            return BadRequest(new { error = "Payment verification failed!" });

        return Ok(new { message = "Payment verified successfully!" });
    }

    [HttpPost("stripe-webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret)) return StatusCode(503);

        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(body, Request.Headers["Stripe-Signature"], webhookSecret);
        }
        catch (StripeException)
        {
            return BadRequest();
        }

        var sessionId = (stripeEvent.Data.Object as Stripe.Checkout.Session)?.Id;
        var processed = await _paymentService.ProcessWebhookEventAsync(stripeEvent.Id, stripeEvent.Type, body, sessionId);
        if (!processed) return StatusCode(503);

        return Ok();
    }

    [HttpPost("refund/{orderId}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
    public async Task<IActionResult> RefundOrder(int orderId, [FromBody] RefundRequest? request)
    {
        var success = await _paymentService.RefundOrderAsync(
            orderId,
            request?.Amount,
            request?.Reason,
            OrderActor.Staff());

        if (!success)
            return BadRequest(new { error = "Refund failed or order cannot be refunded." });

        return Ok(new { message = "Refund executed successfully!" });
    }
}
