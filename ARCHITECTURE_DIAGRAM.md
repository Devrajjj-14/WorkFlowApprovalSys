# WorkFlowApprovalSys — Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              BROWSER (User)                                             │
│                                                                                         │
│   Opens: http://localhost:5001                                                          │
└─────────────────────────┬───────────────────────────────────────────────────────────────┘
                          │  HTTP Request (with .AspNetCore.Cookies)
                          ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                    WorkflowApprovalUI  (localhost:5001)                                 │
│                    Folder: WorkflowApprovalUI/                                          │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│  │  Program.cs                                                                     │   │
│  │  - AddAuthentication(Cookie)     ← protects UI pages                           │   │
│  │  - AddSession()                  ← stores JWT in server memory                 │   │
│  │  - AddHttpClient("API")          ← base URL = http://localhost:5000             │   │
│  │  - UseSession()                                                                 │   │
│  │  - UseAuthentication()           ← validates cookie on every request           │   │
│  │  - UseAuthorization()            ← checks [Authorize] on controllers           │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                         │
│  ┌──────────────────────────────────────┐  ┌──────────────────────────────────────┐   │
│  │  Views/Auth/                         │  │  Views/Projects/                     │   │
│  │  ├── Login.cshtml                    │  │  ├── Index.cshtml                    │   │
│  │  │   @model LoginViewModel           │  │  │   @model List<ProjectResponse>    │   │
│  │  │   ViewBag.Error                   │  │  │   TempData["Success"]             │   │
│  │  │   TempData["Success"]             │  │  │                                  │   │
│  │  └── Register.cshtml                 │  │  └── Detail.cshtml                   │   │
│  │      @model RegisterViewModel        │  │      @model ProjectDetailViewModel   │   │
│  │      ViewBag.Error                   │  │      Session["UserRole"]             │   │
│  └──────────────────────────────────────┘  └──────────────────────────────────────┘   │
│                    │                                        │                           │
│                    ▼                                        ▼                           │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │  Controllers/   (Folder: WorkflowApprovalUI/Controllers/)                        │  │
│  │                                                                                  │  │
│  │  AuthController.cs          ProjectsController.cs    TasksController.cs         │  │
│  │  - Login()                  - Index()                - Create()                 │  │
│  │    SignInAsync(Cookie) ←    - Detail(int id)         - UpdateStatus()           │  │
│  │    Session.Set(JwtToken)    - Create()                                          │  │
│  │    Session.Set(UserRole)    - UpdateStatus()         ApprovalsController.cs     │  │
│  │  - Register()                                        - Create()                 │  │
│  │  - Logout()                 FilesController.cs       - Approve()                │  │
│  │    SignOutAsync()           - Upload()               - Reject()                 │  │
│  │    Session.Clear()                                   - RequestChanges()         │  │
│  │                             CommentsController.cs                               │  │
│  │                             - Create()               HomeController.cs          │  │
│  │                                                      - redirects to Projects    │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
│                    │                                                                    │
│                    ▼                                                                    │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │  Services/ApiService.cs   (Folder: WorkflowApprovalUI/Services/)                 │  │
│  │                                                                                  │  │
│  │  CreateClient()                                                                  │  │
│  │  → reads JWT from Session["JwtToken"]                                           │  │
│  │  → sets Authorization: Bearer eyJ... on every API call                          │  │
│  │                                                                                  │  │
│  │  LoginAsync()         → POST /api/auth/login                                    │  │
│  │  RegisterAsync()      → POST /api/auth/register                                 │  │
│  │  GetProjectsAsync()   → GET  /api/projects                                      │  │
│  │  GetProjectAsync(id)  → GET  /api/projects/{id}                                 │  │
│  │  CreateProjectAsync() → POST /api/projects                                      │  │
│  │  GetTasksAsync(id)    → GET  /api/projects/{id}/tasks                           │  │
│  │  CreateTaskAsync()    → POST /api/tasks                                         │  │
│  │  GetApprovalsAsync()  → GET  /api/projects/{id}/approvals                       │  │
│  │  CreateApprovalAsync()→ POST /api/approvals                                     │  │
│  │  ApproveAsync()       → PUT  /api/approvals/{id}/approve                        │  │
│  │  RejectAsync()        → PUT  /api/approvals/{id}/reject                         │  │
│  │  GetCommentsAsync()   → GET  /api/projects/{id}/comments                        │  │
│  │  CreateCommentAsync() → POST /api/comments                                      │  │
│  │  GetFilesAsync()      → GET  /api/projects/{id}/files                           │  │
│  │  UploadFileAsync()    → POST /api/projects/{id}/files/upload                    │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────┬───────────────────────────────────────────────────────────────┘
                          │  HTTP Request
                          │  Header: Authorization: Bearer eyJ...
                          │  Body: JSON payload
                          ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                    WorkflowApprovalApi  (localhost:5000)                                │
