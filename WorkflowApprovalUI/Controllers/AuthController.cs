// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: Claim class doesn't exist — login can't build the user's identity
using System.Security.Claims;

// Without this: SignInAsync and SignOutAsync don't exist — cookie login/logout can't run
using Microsoft.AspNetCore.Authentication;

// Without this: CookieAuthenticationDefaults doesn't exist — cookie auth scheme name is unknown
using Microsoft.AspNetCore.Authentication.Cookies;

// Without this: Controller, IActionResult, View, RedirectToAction don't exist — MVC actions fail
using Microsoft.AspNetCore.Mvc;

// Without this: LoginViewModel, RegisterViewModel, AuthResponse are unknown — login/register won't compile
using WorkflowApprovalUI.Models;

// Without this: ApiService is unknown — this controller can't call the backend API
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

// AuthController handles login, registration, and logout for the MVC frontend
// Without this class: users can't sign in, sign up, or sign out — the app is unusable
public class AuthController : Controller
{
    // Injected ApiService — the only way this controller talks to the backend /api/auth endpoints
    // Without this field: Login and Register have no API client — authentication actions can't run
    private readonly ApiService _api;

    // DI injects ApiService when ASP.NET creates this controller per request
    // Without this constructor: DI can't wire ApiService — app crashes when Auth routes are hit
    public AuthController(ApiService api) => _api = api;

    // ── Login (GET) ───────────────────────────────────────────────────────────
    // Shows the login form, or redirects already-logged-in users to the project list
    // Without this action: GET /Auth/Login returns 404 — users never see the login page
    [HttpGet]
    public IActionResult Login() => User.Identity?.IsAuthenticated == true
        // Without this branch: logged-in users see the login form again instead of going to projects
        ? RedirectToAction("Index", "Projects")
        // Without this branch: unauthenticated users get no login view — blank or error page
        : View();

    // ── Login (POST) ──────────────────────────────────────────────────────────
    // Validates credentials against the backend, stores JWT in session, and signs in with a cookie
    // Without this action: the login form submit does nothing — users can never authenticate
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        // Calls backend POST /api/auth/login with email and password from the form
        // Also returns responseTime from ServerTimingMiddleware via the X-Response-Time header
        // Without this: credentials are never sent to the API — login always fails
        var (data, error, responseTime) = await _api.LoginAsync(vm.Email, vm.Password);

        // If the API returned no user/token, show the error on the login page again
        // Without this block: failed logins would still try to sign in — broken session and cookie state
        if (data == null)
        {
            // Puts the API error (or a fallback message) into the view for display
            // Without this: users see a generic blank page with no reason login failed
            ViewBag.Error = error ?? "Login failed.";

            // Re-renders the login form with the entered email/password still bound
            // Without this: failed login would redirect away — user loses what they typed
            return View(vm);
        }

        // Store response time from ServerTimingMiddleware to display after redirect on the projects page
        // Without this: login succeeds but user never sees how long the server took to process login
        if (responseTime != null)
            TempData["ServerTime"] = $"Login processed in {responseTime} by the server.";

        // Stores the JWT in server session so ApiService can attach it to every backend call
        // Without this: session has no token — all protected API calls return 401 after login
        HttpContext.Session.SetString("JwtToken", data.Token);

        // Stores display name in session for layout/header without re-fetching from API
        // Without this: UI can't show "Welcome, {name}" from session helpers
        HttpContext.Session.SetString("UserName", data.FullName);

        // Stores role in session for quick role checks in views if needed
        // Without this: views relying on session role string show empty or wrong role
        HttpContext.Session.SetString("UserRole", data.Role);

        // Stores numeric user id in session for client-side or view logic
        // Without this: session-based user id lookups return null
        HttpContext.Session.SetInt32("UserId", data.UserId);

        // Builds the list of claims that describe who is logged in for cookie authentication
        // Without this: SignInAsync has no identity — cookie auth doesn't know the user
        var claims = new List<Claim>
        {
            // Without this claim: [Authorize] and User.FindFirst(NameIdentifier) can't identify the user
            new(ClaimTypes.NameIdentifier, data.UserId.ToString()),

            // Without this claim: User.Identity.Name is empty — UI can't show the user's name
            new(ClaimTypes.Name, data.FullName),

            // Without this claim: email-based identity features and displays break
            new(ClaimTypes.Email, data.Email),

            // Without this claim: [Authorize(Roles = "...")] never matches — role gates block everyone
            new(ClaimTypes.Role, data.Role)
        };

        // Wraps claims in an identity tied to the cookie authentication scheme
        // Without this: SignInAsync has no valid ClaimsIdentity — cookie isn't created correctly
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // Writes the encrypted auth cookie to the browser — user is now "logged in" to the MVC app
        // Without this: no auth cookie — every [Authorize] page redirects back to login
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        // Sends the user to the project list after successful login
        // Without this: login succeeds but user stays on login page with no next step
        return RedirectToAction("Index", "Projects");
    }

    // ── Register (GET) ────────────────────────────────────────────────────────
    // Shows the registration form for new accounts
    // Without this action: GET /Auth/Register returns 404 — users can't open the register page
    [HttpGet]
    public IActionResult Register() => View();

    // ── Register (POST) ───────────────────────────────────────────────────────
    // Creates a new user via the backend API, then sends them to login on success
    // Without this action: register form submit does nothing — accounts can't be created
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        // Calls backend POST /api/auth/register with the form view model
        // Without this: registration data never reaches the API — sign-up always fails
        var (data, error) = await _api.RegisterAsync(vm);

        // If registration failed, show error and keep the user on the register form
        // Without this block: errors would still redirect — user thinks they registered when they didn't
        if (data == null)
        {
            // Surfaces API validation or conflict message on the register view
            // Without this: failed registration shows no feedback
            ViewBag.Error = error ?? "Registration failed.";

            // Re-renders register form with entered values preserved
            // Without this: user loses form input after a failed attempt
            return View(vm);
        }

        // Flash message shown once on the login page after redirect
        // Without this: user lands on login with no confirmation their account was created
        TempData["Success"] = "Account created! Please log in.";

        // Sends new user to login — registration does not auto-sign-in in this app
        // Without this: user stays on register page after success — confusing flow
        return RedirectToAction(nameof(Login));
    }

    // ── Logout ─────────────────────────────────────────────────────────────────
    // Clears cookie auth and session, then returns to the login page
    // Without this action: logout link does nothing — users stay authenticated forever
    public async Task<IActionResult> Logout()
    {
        // Removes the authentication cookie so [Authorize] stops recognizing the user
        // Without this: auth cookie remains — user appears still logged in
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Clears JWT and all session keys so ApiService can't reuse an old token
        // Without this: stale JWT stays in session — confusing state if they log in as someone else
        HttpContext.Session.Clear();

        // Returns user to login after sign-out
        // Without this: logout completes but browser stays on a protected page
        return RedirectToAction(nameof(Login));
    }
}
