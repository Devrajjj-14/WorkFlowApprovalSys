
// ── Imports ───────────────────────────────────────────────────────────────────
// Without System.Text: Encoding.UTF8.GetBytes() won't work — JWT key conversion fails
using System.Text;

// Without this: JwtBearerDefaults and AddJwtBearer() don't exist — JWT auth setup breaks entirely
using Microsoft.AspNetCore.Authentication.JwtBearer;

// Without this: AddDbContext() and UseMySql() don't exist — database cannot be registered
using Microsoft.EntityFrameworkCore;

// Without this: SymmetricSecurityKey and TokenValidationParameters don't exist — JWT validation fails
using Microsoft.IdentityModel.Tokens;

// Without this: OpenApiInfo, OpenApiSecurityScheme etc. don't exist — Swagger setup fails to compile
using Microsoft.OpenApi.Models;

// Without this: Log.Logger, Log.Information(), LoggerConfiguration don't exist — Serilog is unavailable
using Serilog;

// Without this: TelemetryConverter.Traces used in the Application Insights sink doesn't compile
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

// Without this: TelemetryConfiguration lives here — needed to pass the key to the AI sink
using Microsoft.ApplicationInsights.Extensibility;

// Without this: AppDbContext class is unknown here — AddDbContext<AppDbContext> fails to compile
using WorkflowApprovalApi.Data;

// Without this: TokenService class is unknown — AddScoped<TokenService>() fails to compile
using WorkflowApprovalApi.Helpers;

// Without this: UseCustomMiddleware() and the two middleware classes are unknown — custom middleware can't be added
using WorkflowApprovalApi.Middleware;

// Without this: AuthService, ProjectService etc. are unknown — all AddScoped<Interface, Implementation>() fail
using WorkflowApprovalApi.Services.Implementations;

// Without this: IAuthService, IProjectService etc. are unknown — all service interface registrations fail
using WorkflowApprovalApi.Services.Interfaces;

// Creates a minimal Serilog logger BEFORE the app fully starts
// Without this: if the app crashes during startup you see nothing — no error output at all
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()           // write startup logs to the terminal only
    .CreateBootstrapLogger();    // lightweight mode — full config loads later from appsettings.json

// Application Insights instrumentation key will be read from configuration below
string? instrumentationKey = null;

