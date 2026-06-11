// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: CookieAuthenticationDefaults and AddCookie() don't exist
// Cookie authentication is how the frontend remembers that a user is logged in across page loads
using Microsoft.AspNetCore.Authentication.Cookies;

// Without this: ApiService class is unknown here — AddScoped<ApiService>() fails to compile
// ApiService (Services/ApiService.cs) is the only class that calls the backend API
using WorkflowApprovalUI.Services;

// ── Application Builder ───────────────────────────────────────────────────────
// Creates the application builder — the object used to register all services before the app starts
// args passes any command-line arguments like --urls http://localhost:5001
// Without this: nothing below works — builder doesn't exist
var builder = WebApplication.CreateBuilder(args);

// ── MVC Setup ─────────────────────────────────────────────────────────────────
// Registers support for Controllers AND Views (Razor .cshtml files)
// The backend uses AddControllers() (API only, no views)
// The frontend needs views so it uses AddControllersWithViews()
// Without this: no controllers, no views — the entire MVC app doesn't function
builder.Services.AddControllersWithViews();

// Registers IHttpContextAccessor in the DI container
// This allows services (specifically ApiService) to access the current HTTP request
// ApiService uses it to read the JWT token from the session: HttpContext.Session.GetString("JwtToken")
// Without this: ApiService cannot read the session — it can't attach the JWT to backend calls
// Result: every backend API call gets a 401 because no token is sent
builder.Services.AddHttpContextAccessor();

// Registers ApiService (Services/ApiService.cs) as a Scoped service
// Scoped = one instance per HTTP request
// Every controller gets an ApiService injected — it's the single connection point to the backend
// Without this: every controller constructor fails to receive ApiService — app crashes on startup
builder.Services.AddScoped<ApiService>();

// ── Cookie Authentication ─────────────────────────────────────────────────────
// Sets up Cookie-based authentication for the MVC frontend
// This is separate from the backend's JWT auth — it's only for this frontend app
// After login, an encrypted cookie is written to the browser
// On every subsequent request, ASP.NET reads that cookie to know who the user is
// Without AddAuthentication: UseAuthentication() below does nothing — [Authorize] stops working
// Result: every protected page redirects to login, even for logged-in users
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // If a user hits a protected page without being logged in, redirect here
        // Without this: unauthenticated users get a 401 error page instead of being redirected to login
        options.LoginPath = "/Auth/Login";

        // The URL that clears the cookie when logging out
        // Without this: the logout endpoint can't clear the auth cookie properly
        options.LogoutPath = "/Auth/Logout";

        // Cookie expires after 2 hours of inactivity — user gets logged out automatically
        // Without this: the cookie never expires — users stay logged in forever
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

// ── Session ───────────────────────────────────────────────────────────────────
// Session stores data server-side tied to the user's browser session
// Your app stores the JWT token in session: Session.SetString("JwtToken", token)
// ApiService reads it from session to attach to every backend API call
// Without AddSession: session doesn't work — JWT is never stored — all backend calls fail with 401
builder.Services.AddSession(options =>
{
    // Session expires after 2 hours of inactivity — matches the JWT expiry
    // Without this: session never expires — old JWT tokens could persist in session
    options.IdleTimeout = TimeSpan.FromHours(2);

    // HttpOnly = true means JavaScript cannot read the session cookie — protects against XSS attacks
    // Without this: browser scripts could read the session ID — security vulnerability
    options.Cookie.HttpOnly = true;

    // IsEssential = true means this cookie is stored even if the user hasn't accepted cookies (GDPR)
    // Without this: session may not work if the user hasn't given cookie consent
    options.Cookie.IsEssential = true;
});

// ── HttpClient Registration ───────────────────────────────────────────────────
// Registers a named HttpClient called "API" pointed at the backend URL
// ApiService uses _factory.CreateClient("API") to get this client
// BaseAddress is read from appsettings.json under "ApiBaseUrl" — defaults to http://localhost:5000
// Without this: ApiService has no HttpClient to make requests with — every backend call fails
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000");
});

// ── Build the App ─────────────────────────────────────────────────────────────
// Seals all service registrations and creates the runnable application
// Everything before this was configuration. Everything after this is the request pipeline.
// Without this: app doesn't exist — nothing below compiles
var app = builder.Build();

// ── Error Handling (Production Only) ─────────────────────────────────────────
// In production (not Development), unhandled exceptions redirect to /Home/Error
// In Development, the detailed exception page is shown instead (better for debugging)
// Without this block: production crashes show raw error details to users — security risk
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// ── Middleware Pipeline ───────────────────────────────────────────────────────
// ORDER MATTERS — every request goes through these steps top to bottom

// Serves CSS, JS, and image files from the wwwroot/ folder
// This is what delivers wwwroot/css/app.css and wwwroot/js/app.js to the browser
// Without this: no styling, no JavaScript — the app looks broken and modals don't work
app.UseStaticFiles();

// Enables the routing system so ASP.NET can match URLs to controllers
// Must come before UseSession, UseAuthentication, and UseAuthorization
// Without this: URL routing doesn't work — all requests return 404
app.UseRouting();

// Activates session middleware — must come AFTER UseRouting and BEFORE UseAuthentication
// Session reads/writes the server-side session store for every request
// Without this: session is registered but never activated — Session.GetString() always returns null
// Result: JWT is never read from session — all backend API calls fail with 401
app.UseSession();

// Reads the authentication cookie set during login and populates HttpContext.User
// This is WHO ARE YOU — establishes user identity for every request
// Must come before UseAuthorization
// Without this: HttpContext.User is always empty — all [Authorize] attributes redirect to login
app.UseAuthentication();

// Checks [Authorize] attributes on controllers against HttpContext.User
// This is WHAT CAN YOU DO — enforces role-based permissions
// Without this: role restrictions stop working — any logged-in user can access any page
app.UseAuthorization();

// ── Default Route ─────────────────────────────────────────────────────────────
// Maps URLs to controllers using the pattern {controller}/{action}/{id?}
// Default = HomeController.Index() when visiting the root /
// HomeController.Index() immediately redirects to /Projects — so / shows the projects list
// Without this: no MVC routes exist — every request returns 404
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Starts the web server and begins listening for requests on http://localhost:5001
// Unlike the backend which uses await app.RunAsync(), this uses the synchronous Run()
// Without this: the app builds and immediately exits — never serves anything
app.Run();
