using Microsoft.EntityFrameworkCore;
using WorkflowApprovalApi.Data;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Models;
using WorkflowApprovalApi.Services.Interfaces;
using WorkflowApprovalApi.Helpers;
namespace WorkflowApprovalApi.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, TokenService tokenService, ILogger<AuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Registering new user with email {Email}", request.Email);

        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
        {
            _logger.LogWarning("Registration failed: email {Email} already exists", request.Email);
            throw new InvalidOperationException("Email is already registered.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} registered successfully", user.Id);
        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Login attempt for email {Email}", request.Email);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for email {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);
        return BuildAuthResponse(user);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
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
