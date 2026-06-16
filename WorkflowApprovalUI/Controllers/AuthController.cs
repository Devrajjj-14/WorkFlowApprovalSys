using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalUI.Models;
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

public class AuthController : Controller
{
    private readonly ApiService _api;
    public AuthController(ApiService api) => _api = api;

    [HttpGet]
    public IActionResult Login() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Projects")
        : View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        var (data, error, responseTime) = await _api.LoginAsync(vm.Email, vm.Password);
        if (data == null)
        {
            ViewBag.Error = error ?? "Login failed.";
            return View(vm);
        }

        // Store response time from ServerTimingMiddleware to display after redirect
        if (responseTime != null)
            TempData["ServerTime"] = $"Login processed in {responseTime} by the server.";

        HttpContext.Session.SetString("JwtToken", data.Token);
        HttpContext.Session.SetString("UserName", data.FullName);
        HttpContext.Session.SetString("UserRole", data.Role);
        HttpContext.Session.SetInt32("UserId", data.UserId);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, data.UserId.ToString()),
            new(ClaimTypes.Name, data.FullName),
            new(ClaimTypes.Email, data.Email),
            new(ClaimTypes.Role, data.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Projects");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        var (data, error) = await _api.RegisterAsync(vm);
        if (data == null)
        {
            ViewBag.Error = error ?? "Registration failed.";
            return View(vm);
        }
        TempData["Success"] = "Account created! Please log in.";
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}
