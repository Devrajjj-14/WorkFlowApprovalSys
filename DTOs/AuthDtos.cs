// ── Imports ───────────────────────────────────────────────────────────────────
// Without this: [Required], [EmailAddress], [MinLength] attributes don't exist — model validation fails to compile
using System.ComponentModel.DataAnnotations;

// Without this: UserRole enum on RegisterRequest is unknown — registration DTO won't compile
using WorkflowApprovalApi.Models;

// ── Namespace ─────────────────────────────────────────────────────────────────
// Groups auth-related request/response shapes used by AuthController and AuthService
// Without this: AuthController cannot resolve RegisterRequest, LoginRequest, or AuthResponse
namespace WorkflowApprovalApi.DTOs;

// ── RegisterRequest — body for POST /api/auth/register ────────────────────────
// Incoming JSON from the registration form — validated before a User row is created
// Without this class: register endpoint has no typed binding target — request body is ignored
public class RegisterRequest
{
    // Must be non-empty — ASP.NET model validation returns 400 if missing
    // Without this: new users could register with no display name
    [Required]
    public string FullName { get; set; } = string.Empty;

    // Must be present and a valid email format — blocks malformed addresses at the API layer
    // Without this: invalid emails reach the database — login lookup becomes unreliable
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Minimum 6 characters — enforced before BCrypt hashing in AuthService
    // Without this: weak one-character passwords could be registered
    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    // Role chosen at signup (Admin, Manager, Designer, etc.) — bound as enum integer from JSON
    // Without this: every new user gets default enum value — role assignment on register breaks
    [Required]
    public UserRole Role { get; set; }
}

// ── LoginRequest — body for POST /api/auth/login ──────────────────────────────
// Email + password pair sent by the login form — never includes a role or token
// Without this class: login endpoint cannot bind credentials — every login returns 400
public class LoginRequest
{
    // Account email used to look up the User row in the database
    // Without this: AuthService cannot find which user is trying to sign in
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Plain-text password — compared against PasswordHash via BCrypt in AuthService
    // Without this: login always fails — there is nothing to verify against the hash
    [Required]
    public string Password { get; set; } = string.Empty;
}

// ── AuthResponse — returned after successful login or register ─────────────────
// Safe subset of User data plus JWT — PasswordHash is never included
// Without this class: auth endpoints have no consistent JSON shape — frontend deserialization breaks
public class AuthResponse
{
    // User's primary key — stored in JWT claims and used by the UI for "current user id"
    // Without this: frontend cannot identify which user is logged in after auth
    public int UserId { get; set; }

    // Display name shown in the navbar and audit labels after login
    // Without this: UI shows blank name even though auth succeeded
    public string FullName { get; set; } = string.Empty;

    // Email echoed back for display and session — not used again until next login
    // Without this: profile/header email field stays empty post-login
    public string Email { get; set; } = string.Empty;

    // Role as a string (e.g. "Manager") — easier for the frontend than parsing enum integers
    // Without this: role-based UI (admin menus, reviewer panels) cannot render correctly
    public string Role { get; set; } = string.Empty;

    // JWT bearer token — client stores this and sends Authorization header on protected calls
    // Without this: user is "logged in" in name only — no token means every API call gets 401
    public string Token { get; set; } = string.Empty;
}
