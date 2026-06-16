// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: AnyAsync, FirstOrDefaultAsync, Add, SaveChangesAsync don't exist — DB operations fail to compile
using Microsoft.EntityFrameworkCore;

// Without this: AppDbContext class is unknown — constructor injection and _context fail to compile
using WorkflowApprovalApi.Data;

// Without this: RegisterRequest, LoginRequest, AuthResponse are unknown — method signatures fail to compile
using WorkflowApprovalApi.DTOs;

// Without this: User model class is unknown — new User { ... } fails to compile
using WorkflowApprovalApi.Models;

// Without this: IAuthService interface is unknown — AuthService can't implement the contract
using WorkflowApprovalApi.Services.Interfaces;

// Without this: TokenService class is unknown — JWT generation in BuildAuthResponse fails to compile
using WorkflowApprovalApi.Helpers;

namespace WorkflowApprovalApi.Services.Implementations;

// AuthService handles user registration and login
// Called by AuthController — validates credentials, hashes passwords, returns JWT tokens
// Without this class: register/login endpoints have no business logic — DI registration fails at startup
public class AuthService : IAuthService
{
    // EF Core database context — used to read/write Users table
    // Without this field: no database access — registration and login cannot work
    private readonly AppDbContext _context;

    // Generates JWT tokens after successful register/login
    // Without this field: BuildAuthResponse can't create a Token — clients get no auth token
    private readonly TokenService _tokenService;

    // Structured logger for registration/login events and failures
    // Without this field: no auth audit trail in logs — harder to debug failed logins
    private readonly ILogger<AuthService> _logger;

    // Constructor — DI injects database, token helper, and logger
    // Without this constructor: AuthService can't be constructed — app crashes on startup
    public AuthService(AppDbContext context, TokenService tokenService, ILogger<AuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────────
    // Creates a new user account with hashed password and returns auth response with JWT
    // Without this: POST /api/auth/register has nothing to call — registration always fails
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Log that a registration attempt started — helps trace duplicate-email issues
        // Without this: registration attempts are invisible in logs
        _logger.LogInformation("Registering new user with email {Email}", request.Email);

        // Check if email is already taken — prevents duplicate accounts
        // Without this: two users could register with the same email — login ambiguity and data corruption
        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);

        // If email exists, reject registration with a clear error
        // Without this if block: duplicate emails would be inserted — unique constraint may crash instead
        if (emailExists)
        {
            _logger.LogWarning("Registration failed: email {Email} already exists", request.Email);
            throw new InvalidOperationException("Email is already registered.");
        }

        // Build the new User entity from the request
        // Password is hashed with BCrypt — plain password is never stored
        // Without this: no user record to save — registration completes with nothing in DB
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        // Stage the user for insert and persist to MySQL
        // Without Add: EF doesn't track the entity — SaveChangesAsync writes nothing
        // Without SaveChangesAsync: user stays in memory only — not in database
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Log success with the new auto-generated user Id
        // Without this: successful registrations aren't visible in logs
        _logger.LogInformation("User {UserId} registered successfully", user.Id);

        // Return JWT + user profile — same shape as login response
        // Without this return: controller gets nothing — client sees empty 200 or error
        return BuildAuthResponse(user);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────
    // Validates email/password and returns auth response with JWT
    // Without this: POST /api/auth/login has nothing to call — login always fails
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Log login attempt — useful for security auditing
        // Without this: failed login storms are hard to detect in logs
        _logger.LogInformation("Login attempt for email {Email}", request.Email);

        // Look up user by email — null if not registered
        // Without this: we can't verify credentials against stored hash
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        // Reject if user missing OR password doesn't match BCrypt hash
        // Without this if block: wrong passwords would still get a JWT — major security hole
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for email {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Log successful login with user Id
        // Without this: successful logins aren't traceable in logs
        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        // Return JWT + user profile
        // Without this return: client never receives token — can't call protected endpoints
        return BuildAuthResponse(user);
    }

    // ── BuildAuthResponse (private helper) ────────────────────────────────────
    // Maps User entity to AuthResponse DTO and attaches a fresh JWT
    // Without this: RegisterAsync and LoginAsync would duplicate token-building logic
    private AuthResponse BuildAuthResponse(User user)
    {
        // Build the response object sent back to the client
        // Token comes from TokenService — same key/issuer as Program.cs JWT validation
        // Without this: API returns no userId, role, or token — frontend can't authenticate
        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Token = _tokenService.GenerateToken(user)
        };
    }
}
