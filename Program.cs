
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WorkflowApprovalApi.Data;
using WorkflowApprovalApi.Helpers;
using WorkflowApprovalApi.Services.Implementations;
using WorkflowApprovalApi.Services.Interfaces;

// Create the WebApplication builder
// This is the starting point for configuring services, settings, and the app pipeline
var builder = WebApplication.CreateBuilder(args);

// Read database connection string from appsettings.json
// It looks for ConnectionStrings:DefaultConnection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register AppDbContext with Dependency Injection
// This tells .NET that AppDbContext will use MySQL database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,

        // Tell EF Core which MySQL server version is being used
        // Here MySQL version is 8.0.36
        new MySqlServerVersion(new Version(8, 0, 36))
    )
);

// Register TokenService in Dependency Injection
// TokenService is used to create JWT tokens after login
builder.Services.AddScoped<TokenService>();

// Register service interfaces with their implementation classes
// This means whenever IAuthService is needed, .NET will provide AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Whenever IProjectService is needed, .NET will provide ProjectService
builder.Services.AddScoped<IProjectService, ProjectService>();

// Whenever ITaskService is needed, .NET will provide TaskService
builder.Services.AddScoped<ITaskService, TaskService>();

// Whenever IFileService is needed, .NET will provide FileService
builder.Services.AddScoped<IFileService, FileService>();

// Whenever ICommentService is needed, .NET will provide CommentService
builder.Services.AddScoped<ICommentService, CommentService>();

// Whenever IApprovalService is needed, .NET will provide ApprovalService
builder.Services.AddScoped<IApprovalService, ApprovalService>();

// Read Jwt section from appsettings.json
// Example: Jwt:Key, Jwt:Issuer, Jwt:Audience
var jwtSettings = builder.Configuration.GetSection("Jwt");

// Read JWT secret key from configuration
// The ! means we are telling C# that this value will not be null
var jwtKey = jwtSettings["Key"]!;

// Add authentication system to the app
// JwtBearerDefaults.AuthenticationScheme means we are using JWT Bearer token authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Define rules for validating JWT tokens
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Validate that token was issued by the correct issuer
            // Issuer comes from appsettings.json
            ValidateIssuer = true,

            // Validate that token is meant for the correct audience
            // Audience comes from appsettings.json
            ValidateAudience = true,

            // Validate that token is not expired
            ValidateLifetime = true,

            // Validate that token was signed using the correct secret key
            ValidateIssuerSigningKey = true,

            // Expected issuer value
            // Token issuer must match this value
            ValidIssuer = jwtSettings["Issuer"],

            // Expected audience value
            // Token audience must match this value
            ValidAudience = jwtSettings["Audience"],

            // Secret key used to verify token signature
            // Encoding.UTF8.GetBytes converts string key into byte array
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            )
        };
    });

// Add authorization support
// This enables [Authorize] and [Authorize(Roles = "...")]
builder.Services.AddAuthorization();

// Add controller support
// This allows app to use controller classes like AuthController, ProjectsController, etc.
builder.Services.AddControllers();

// Required for Swagger/OpenAPI endpoint discovery
// It helps Swagger find available API endpoints
builder.Services.AddEndpointsApiExplorer();

// Add and configure Swagger
// Swagger is used to test APIs from browser
builder.Services.AddSwaggerGen(options =>
{
    // Basic Swagger document information
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        // API title shown in Swagger UI
        Title = "Workflow Approval API",

        // API version
        Version = "v1",

        // API description shown in Swagger UI
        Description = "Mini Dragonfly/Mediabox-style workflow approval backend"
    });

    // Add JWT Bearer authentication option in Swagger UI
    // This creates the Authorize button in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        // Header name where token will be sent
        Name = "Authorization",

        // Security type is HTTP
        Type = SecuritySchemeType.Http,

        // Scheme is bearer because JWT is sent as Bearer token
        Scheme = "bearer",

        // Token format is JWT
        BearerFormat = "JWT",

        // Token will be sent in request header
        In = ParameterLocation.Header,

        // Help text shown in Swagger UI
        Description = "Enter JWT token. Example: Bearer {your token}"
    });

    // Tell Swagger that secured APIs require Bearer token
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            // Refer to the Bearer security definition created above
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    // This refers to a security scheme
                    Type = ReferenceType.SecurityScheme,

                    // This must match the name used in AddSecurityDefinition
                    Id = "Bearer"
                }
            },

            // No specific scopes are required
            Array.Empty<string>()
        }
    });
});

// Build the app after all services are registered
var app = builder.Build();

// Enable Swagger JSON endpoint
// Example: /swagger/v1/swagger.json
app.UseSwagger();

// Enable Swagger UI page
// Usually available at /swagger
app.UseSwaggerUI();

// Enable serving static files from wwwroot folder
// Useful if uploaded files or public files need to be served
app.UseStaticFiles();

// Enable authentication middleware
// This checks JWT token and identifies the logged-in user
// Important: UseAuthentication should come before UseAuthorization
app.UseAuthentication();

// Enable authorization middleware
// This checks whether logged-in user has permission/role to access endpoint
app.UseAuthorization();

// Map controller routes
// This connects controller endpoints like api/auth/login, api/projects, etc.
app.MapControllers();

// Start the application
// The app keeps running and listens for HTTP requests
app.Run();