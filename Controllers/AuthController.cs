using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Services.Interfaces;
using WorkflowApprovalApi.Services.Implementations;
using System.Security.Claims;

namespace WorkflowApprovalApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly TokenBlacklistService _blacklistService;

    public AuthController(IAuthService authService, TokenBlacklistService blacklistService)
    {
        _authService = authService;
        _blacklistService = blacklistService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
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
