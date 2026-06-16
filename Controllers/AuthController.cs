// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: [AllowAnonymous] and [Authorize] attributes don't exist — auth bypass on register/login breaks
using Microsoft.AspNetCore.Authorization;

// Without this: [ApiController], [Route], [HttpPost], ControllerBase, ActionResult don't exist — controller won't compile
using Microsoft.AspNetCore.Mvc;

// Without this: RegisterRequest, LoginRequest, AuthResponse are unknown — request/response types fail to compile
using WorkflowApprovalApi.DTOs;

// Without this: IAuthService is unknown — AuthController can't be constructed via DI
using WorkflowApprovalApi.Services.Interfaces;
using WorkflowApprovalApi.Services.Implementations;
using System.Security.Claims;

// Without this: AuthController lives in the global namespace — breaks project structure and DI discovery
namespace WorkflowApprovalApi.Controllers;

// Marks this class as an API controller — enables automatic model validation and binding error responses
// Without this: invalid JSON bodies may not return clean 400 errors — model binding behaves differently
[ApiController]

// Sets the base URL prefix for all actions in this controller to /api/auth
// Without this: routes default to /AuthController — register/login URLs won't match the frontend
[Route("api/auth")]

// AuthController handles user registration and login — the only endpoints that don't require a JWT
// Without this class: POST /api/auth/register and POST /api/auth/login don't exist — nobody can sign in
public class AuthController : ControllerBase
{
    // Holds the injected auth service that talks to the database and generates JWT tokens
    // Without this field: Register and Login have no way to call business logic — every action crashes
    private readonly IAuthService _authService;
    private readonly TokenBlacklistService _blacklistService;

    public AuthController(IAuthService authService, TokenBlacklistService blacklistService)
    {
        // Stores the service instance for use in Register and Login action methods
        // Without this assignment: _authService stays null — NullReferenceException on every request
        _authService = authService;
        _blacklistService = blacklistService;
    }

    // ── Register ────────────────────────────────────────────────────────────────

    // Allows this endpoint to be called without a JWT — new users don't have a token yet
    // Without this: register requests require auth — chicken-and-egg problem, nobody can sign up
    [AllowAnonymous]

    // Maps POST /api/auth/register to this method
    // Without this: the register URL doesn't exist — frontend registration form gets 404
    [HttpPost("register")]

    // Creates a new user account and returns a JWT plus user info
    // Without this method: registration is impossible — the app has no way to add users
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        // Wraps service call in try/catch so duplicate-email errors become clean 400 responses
        // Without this try: unhandled InvalidOperationException bubbles up as a 500 HTML error page
        try
        {
            // Delegates to AuthService — hashes password, saves user to DB, generates JWT
            // Without this: no user is created — register always does nothing
            var result = await _authService.RegisterAsync(request);

            // Returns HTTP 200 with the AuthResponse JSON (token, userId, email, role)
            // Without this: client gets empty 204 — frontend can't store the JWT or show success
            return Ok(result);
        }
        // Catches business-rule failures like "email already exists"
        // Without this catch: duplicate registration crashes with 500 instead of a readable error
        catch (InvalidOperationException ex)
        {
            // Returns HTTP 400 with { message: "..." } — frontend can show the error to the user
            // Without this: client can't tell WHY registration failed
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Login ───────────────────────────────────────────────────────────────────

    // Allows login without an existing JWT — users authenticate here to GET their first token
    // Without this: login requires auth — nobody can ever obtain a token
    [AllowAnonymous]

    // Maps POST /api/auth/login to this method
    // Without this: the login URL doesn't exist — frontend login form gets 404
    [HttpPost("login")]

    // Validates email/password and returns a JWT plus user info
    // Without this method: login is impossible — no user can access protected endpoints
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        // Wraps service call in try/catch so bad credentials become clean 401 responses
        // Without this try: UnauthorizedAccessException becomes an unhandled 500 error
        try
        {
            // Delegates to AuthService — verifies password hash, generates fresh JWT
            // Without this: credentials are never checked — login always fails silently or crashes
            var result = await _authService.LoginAsync(request);

            // Returns HTTP 200 with AuthResponse — frontend stores token in session for later API calls
            // Without this: client gets no token — all subsequent protected requests return 401
            return Ok(result);
        }
        // Catches wrong password or unknown email from AuthService
        // Without this catch: failed login crashes with 500 instead of a proper 401 Unauthorized
        catch (UnauthorizedAccessException ex)
        {
            // Returns HTTP 401 with { message: "..." } — frontend shows "invalid credentials"
            // Without this: client can't distinguish bad credentials from a server error
            return Unauthorized(new { message = ex.Message });
        }
    }

    // Logout endpoint — extracts the current token and blacklists it.
    // Even though the JWT is still technically valid, it will be blocked by
    // TokenBlacklistMiddleware on every subsequent request.
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            _blacklistService.Blacklist(token);
        }
        return Ok(new { message = "Logged out successfully. Token has been revoked." });
    }
}
