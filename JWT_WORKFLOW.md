# JWT Complete Workflow — WorkflowApprovalApi

> **Layman explanation:** JWT (JSON Web Token) is like a **visitor ID badge** at an office.
> When you arrive (login), the receptionist gives you a badge with your name, department, and access level printed on it.
> Every time you enter a room (make an API request), the security guard reads your badge — they don't call the receptionist again.
> The badge expires after 2 hours and you need to get a new one.

---

## The Big Picture — All 6 Players

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│   appsettings.json   ──► TokenService  ──► AuthService             │
│         │                    │                  │                   │
│    (secret key,         (creates the        (calls TokenService     │
│     issuer,              JWT token)          after login/register)  │
│     audience,                │                  │                   │
│     expiry)                  └──────────────────┘                   │
│         │                           │                               │
│         │                     AuthController                        │
│         │                    (sends token to client)                │
│         │                                                           │
│         ▼                                                           │
│   Program.cs                                                        │
│  AddAuthentication()  ──► UseAuthentication()  ──► [Authorize]     │
│  (register rules)         (apply rules per         (protect the    │
│                            request)                 endpoints)      │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## PART 1 — The Config (Where Everything Starts)

### File: `appsettings.json` (lines 4–9)

```json
"Jwt": {
    "Key": "THIS_IS_A_DEMO_SECRET_KEY_FOR_WORKFLOW_APPROVAL_API_CHANGE_LATER",
    "Issuer": "WorkflowApprovalApi",
    "Audience": "WorkflowApprovalApiUsers",
    "ExpiryMinutes": 120
}
```

Think of this as the **rulebook** for the badge system.

| Setting | What it is | Real-life analogy |
|---|---|---|
| `Key` | Secret password used to sign and verify every token | The unique stamp/seal on the ID badge — only your company has it |
| `Issuer` | Who created the token | The company name printed on the badge |
| `Audience` | Who the token is meant for | "This badge is valid for Building A only" |
| `ExpiryMinutes` | How long the token is valid | Badge expires after 2 hours |

### ❌ If you delete this section:
- `TokenService` crashes immediately — it reads `jwtSettings["Key"]` and gets null
- `Program.cs` crashes — `jwtKey` will be null, `AddJwtBearer` throws a NullReferenceException
- **The entire API will not start**

---

## PART 2 — Token Creation (Making the Badge)

### File: `Helpers/TokenService.cs`

