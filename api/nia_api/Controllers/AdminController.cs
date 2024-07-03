using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace nia_api.Controllers;

[Authorize("Admin")]
public class AdminController : ControllerBase
{
    public AdminController()
    {
        
    }
}