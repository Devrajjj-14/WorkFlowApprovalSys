using Microsoft.AspNetCore.Mvc;

namespace WorkflowApprovalUI.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Projects");
}
