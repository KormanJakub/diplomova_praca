using Microsoft.AspNetCore.Mvc;
using nia_api.Requests;
using nia_api.Services;

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

  public PaymentController(PaymentService paymentService)
  {
   _paymentService = paymentService;
  }

 [HttpPost("create-checkout-session")]
 public IActionResult CreateCheckoutSession([FromBody] PaymentRequest request)
 {
  var sessionUrl = _paymentService.CreateSession(request);
  return Ok(new { url = sessionUrl });
 }

 [HttpPost("verify-payment")]
 public async Task<IActionResult> VerifyPayment(string sessionId)
 {
  var isVerified = await _paymentService.VerifyPayment(sessionId);

  if (!isVerified)
   return BadRequest(new { error = "Payment verification failed!" });

  return Ok(new { message = "Payment verified successfully!" });
 }
}