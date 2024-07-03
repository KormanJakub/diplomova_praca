using Microsoft.AspNetCore.Mvc;

namespace nia_api.Controllers;

public class AdminController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}