```csharp
public string GenerateToken(User user)
{
    // Step 1: Read the secret key from appsettings.json
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

    // Step 2: Use the key to create a "signing stamp"
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    // Step 3: Pack user information into the token (these are called CLAIMS)
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // User's ID number
        new Claim(ClaimTypes.Email, user.Email),                   // User's email
        new Claim(ClaimTypes.Name, user.FullName),                 // User's full name
        new Claim(ClaimTypes.Role, user.Role.ToString())           // User's role (Admin/Manager/etc)
    };

    // Step 4: Assemble the token with issuer, audience, claims, expiry, and signature
    var token = new JwtSecurityToken(
        issuer: jwtSettings["Issuer"],
        audience: jwtSettings["Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(120),
        signingCredentials: credentials);

    // Step 5: Convert it to a string — this is what gets sent to the client
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

> **Layman:** This is like the badge printing machine. You feed it the user's details, it stamps it with the secret seal, and prints out the badge string.

### What are CLAIMS?
Claims are pieces of information **packed inside the token**. Like fields printed on an ID badge:

| Claim | Value stored | Used for |
|---|---|---|
| `NameIdentifier` | `"5"` (user's DB id) | `GetCurrentUserId()` in every controller |
| `Email` | `"john@company.com"` | Identifying the user |
| `Name` | `"John Smith"` | Display name |
| `Role` | `"Manager"` | Role-based access control `[Authorize(Roles=...)]` |

### ❌ If you delete `TokenService`:
- `AuthService` will not compile — it injects `TokenService` in its constructor
- No token can ever be generated
- Login and Register will both fail
- **Nobody can ever authenticate**

### ❌ If you remove a Claim (e.g. remove the Role claim):
- `[Authorize(Roles = "Admin,Manager")]` will **always fail** for everyone
- All role-restricted endpoints return **403 Forbidden**

### ❌ If you remove the NameIdentifier claim:
- `GetCurrentUserId()` in every controller will throw a **NullReferenceException**
- Any endpoint that needs to know who the current user is will **crash**

---

## PART 3 — Login Flow (Handing Out the Badge)

### File: `Services/Implementations/AuthService.cs`

```csharp
public async Task<AuthResponse> LoginAsync(LoginRequest request)
{
    // Step 1: Find the user in the database by email
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

    // Step 2: Verify the password using BCrypt
    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        throw new UnauthorizedAccessException("Invalid email or password.");

    // Step 3: Build the response including the JWT token
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
        Token = _tokenService.GenerateToken(user)  // ← TokenService called here
    };
}
```

> **Layman:** The receptionist checks your name in the register (database), verifies your password, then calls the badge printer (TokenService) to make your badge and hands it to you.

### File: `Controllers/AuthController.cs`

```csharp
[AllowAnonymous]   // ← No badge needed to reach this endpoint
[HttpPost("login")]
public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
{
    var result = await _authService.LoginAsync(request);
    return Ok(result);  // ← Returns the token to the client
}
```

> `[AllowAnonymous]` means this door has no security guard — anyone can walk in. Makes sense because you can't be logged in before you log in.

### What the client receives:
```json
{
  "userId": 5,
  "fullName": "John Smith",
  "email": "john@company.com",
  "role": "Manager",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### ❌ If you remove `[AllowAnonymous]` from Login:
- The login endpoint now requires a JWT token to login
- **Nobody can ever log in** — classic chicken-and-egg problem

### ❌ If you delete `AuthService`:
- `AuthController` can't compile — it injects `IAuthService`
- No register or login possible
- **The entire auth system breaks**

---

## PART 4 — Token Registration (Teaching the App to Understand the Badge)

### File: `Program.cs` (lines 47–62)

```csharp
// Step 1: Tell the app to READ the secret key from config
var jwtKey = jwtSettings["Key"]!;

// Step 2: Register the JWT Bearer scheme
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,           // Check: was this token made by us?
            ValidateAudience = true,         // Check: is this token meant for us?
            ValidateLifetime = true,         // Check: has the token expired?
            ValidateIssuerSigningKey = true, // Check: was this token signed with our secret key?
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
```

> **Layman:** This is like training the security guards. You show them: "Here is what a valid badge looks like — our company name, the right department, not expired, and has our secret stamp. Reject anything else."

### The 4 Validation Checks Explained:

| Check | What it does | If you set it to false |
|---|---|---|
| `ValidateIssuer` | Confirms token was made by `WorkflowApprovalApi` | Any server's token would be accepted |
| `ValidateAudience` | Confirms token is for `WorkflowApprovalApiUsers` | Tokens meant for other apps would work |
| `ValidateLifetime` | Rejects expired tokens | Tokens would work forever, even after 2 hours |
| `ValidateIssuerSigningKey` | Verifies the secret key signature | Fake/tampered tokens would be accepted — **critical security risk** |

### ❌ If you delete `AddAuthentication` / `AddJwtBearer`:
- `UseAuthentication()` middleware has nothing to work with
- `HttpContext.User` is never populated
- `GetCurrentUserId()` crashes on every request
- `[Authorize]` has no authentication scheme to use — either allows everyone or blocks everyone
- **Complete auth failure**

---

## PART 5 — Token Activation Per Request (The Security Guard at Every Door)

### File: `Program.cs` (lines 113–114)

```csharp
app.UseAuthentication();   // ← Guard reads and validates the badge
app.UseAuthorization();    // ← Guard checks if this badge allows entry to THIS room
```

> **Layman:**
> - `UseAuthentication` = The guard at the building entrance who reads your badge and confirms it's real
> - `UseAuthorization` = The guard at each floor/room who checks if your badge gives you access to THIS specific area

### What `UseAuthentication()` does on EVERY request:
1. Reads the `Authorization: Bearer eyJ...` header from the incoming request
2. Decodes the JWT token
3. Validates it against the rules set in `AddJwtBearer` (Part 4)
4. If valid → populates `HttpContext.User` with all the claims (userId, email, role)
5. If invalid/missing → `HttpContext.User` stays empty (anonymous)

### What `UseAuthorization()` does:
1. Looks at `HttpContext.User` (populated by `UseAuthentication`)
2. Checks if the endpoint has `[Authorize]` or `[Authorize(Roles="...")]`
3. If the user's claims satisfy the requirement → lets the request through
4. If not → returns **401 Unauthorized** or **403 Forbidden**

### ❌ If you delete `UseAuthentication()`:
- JWT tokens are **never read or validated**
- `HttpContext.User` is always empty
- `GetCurrentUserId()` returns null → **NullReferenceException crash** on every protected endpoint
- Role checks never work
- **Everything breaks**

### ❌ If you delete `UseAuthorization()`:
- `[Authorize]` and `[Authorize(Roles="...")]` attributes are **completely ignored**
- Anyone — even without a token — can call any endpoint
- **Security is gone**

### ❌ If you swap the order (put UseAuthorization BEFORE UseAuthentication):
- `UseAuthorization` runs before the user identity is populated
- It sees an empty/anonymous user on every request
- **All protected endpoints return 401 for everyone**, even with a valid token

---

## PART 6 — Protecting Endpoints (The Door Locks)

### File: `Controllers/ApprovalsController.cs`

```csharp
[Authorize]                              // ← Class-level: ALL endpoints need a valid JWT
[ApiController]
[Route("api/approvals")]
public class ApprovalsController : ControllerBase
{
    [Authorize(Roles = "Admin,Manager,Designer")]  // ← Extra restriction on this specific endpoint
    [HttpPost]
    public async Task<ActionResult<ApprovalResponse>> Create(...)

    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]  // ← Different roles for approve/reject
    [HttpPut("{id}/approve")]
    public async Task<ActionResult<ApprovalResponse>> Approve(...)
}
```

### How `[Authorize]` layers work:

```
Request comes in
      ↓
[Authorize] on the CLASS → Is there a valid JWT token at all?
      ↓ YES
[Authorize(Roles="Admin,Manager,Designer")] on the METHOD → Does the token's Role claim match?
      ↓ YES
Controller action runs
```

> **Layman:**
> - `[Authorize]` on the class = "You must have a badge to enter this building"
> - `[Authorize(Roles="Admin,Manager")]` on a method = "Only managers and admins can enter this specific room"

### Role Matrix — Who Can Do What:

| Action | Admin | Manager | Designer | Reviewer | Client |
|---|---|---|---|---|---|
| Create Approval | ✅ | ✅ | ✅ | ❌ | ❌ |
| Approve / Reject | ✅ | ✅ | ❌ | ✅ | ✅ |
| Create Task | ✅ | ✅ | ❌ | ❌ | ❌ |
| Update Project Status | ✅ | ✅ | ❌ | ❌ | ❌ |
| View Projects/Tasks/Files | ✅ | ✅ | ✅ | ✅ | ✅ |

### ❌ If you remove `[Authorize]` from the class:
- All endpoints in that controller become **publicly accessible without any token**
- Anyone on the internet can create/approve/reject approvals

### ❌ If you remove `[Authorize(Roles="...")]` from a method:
- The role restriction is gone for that endpoint
- Any logged-in user regardless of role can call it

---

## PART 7 — Reading the User Identity (Using the Badge Info)

### In every controller: `GetCurrentUserId()`

```csharp
private int GetCurrentUserId()
{
    // User.FindFirstValue reads from HttpContext.User
    // which was populated by UseAuthentication() in the middleware
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    return int.Parse(userIdClaim!);
}
```

This is used in every action that needs to know WHO is making the request:

```csharp
// Example in ApprovalsController
var userId = GetCurrentUserId();
var result = await _approvalService.CreateAsync(request, userId);
// The approval is now linked to the correct user
```

> **Layman:** Instead of the user telling you "I am user #5", the system reads it directly from the validated badge. The user can't lie about who they are.

### ❌ If `GetCurrentUserId()` is removed and userId is taken from the request body instead:
- Users could **fake their identity** by sending any userId in the request
- A Designer could create an approval and claim it was made by an Admin
- **Major security vulnerability**

---

## PART 8 — The Frontend Side (UI Using the Badge)

### File: `WorkflowApprovalUI/Controllers/AuthController.cs`

After successful login, the UI stores the token in the **session**:
```csharp
HttpContext.Session.SetString("JwtToken", data.Token);    // Store the JWT
HttpContext.Session.SetString("UserName", data.FullName); // Store name for display
HttpContext.Session.SetString("UserRole", data.Role);     // Store role for UI logic
HttpContext.Session.SetInt32("UserId", data.UserId);      // Store userId
```

### File: `WorkflowApprovalUI/Services/ApiService.cs`

Every API call attaches the token from session:
```csharp
private HttpClient CreateClient()
{
    var client = _factory.CreateClient("API");
    var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
    if (!string.IsNullOrEmpty(token))
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return client;
}
```

> **Layman:** After the receptionist gives you the badge, you put it in your pocket (session). Every time you walk up to a door (API call), `ApiService` automatically takes the badge out of your pocket and holds it up for the guard.

### ❌ If session is cleared but cookie is not (or vice versa):
- UI thinks you are logged out (no JWT in session)
- But cookie still says you are logged in
- `CreateClient()` sends requests without a Bearer token → API returns 401
- **Confusing broken state** — user sees pages but gets errors on every action

---

## PART 9 — Complete Flow Diagram

```
USER TYPES email + password → clicks Login
        │
        ▼
AuthController (POST /api/auth/login)   [AllowAnonymous — no token needed]
        │
        ▼
AuthService.LoginAsync()
  → Checks email in DB
  → BCrypt verifies password
  → Calls TokenService.GenerateToken(user)
        │
        ▼
TokenService.GenerateToken()
  → Reads Key, Issuer, Audience, ExpiryMinutes from appsettings.json
  → Packs claims: userId, email, name, role
  → Signs with HMAC-SHA256 using the secret Key
  → Returns token string "eyJ..."
        │
        ▼
AuthResponse returned to client:
  { userId, fullName, email, role, token: "eyJ..." }
        │
        ▼  (UI stores token in Session)
        │
        │══════════════ LATER — User does something ══════════════
        │
        ▼
User clicks "Create Approval"
        │
        ▼
ApiService.CreateApprovalAsync()
  → CreateClient() reads JWT from session
  → Sets Authorization: Bearer eyJ... header
  → Sends POST /api/approvals
        │
        ▼
Request hits the server
        │
        ▼
ExceptionHandlingMiddleware  (outermost — catches any crash)
        │
        ▼
RequestLoggingMiddleware     (logs the incoming request)
        │
        ▼
UseAuthentication()          (reads the Bearer token from header)
  → Decodes the JWT
  → Validates: issuer ✓ audience ✓ not expired ✓ signature ✓
  → Populates HttpContext.User with claims
        │
        ▼
UseAuthorization()           (checks [Authorize] on the controller)
  → Sees [Authorize(Roles = "Admin,Manager,Designer")]
  → Reads Role claim from HttpContext.User = "Manager"
  → "Manager" is in the allowed list ✓
        │
        ▼
ApprovalsController.Create() runs
  → GetCurrentUserId() reads NameIdentifier claim → userId = 5
  → Calls ApprovalService.CreateAsync(request, 5)
  → Returns 200 OK with the created approval
```

---

## PART 10 — What Happens If You Delete Each Thing

| What you delete | Immediate consequence | End result |
|---|---|---|
| `appsettings.json` Jwt section | `TokenService` and `Program.cs` crash on startup | **App won't start** |
| `TokenService` | `AuthService` won't compile | **No tokens ever created, nobody can log in** |
| `AuthService` | `AuthController` won't compile | **No login/register** |
| `[AllowAnonymous]` on Login | Login requires a token to get a token | **Nobody can ever log in** |
| `AddAuthentication()` in Program.cs | No scheme registered for `UseAuthentication` | **All protected endpoints fail** |
| `AddJwtBearer()` in Program.cs | JWT tokens are never validated | **Tokens accepted or rejected randomly** |
| `UseAuthentication()` in Program.cs | `HttpContext.User` never populated | **All controllers crash on GetCurrentUserId()** |
| `UseAuthorization()` in Program.cs | `[Authorize]` attributes ignored | **All endpoints open to anyone** |
| `UseAuthentication` before `UseAuthorization` (swap order) | Auth runs before user identity is known | **All protected endpoints return 401** |
| Role claims in `TokenService` | `[Authorize(Roles="...")]` always fails | **All role-restricted endpoints return 403** |
| NameIdentifier claim in `TokenService` | `GetCurrentUserId()` throws NullReferenceException | **Every protected action crashes** |
| `GetCurrentUserId()` in controllers | Can't know who made the request | **Actions save data with wrong/null userId** |
| JWT from session in `ApiService` | API calls sent without Bearer token | **API returns 401 on every request from UI** |

---

## PART 11 — Things That Are NOT Linked to JWT

These things work completely independently of JWT:

| Thing | Why it's independent |
|---|---|
| **Serilog logging** | Runs before and after auth — logs everything regardless of token validity |
| **ExceptionHandlingMiddleware** | Catches crashes from anywhere — doesn't care about tokens |
| **RequestLoggingMiddleware** | Logs all requests — even unauthenticated ones |
| **Swagger UI** | Just documents and calls endpoints — the token is optional (you paste it manually) |
| **EF Core / Database** | Doesn't know about JWT — just stores and retrieves data |
| **BCrypt password hashing** | Happens before JWT is involved — just for verifying passwords |
| **File storage (`wwwroot/uploads`)** | Files are saved to disk — unrelated to who made the request |
| **Cookie auth in the UI** | The UI has its own separate cookie session — JWT is just stored inside it |

---

> **Final summary in one paragraph:**
>
> JWT starts in `appsettings.json` where the secret key, issuer, audience, and expiry are defined.
> When a user logs in, `AuthService` verifies their password, then calls `TokenService` which reads those settings, packs the user's id, email, name and role as claims, signs it with the secret key, and returns a token string.
> `AuthController` sends this token to the client.
> Meanwhile in `Program.cs`, `AddAuthentication + AddJwtBearer` taught the app how to validate a token using the same secret key and rules.
> When the next request comes in with that token in the header, `UseAuthentication()` middleware validates it and populates `HttpContext.User` with the claims.
> Then `UseAuthorization()` checks the `[Authorize]` attributes against those claims.
> Inside the controller, `GetCurrentUserId()` reads the userId from the claims.
> Delete any one link in this chain and the whole system breaks.