// ── Top-Level Try/Catch — wraps the entire application lifecycle ──────────────
// Without this: any unhandled startup or runtime crash exits silently with no log
try
{
    // First log line you see when running dotnet run
    // Without this: you just lose that startup confirmation message — nothing else breaks
    Log.Information("Starting Workflow Approval API");

    // Creates the application builder — the object used to register ALL services
    // Without this: nothing below works — builder doesn't exist and the app cannot be configured
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog Full Setup ────────────────────────────────────────────────────
    // The file sink is configured HERE in code — not in appsettings.json — because we need to
    // build the filename using the ACTUAL current date (e.g. "custom logger 2026-06-12.log").
    // If we put the path in appsettings.json Serilog treats {Date} as a literal string,
    // which is exactly what created the broken file called "custom logger {Date".
    //
    // DateTime.Now.ToString("yyyy-MM-dd") resolves RIGHT NOW at startup, so today's date
    // is permanently written into the filename for this process lifetime.
    // At midnight the app is typically restarted (or you can set rollingInterval below),
    // which triggers this line again and produces a new file for the new date.
    instrumentationKey = builder.Configuration["ApplicationInsights:InstrumentationKey"];
    var todayLogFile = $"logs/custom logger {DateTime.Now:yyyy-MM-dd}.log";

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)  // loads Console sink + MinimumLevel from appsettings.json
            .ReadFrom.Services(services)                    // lets Serilog use DI-registered enrichers
            .Enrich.FromLogContext()                        // adds contextual info (request ID, user etc.) to every log
            // ── File Sink ────────────────────────────────────────────────────
            // Writes every log entry to  logs/custom logger 2026-06-12.log  (or whatever today is)
            // rollingInterval: Day  → Serilog will start a NEW file the next day automatically
            //                         even if the app keeps running past midnight
            // retainedFileCountLimit: 30  → deletes files older than 30 days so disk never fills up
            // outputTemplate  → the exact text format written inside the file
            .WriteTo.File(
                path: todayLogFile, //this is dor today's actual date
                rollingInterval: RollingInterval.Day, // This is for new to be created for tommorow
                retainedFileCountLimit: 30, // keeps all the logs for 1 month then it start to delete
                outputTemplate: "{Timestamp:yyyy-MM-dd} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
            );

        // ── Application Insights Sink ─────────────────────────────────────────
        // If an InstrumentationKey is set in appsettings.json, every Serilog log entry is
        // ALSO forwarded to Azure Application Insights as a Trace telemetry item.
        // This means you see the same logs in the Azure portal under  Logs → traces
        // and can query them, set alerts, and build dashboards — without changing any
        // controller or service code at all.
        // Without this block: logs stay local only; nothing reaches the cloud.
        if (!string.IsNullOrWhiteSpace(instrumentationKey) &&
            instrumentationKey != "YOUR_INSTRUMENTATION_KEY_HERE")
        {
            var telemetryConfig = new TelemetryConfiguration
            {
                InstrumentationKey = instrumentationKey
            };
             // This line adds Application Insights as a destination for logs
            // Exactly like WriteTo.File() sends logs to a file, this sends them to Azure
            configuration.WriteTo.ApplicationInsights(
                telemetryConfiguration: telemetryConfig,
                telemetryConverter: TelemetryConverter.Traces
            );
        }
    });

    // ── Database Connection ───────────────────────────────────────────────────
    // Reads "ConnectionStrings:DefaultConnection" from appsettings.json
    // Value: "server=localhost;port=3306;database=workflow_approval_db;user=root;password=Root123@;"
    // Without this: connectionString is null — UseMySql() below fails or connects to nothing
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // Registers AppDbContext (Data/AppDbContext.cs) with the DI container as Scoped
    // Scoped = one instance per HTTP request — important for EF change tracking
    // UseMySql tells EF Core to use MySQL via the Pomelo driver with your connection string
    // MySqlServerVersion(8.0.36) tells Pomelo what MySQL version you have for correct SQL generation
    // Without this entire block: no service can get AppDbContext injected — every DB operation crashes
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 36))
        )
    );

    // ── Service Registrations (Dependency Injection) ──────────────────────────

    // Registers TokenService (Helpers/TokenService.cs) — generates JWT tokens after login
    // No interface used because it's a utility class with no need for swapping
    // Without this: AuthService constructor can't receive TokenService — app crashes on startup
    builder.Services.AddScoped<TokenService>();

    // Maps IAuthService (Services/Interfaces/IAuthService.cs)
    //   to AuthService (Services/Implementations/AuthService.cs)
    // AuthController depends on IAuthService — without this it cannot be constructed
    // Without this: register/login endpoints return 500 on every call
    builder.Services.AddScoped<IAuthService, AuthService>();

    // Maps IProjectService → ProjectService
    // ProjectsController depends on this — without it all project endpoints crash
    builder.Services.AddScoped<IProjectService, ProjectService>();

    // Maps ITaskService → TaskService
    // TasksController depends on this — without it all task endpoints crash
    builder.Services.AddScoped<ITaskService, TaskService>();

    // Maps IFileService → FileService
    // FilesController depends on this — without it all file upload/download endpoints crash
    builder.Services.AddScoped<IFileService, FileService>();

    // Maps ICommentService → CommentService
    // CommentsController depends on this — without it all comment endpoints crash
    builder.Services.AddScoped<ICommentService, CommentService>();

    // Maps IApprovalService → ApprovalService
    // ApprovalsController depends on this — without it all approval endpoints crash
    builder.Services.AddScoped<IApprovalService, ApprovalService>();

    // ── JWT Configuration ─────────────────────────────────────────────────────

    // Reads the entire "Jwt" section from appsettings.json (Key, Issuer, Audience, ExpiryMinutes)
    // Without this: jwtKey below is null — the signing key cannot be built — JWT validation breaks
    var jwtSettings = builder.Configuration.GetSection("Jwt");

    // Extracts just the secret key string used to sign and verify JWT tokens
    // This same key is used in Helpers/TokenService.cs to SIGN tokens
    // Here it is used to VERIFY them — both must match or every token fails validation
    // The ! means "trust me this won't be null" — without this: IssuerSigningKey below has no value
    var jwtKey = jwtSettings["Key"]!;

    // Registers JWT Bearer as the default authentication scheme
    // This means requests with "Authorization: Bearer <token>" are validated as JWTs
    // Without AddAuthentication: UseAuthentication() below does nothing — all [Authorize] stops working
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Checks the "iss" claim in the token must equal "WorkflowApprovalApi" from appsettings.json
                // Without this: tokens from any issuer are accepted — security risk
                ValidateIssuer = true,

                // Checks the "aud" claim must equal "WorkflowApprovalApiUsers" from appsettings.json
                // Without this: tokens meant for other apps would be accepted here
                ValidateAudience = true,

                // Checks the "exp" claim — token must not be expired (120 mins from issue time)
                // Without this: expired tokens work forever — major security risk
                ValidateLifetime = true,

                // Verifies the cryptographic signature using the secret key
                // This prevents forged tokens — most important check of all
                // Without this: anyone can create a fake token and gain access
                ValidateIssuerSigningKey = true,

                // The expected issuer value — must match what TokenService.cs puts in the token
                // If this doesn't match the token's issuer claim, validation fails with 401
                ValidIssuer = jwtSettings["Issuer"],

                // The expected audience value — must match what TokenService.cs puts in the token
                // If this doesn't match the token's audience claim, validation fails with 401
                ValidAudience = jwtSettings["Audience"],

                // Converts the secret key string to bytes and wraps it as a signing key
                // This is the DIRECT LINK to TokenService.cs — same key used to sign tokens there
                // Without this: the validator has no key to check signatures against — all tokens fail
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                )
            };
        });

    // Registers the authorization system that reads [Authorize] and [Authorize(Roles="...")] attributes
    // Authentication = who are you. Authorization = what can you do. These are separate.
    // Without this: role restrictions don't work — a Client could delete projects
    builder.Services.AddAuthorization();

    // ── Application Insights Integration ──────────────────────────────────────
    // Registers Application Insights services for telemetry collection
    // This enables monitoring of requests, dependencies, exceptions, and performance metrics
    // Without this: Application Insights cannot track application behavior and performance
    // Note: Use AddApplicationInsightsTelemetry() to auto-discover InstrumentationKey from config
    builder.Services.AddApplicationInsightsTelemetry();

    // Registers all API controllers and adds JsonStringEnumConverter globally
    // JsonStringEnumConverter allows enums to be sent as strings ("Designer") not just integers (2)
    // This was the fix for the registration bug where Role: "Designer" was rejected
    // Without AddControllers: no endpoints exist. Without converter: role string on register fails
    builder.Services.AddControllers()
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    // ── CORS Policy ───────────────────────────────────────────────────────────
    // CORS = Cross-Origin Resource Sharing
    // The browser blocks requests from localhost:5001 to localhost:5000 (different ports = different origins)
    // This policy explicitly allows the frontend to call the backend and load images from it
    // AllowCredentials() lets the browser send auth headers cross-origin
    // Without this: image thumbnails fail to load, some frontend API calls may be blocked by the browser
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontendPolicy", policy =>
            policy.WithOrigins("http://localhost:5001")  // only allow requests from the frontend
                  .AllowAnyHeader()                      // any request header is allowed
                  .AllowAnyMethod()                      // GET, POST, PUT, DELETE etc. all allowed
                  .AllowCredentials());                  // allow cookies and auth headers cross-origin
    });

    // Required by Swagger to discover all your API endpoints, routes, and parameter types
    // Without this: Swagger has nothing to document — the UI would be empty
    builder.Services.AddEndpointsApiExplorer();

    // ── Swagger UI Configuration ──────────────────────────────────────────────
    // Configures the interactive API documentation UI at http://localhost:5000/swagger
    // Without the entire AddSwaggerGen block: Swagger UI does not exist
    builder.Services.AddSwaggerGen(options =>
    {
        // Creates the v1 documentation group — the title and description shown at top of Swagger UI
        // Without this: Swagger has no document to show
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Workflow Approval API",
            Version = "v1",
            Description = "Mini Dragonfly/Mediabox-style workflow approval backend"
        });

        // Adds the "Authorize" button to the Swagger UI so you can paste your JWT and test protected endpoints
        // Without this: you cannot test any [Authorize] endpoint from Swagger — they all return 401
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT token. Example: Bearer {your token}"
        });

        // Applies the Bearer requirement to all endpoints — adds padlock icon to every route in Swagger UI
        // Without this: the auth button exists but the token is not actually sent with requests
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ── Build the App ─────────────────────────────────────────────────────────
    // This is the dividing line — seals the DI container and creates the runnable application
    // Everything before this was configuration. Everything after this is the request pipeline.
    // Without this: app doesn't exist — nothing below compiles
    var app = builder.Build();

    // ── Middleware Pipeline ───────────────────────────────────────────────────
    // ORDER MATTERS — requests go through each step top to bottom

    // Activates ExceptionHandlingMiddleware and RequestLoggingMiddleware (Middleware/MiddlewareExtensions.cs)
    // ExceptionHandling wraps everything — catches crashes and returns clean JSON errors
    // RequestLogging logs every request with method, path, status code, and duration in ms
    // Without this: unhandled exceptions return HTML crash pages and no request logging happens
    app.UseCustomMiddleware();

    // Applies the "FrontendPolicy" CORS rules defined above to every request
    // Must come before UseAuthentication — CORS headers must be added before auth processing
    // Without this: the CORS policy is registered but never actually applied
    app.UseCors("FrontendPolicy");

    // Serves the raw Swagger JSON spec at /swagger/v1/swagger.json
    // Without this: the JSON spec doesn't exist — Swagger UI has nothing to load
    app.UseSwagger();

    // Serves the interactive Swagger HTML UI at http://localhost:5000/swagger
    // RoutePrefix = "swagger" means the UI is at /swagger not at the root /
    // Without this: the interactive UI doesn't exist even if the JSON spec does
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Workflow Approval API v1");
        c.RoutePrefix = "swagger";
    });

    // Creates a minimal endpoint: visiting http://localhost:5000 redirects to /swagger
    // AllowAnonymous means no JWT required for this redirect — anyone can reach it
    // Without this: visiting the root URL returns a 404 instead of redirecting to Swagger
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

    // Enables serving static files from the wwwroot/ folder
    // Required for the file download endpoint — physical files live in wwwroot/uploads/
    // Without this: file downloads return 404 even though the files exist on disk
    app.UseStaticFiles();

    // Reads the JWT from the Authorization header, validates it, and populates HttpContext.User
    // This is WHO ARE YOU — establishes the user's identity for every request
    // Must come before UseAuthorization
    // Without this: HttpContext.User is always empty — GetCurrentUserId() crashes, all [Authorize] fails
    app.UseAuthentication();

    // Checks [Authorize] and [Authorize(Roles="...")] attributes against HttpContext.User
    // This is WHAT CAN YOU DO — enforces permissions after identity is established
    // Without this: role restrictions don't work — anyone can call any endpoint
    app.UseAuthorization();

    // Scans all [ApiController] classes and maps their [Route] and [Http*] attributes to URLs
    // This is what makes POST /api/auth/login route to AuthController.Login()
    // Without this: no endpoints exist — every request returns 404
    app.MapControllers();

    // Starts the web server and begins listening for requests on http://localhost:5000
    // await means this runs until the app is stopped (Ctrl+C)
    // Without this: the app builds and immediately exits — never serves anything
    await app.RunAsync();
}
// ── Global Error Handler ──────────────────────────────────────────────────────
// Catches any unhandled exception during the entire app lifecycle (startup crashes, etc.)
// Without this: a startup crash exits silently — you see nothing, no error message
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
// ── Cleanup ───────────────────────────────────────────────────────────────────
// Runs whether the app exits normally or crashes
// Serilog buffers log entries — this forces any buffered entries to be written to disk before exit
// Without this: the last few log entries before a crash may never reach your log file
finally
{
    await Log.CloseAndFlushAsync();
}
