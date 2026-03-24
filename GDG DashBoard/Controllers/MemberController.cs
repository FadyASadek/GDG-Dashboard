using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GDG_DashBoard.Controllers;

[Authorize]
public class MemberController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
