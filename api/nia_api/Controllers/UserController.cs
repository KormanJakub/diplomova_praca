using Microsoft.AspNetCore.Mvc;

namespace nia_api.Controllers;

[ApiController]
[Route("user")]
public class UserController : ControllerBase
{
    /*
     * TODO:
     * Svoje udaje
     * Update udajov
     * Vymazanie profilu
     * Nákup tovaru
     * Platobná brána
     */
    [HttpGet("profile")]
    public IActionResult GetUserProfile()
    {
        return Ok(new { Message = "This" });
    }
}