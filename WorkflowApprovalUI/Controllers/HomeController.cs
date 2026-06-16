// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: Controller, IActionResult, RedirectToAction don't exist — Home route won't compile
using Microsoft.AspNetCore.Mvc;

namespace WorkflowApprovalUI.Controllers;

// HomeController is the default landing controller when the app root URL is visited
// Without this class: default route has no target — root URL may 404 or misroute
public class HomeController : Controller
{
    // Root/home action — immediately sends users to the project list (the real home of the app)
    // Without this action: visiting / or /Home/Index has no handler — users see an error page
    public IActionResult Index() => RedirectToAction("Index", "Projects");
}
