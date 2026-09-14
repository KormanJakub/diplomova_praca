using Microsoft.AspNetCore.Mvc;
using nia_api.Requests;
using nia_api.Services;
using Stripe;

namespace nia_api.Controllers;

/*
 * TODO:
 * Vytvor platobnú bránu 
 * Webhook
 */

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
 public async Task<IActionResult> CreateCheckoutSession([FromBody] PaymentRequest request)
 {
  if (request == null || request.OrderId <= 0 || string.IsNullOrWhiteSpace(request.CancellationToken))
   return BadRequest("Invalid request");
  
  var sessionUrl = await _paymentService.CreateSessionAsync(request);
  return sessionUrl == null ? BadRequest(new { error = "Invalid order." }) : Ok(new { url = sessionUrl });
 }

 [HttpPost("verify-payment")]
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

  if (stripeEvent.Type == "checkout.session.completed" ||
      stripeEvent.Type == "checkout.session.async_payment_succeeded")
  {
   if (stripeEvent.Data.Object is not Stripe.Checkout.Session session ||
       !await _paymentService.VerifyPaymentAsync(session.Id)) return StatusCode(503);
  }

  return Ok();
 }
}
