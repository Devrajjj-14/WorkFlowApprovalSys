// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: JwtSecurityToken, JwtSecurityTokenHandler don't exist — token creation fails to compile
using System.IdentityModel.Tokens.Jwt;

// Without this: Claim, ClaimTypes don't exist — JWT payload claims can't be built
using System.Security.Claims;

// Without this: Encoding.UTF8.GetBytes() doesn't exist — secret key can't be converted to bytes
using System.Text;

// Without this: SymmetricSecurityKey, SigningCredentials, SecurityAlgorithms don't exist — signing breaks
using Microsoft.IdentityModel.Tokens;

// Without this: User model is unknown — GenerateToken(User user) fails to compile
using WorkflowApprovalApi.Models;

namespace WorkflowApprovalApi.Helpers;

// TokenService creates JWT tokens after successful login or registration
// Registered in Program.cs as AddScoped<TokenService>() — AuthService injects it
// Without this class: login/register succeed in the DB but return no token — frontend can't authenticate
public class TokenService
{
    // Holds appsettings.json values — Jwt:Key, Issuer, Audience, ExpiryMinutes
    // Without this field: GenerateToken can't read JWT settings — key and expiry are unavailable
    private readonly IConfiguration _configuration;

    // DI injects IConfiguration automatically — same config source as Program.cs JWT validation
    // Without this constructor: TokenService can't be constructed — AuthService startup fails
    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // ── Generate JWT for a logged-in user ───────────────────────────────────
    // Builds a signed token with user id, email, name, and role claims
    // Program.cs validates tokens using the SAME Key, Issuer, and Audience — they must match exactly
    // Without this method: AuthService has nothing to return after login — every auth call fails
    public string GenerateToken(User user)
    {
        // Reads the entire "Jwt" section from appsettings.json
        // Without this: jwtSettings is empty — Key/Issuer/Audience below are all null
        var jwtSettings = _configuration.GetSection("Jwt");

        // Converts the secret key string to bytes and wraps it for HMAC-SHA256 signing
        // Same key as IssuerSigningKey in Program.cs — mismatch means every token fails validation
        // Without this: credentials can't be created — token has no signature
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

        // Pairs the key with HMAC-SHA256 — the algorithm used to sign the token
        // Without this: JwtSecurityToken has no signingCredentials — token is invalid
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims embedded inside the JWT — read by backend via HttpContext.User after validation
        // NameIdentifier = user id (used by GetCurrentUserId() in controllers)
        // Email, Name, Role = available for authorization and display without another DB hit
        // Without this array: token is empty — [Authorize] passes but identity has no user id or role
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        // Assembles the JWT: issuer, audience, claims, expiry, and signature
        // issuer/audience must match ValidIssuer/ValidAudience in Program.cs or validation returns 401
        // expires uses ExpiryMinutes from config (e.g. 120) from UTC now
        // Without this: no token object exists — WriteToken below has nothing to serialize
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiryMinutes"]!)),
            signingCredentials: credentials);

        // Serializes the JwtSecurityToken to the compact string sent to the client ("eyJhbG...")
        // Without this: AuthService can't return a string — login response has no token field value
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
