// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: RegisterRequest, LoginRequest, and AuthResponse are unknown — method signatures fail to compile
using WorkflowApprovalApi.DTOs;

// Groups all service contracts in one namespace so Program.cs can register them with AddScoped<Interface, Implementation>()
namespace WorkflowApprovalApi.Services.Interfaces;

// Contract for user authentication — register new accounts and log in existing users
// AuthService implements this; AuthController calls it for POST /api/auth/register and POST /api/auth/login
// Without this interface: DI can't bind IAuthService → AuthService — auth endpoints have no service to call
public interface IAuthService
{
    // Creates a new user account from email, password, name, and role
    // Returns a JWT token and user info on success — the frontend stores the token in session
    // Without this: the register endpoint has no method to call — new users can't sign up
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    // Validates email and password against the database and issues a JWT on success
    // Returns the same AuthResponse shape as RegisterAsync so the frontend handles both flows identically
    // Without this: the login endpoint has no method to call — existing users can't authenticate
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