│                    Folder: (root)/                                                      │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│  │  Program.cs  — Middleware Pipeline (runs in this ORDER for every request)       │   │
│  │                                                                                 │   │
│  │  1. ExceptionHandlingMiddleware   ← catches ALL crashes, maps to HTTP codes     │   │
│  │  2. RequestLoggingMiddleware      ← logs method+path+statuscode+time            │   │
│  │  3. UseSwagger()                                                                │   │
│  │  4. UseStaticFiles()              ← serves wwwroot/uploads files                │   │
│  │  5. UseAuthentication()           ← reads JWT, validates, populates User        │   │
│  │  6. UseAuthorization()            ← checks [Authorize] and [Authorize(Roles)]   │   │
│  │  7. MapControllers()              ← routes to correct controller action         │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│  │  Middleware/   (Folder: Middleware/)                                            │   │
│  │  ├── ExceptionHandlingMiddleware.cs                                             │   │
│  │  │   InvalidOperationException  → 400 Bad Request                              │   │
│  │  │   UnauthorizedAccessException→ 401 Unauthorized                             │   │
│  │  │   KeyNotFoundException       → 404 Not Found                                │   │
│  │  │   Everything else            → 500 Internal Server Error                    │   │
│  │  ├── RequestLoggingMiddleware.cs                                                │   │
│  │  │   logs: "GET /api/projects → 200 in 45ms"                                  │   │
│  │  └── MiddlewareExtensions.cs                                                   │   │
│  │       UseCustomMiddleware() → registers both above                             │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│  │  Controllers/   (Folder: Controllers/)                                          │   │
│  │                                                                                 │   │
│  │  AuthController.cs          Route: /api/auth                                   │   │
│  │  [AllowAnonymous]           POST /api/auth/register                            │   │
│  │                             POST /api/auth/login                               │   │
│  │                             → calls AuthService                                │   │
│  │                                                                                 │   │
│  │  ProjectsController.cs      Route: /api/projects                               │   │
│  │  [Authorize]                GET  /api/projects                                 │   │
│  │                             GET  /api/projects/{id}                            │   │
│  │                             POST /api/projects                                 │   │
│  │                             PUT  /api/projects/{id}/status [Admin,Manager]     │   │
│  │                             → calls ProjectService                             │   │
│  │                             → GetCurrentUserId() ← from JWT claim              │   │
│  │                                                                                 │   │
│  │  TasksController.cs         Route: /api/tasks                                  │   │
│  │  [Authorize]                POST /api/tasks             [Admin,Manager]        │   │
│  │                             GET  /api/projects/{id}/tasks                      │   │
│  │                             PUT  /api/tasks/{id}/status                        │   │
│  │                             → calls TaskService                                │   │
│  │                             → GetCurrentUserId() ← AssignedByUserId from JWT  │   │
│  │                                                                                 │   │
│  │  ApprovalsController.cs     Route: /api/approvals                              │   │
│  │  [Authorize]                POST /api/approvals         [Admin,Manager,Designer│   │
│  │                             GET  /api/projects/{id}/approvals                  │   │
│  │                             PUT  /api/approvals/{id}/approve   [Admin,Manager, │   │
│  │                             PUT  /api/approvals/{id}/reject     Reviewer,Client│   │
│  │                             PUT  /api/approvals/{id}/changes-requested         │   │
│  │                             → calls ApprovalService                            │   │
│  │                                                                                 │   │
│  │  CommentsController.cs      Route: /api/comments                               │   │
│  │  [Authorize]                POST /api/comments                                 │   │
│  │                             GET  /api/projects/{id}/comments                   │   │
│  │                             → calls CommentService                             │   │
│  │                                                                                 │   │
│  │  FilesController.cs         Route: /api/projects/{id}/files                    │   │
│  │  [Authorize]                POST /api/projects/{id}/files/upload               │   │
│  │                             GET  /api/projects/{id}/files                      │   │
│  │                             GET  /api/files/{id}/download                      │   │
│  │                             → calls FileService                                │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
│                    │                                                                    │
│                    ▼                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│  │  Services/Interfaces/   (Folder: Services/Interfaces/)                         │   │
│  │  IAuthService    IProjectService    ITaskService                                │   │
│  │  IApprovalService  ICommentService  IFileService                                │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
│                    │  implemented by                                                    │
│                    ▼                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│  │  Services/Implementations/  (Folder: Services/Implementations/)                │   │
│  │                                                                                 │   │
│  │  AuthService.cs                                                                 │   │
│  │  - RegisterAsync()  → BCrypt.HashPassword() → save User → GenerateToken()      │   │
│  │  - LoginAsync()     → BCrypt.Verify()       → GenerateToken()                  │   │
│  │                                                                                 │   │
│  │  ProjectService.cs                                                              │   │
│  │  - CreateAsync()    → save Project to DB                                        │   │
│  │  - GetAllAsync()    → SELECT * FROM Projects                                    │   │
│  │  - GetByIdAsync()   → SELECT WHERE Id = {id}                                   │   │
│  │  - UpdateStatusAsync() → UPDATE Status WHERE Id = {id}                         │   │
│  │                                                                                 │   │
│  │  TaskService.cs                                                                 │   │
│  │  - CreateAsync()    → validate ProjectId + AssignedToUserId → save Task        │   │
│  │  - GetByProjectIdAsync() → SELECT WHERE ProjectId = {id}                       │   │
│  │  - UpdateStatusAsync()   → UPDATE Status WHERE Id = {id}                       │   │
│  │                                                                                 │   │
│  │  ApprovalService.cs                                                             │   │
│  │  - CreateAsync()    → validate Project + File → save Approval                  │   │
│  │  - ApproveAsync()   → UPDATE Status=Approved WHERE Id = {id}                   │   │
│  │  - RejectAsync()    → UPDATE Status=Rejected WHERE Id = {id}                   │   │
│  │                                                                                 │   │
│  │  CommentService.cs                                                              │   │
│  │  - CreateAsync()    → save Comment to DB                                        │   │
│  │  - GetByProjectIdAsync() → SELECT WHERE ProjectId = {id} ORDER BY DESC        │   │
│  │                                                                                 │   │
│  │  FileService.cs                                                                 │   │
│  │  - UploadAsync()    → save file to wwwroot/uploads/{GUID}.ext                  │   │
│  │                     → auto-increment VersionNumber via MAX query               │   │
│  │  - DownloadAsync()  → FileStream from wwwroot/uploads/                         │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
│                    │                                                                    │
│                    ▼                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│  │  Helpers/TokenService.cs  (Folder: Helpers/)                                   │   │
│  │  - GenerateToken(user)                                                          │   │
│  │    → reads Key, Issuer, Audience, Expiry from appsettings.json                 │   │
│  │    → packs claims: userId, email, name, role                                   │   │
│  │    → signs with HMAC-SHA256                                                     │   │
│  │    → returns "eyJ..." string                                                   │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
│                    │                                                                    │
│                    ▼                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│  │  DTOs/   (Folder: DTOs/)                                                       │   │
│  │  AuthDtos.cs      → RegisterRequest, LoginRequest, AuthResponse                │   │
│  │  ProjectDtos.cs   → ProjectCreateRequest, ProjectResponse                      │   │
│  │  TaskDtos.cs      → TaskCreateRequest, TaskResponse, TaskUpdateStatusRequest   │   │
│  │  ApprovalDtos.cs  → ApprovalCreateRequest, ApprovalResponse, ApprovalUpdateReq │   │
│  │  CommentDtos.cs   → CommentCreateRequest, CommentResponse                      │   │
│  │  FileDtos.cs      → FileListResponse                                           │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
│                    │                                                                    │
│                    ▼                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│  │  Data/AppDbContext.cs  (Folder: Data/)                                         │   │
│  │                                                                                 │   │
│  │  DbSet<User>          → Users table                                            │   │
│  │  DbSet<Project>       → Projects table                                         │   │
│  │  DbSet<WorkflowTask>  → Tasks table                                            │   │
│  │  DbSet<UploadedFile>  → UploadedFiles table                                    │   │
│  │  DbSet<Comment>       → Comments table                                         │   │
│  │  DbSet<Approval>      → Approvals table                                        │   │
│  │                                                                                 │   │
│  │  OnModelCreating() → Fluent API relationships:                                 │   │
│  │  User ──< Project          (one user creates many projects)                    │   │
│  │  Project ──< Task          (one project has many tasks, CASCADE delete)        │   │
│  │  User ──< Task             (AssignedTo + AssignedBy, RESTRICT delete)          │   │
│  │  Project ──< UploadedFile  (CASCADE delete)                                    │   │
│  │  Project ──< Comment       (CASCADE delete)                                    │   │
│  │  Project ──< Approval      (CASCADE delete)                                    │   │
│  │  UploadedFile ──< Approval (SET NULL on delete)                                │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
│                    │                                                                    │
│                    ▼                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│  │  Models/  (Folder: Models/)                                                    │   │
│  │  User.cs         → Id, FullName, Email, PasswordHash, Role, CreatedAt          │   │
│  │  Project.cs      → Id, Name, Description, Status, CreatedByUserId             │   │
│  │  WorkflowTask.cs → Id, ProjectId, Title, Status, Priority,                    │   │
│  │                    AssignedToUserId, AssignedByUserId                          │   │
│  │  Approval.cs     → Id, ProjectId, FileId(nullable), Status,                   │   │
│  │                    RequestedByUserId, ReviewedByUserId(nullable)               │   │
│  │  Comment.cs      → Id, ProjectId, UserId, Message, CreatedAt                  │   │
│  │  UploadedFile.cs → Id, ProjectId, FileName, FilePath, VersionNumber           │   │
│  │  Enums.cs        → UserRole, ProjectStatus, TaskStatus, ApprovalStatus,       │   │
│  │                    Priority                                                    │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────┬───────────────────────────────────────────────────────────────┘
                          │  EF Core queries
                          ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                    MySQL Database                                                       │
│                    workflow_approval_db                                                 │
│                                                                                         │
│   Users ──────────────────────────────────────────────────────────┐                    │
│   Projects ──< Tasks ─────────────────────────────────────────────┤                    │
│   Projects ──< UploadedFiles ──< Approvals ───────────────────────┤                    │
│   Projects ──< Comments ──────────────────────────────────────────┤                    │
│   Projects ──< Approvals ─────────────────────────────────────────┘                    │
│                                                                                         │
│   logs/workflow-api-20260612.log  ← Serilog daily rolling file                        │
│   wwwroot/uploads/{GUID}.ext     ← uploaded files stored here                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## API Usage Map — Which API is called from where

| API Endpoint | Called from (UI) | Handled by (Backend) | Service |
|---|---|---|---|
| POST /api/auth/login | `ApiService.LoginAsync()` | `AuthController.Login()` | `AuthService.LoginAsync()` |
| POST /api/auth/register | `ApiService.RegisterAsync()` | `AuthController.Register()` | `AuthService.RegisterAsync()` |
| GET /api/projects | `ApiService.GetProjectsAsync()` | `ProjectsController.GetAll()` | `ProjectService.GetAllAsync()` |
| GET /api/projects/{id} | `ApiService.GetProjectAsync(id)` | `ProjectsController.GetById()` | `ProjectService.GetByIdAsync()` |
| POST /api/projects | `ApiService.CreateProjectAsync()` | `ProjectsController.Create()` | `ProjectService.CreateAsync()` |
| PUT /api/projects/{id}/status | `ApiService.UpdateProjectStatusAsync()` | `ProjectsController.UpdateStatus()` | `ProjectService.UpdateStatusAsync()` |
| POST /api/tasks | `ApiService.CreateTaskAsync()` | `TasksController.Create()` | `TaskService.CreateAsync()` |
| GET /api/projects/{id}/tasks | `ApiService.GetTasksAsync()` | `TasksController.GetByProject()` | `TaskService.GetByProjectIdAsync()` |
| PUT /api/tasks/{id}/status | `ApiService.UpdateTaskStatusAsync()` | `TasksController.UpdateStatus()` | `TaskService.UpdateStatusAsync()` |
| POST /api/approvals | `ApiService.CreateApprovalAsync()` | `ApprovalsController.Create()` | `ApprovalService.CreateAsync()` |
| GET /api/projects/{id}/approvals | `ApiService.GetApprovalsAsync()` | `ApprovalsController.GetByProject()` | `ApprovalService.GetByProjectIdAsync()` |
| PUT /api/approvals/{id}/approve | `ApiService.ApproveAsync()` | `ApprovalsController.Approve()` | `ApprovalService.ApproveAsync()` |
| PUT /api/approvals/{id}/reject | `ApiService.RejectAsync()` | `ApprovalsController.Reject()` | `ApprovalService.RejectAsync()` |
| PUT /api/approvals/{id}/changes-requested | `ApiService.RequestChangesAsync()` | `ApprovalsController.RequestChanges()` | `ApprovalService.RequestChangesAsync()` |
| POST /api/comments | `ApiService.CreateCommentAsync()` | `CommentsController.Create()` | `CommentService.CreateAsync()` |
| GET /api/projects/{id}/comments | `ApiService.GetCommentsAsync()` | `CommentsController.GetByProject()` | `CommentService.GetByProjectIdAsync()` |
| POST /api/projects/{id}/files/upload | `ApiService.UploadFileAsync()` | `FilesController.Upload()` | `FileService.UploadAsync()` |
| GET /api/projects/{id}/files | `ApiService.GetFilesAsync()` | `FilesController.GetByProject()` | `FileService.GetByProjectIdAsync()` |
| GET /api/files/{id}/download | Direct browser link in Detail.cshtml | `FilesController.Download()` | `FileService.GetFileAsync()` |
