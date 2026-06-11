// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: AuthenticationHeaderValue doesn't exist — JWT Bearer header cannot be built
using System.Net.Http.Headers;

// Without this: Encoding.UTF8 doesn't exist — StringContent (JSON body) can't be created
using System.Text;

// Without this: JsonSerializer and JsonDocument don't exist — JSON can't be serialized or parsed
using System.Text.Json;

// Without this: all the model classes (AuthResponse, ProjectResponse etc.) are unknown
using WorkflowApprovalUI.Models;

namespace WorkflowApprovalUI.Services;

// ApiService is the ONLY class in the frontend that talks to the backend API
// Every controller calls this service — it handles all HTTP requests, JWT attachment, and error parsing
// Without this class: every controller has no way to get data — the entire frontend is non-functional
public class ApiService
{
    // IHttpClientFactory creates named HttpClient instances — here it creates the "API" client
    // pointed at http://localhost:5000 (registered in Program.cs)
    // Without this field: we can't make any HTTP calls to the backend
    private readonly IHttpClientFactory _factory;

    // Allows access to the current HTTP request — specifically used to read the JWT from session
    // Without this field: we can't read Session — JWT can't be attached to backend calls
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Shared JSON options for deserializing backend responses
    // PropertyNameCaseInsensitive = true means "userId" and "UserId" both map to the same C# property
    // Without this: JSON fields with different casing fail to deserialize — model properties stay null
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Constructor — DI container injects IHttpClientFactory and IHttpContextAccessor automatically
    // Without this constructor: DI can't create ApiService — every controller that needs it crashes
    public ApiService(IHttpClientFactory factory, IHttpContextAccessor httpContextAccessor)
    {
        _factory = factory;
        _httpContextAccessor = httpContextAccessor;
    }

    // ── Private Helper — creates an authenticated HttpClient ─────────────────
    // Reads the JWT from session (stored there after login in AuthController)
    // Attaches it as "Authorization: Bearer <token>" header to every request
    // Without this method: backend calls go out with no auth header — every protected endpoint returns 401
    private HttpClient CreateClient()
    {
        // Gets the "API" HttpClient with BaseAddress = http://localhost:5000
        var client = _factory.CreateClient("API");

        // Reads the JWT token stored in session after login
        var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");

        // If a token exists, attach it as a Bearer header
        // Without this if block: requests go out unauthenticated — backend returns 401 for protected routes
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    
    // AUTHENTICATION

    // Sends POST /api/auth/login with email and password
    // Uses _factory.CreateClient("API") directly — NOT CreateClient() — because no JWT exists yet at login
    // Without this: the login form has no way to authenticate the user against the backend
    public async Task<(AuthResponse? data, string? error)> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient("API");
        var body = JsonContent(new { email, password });
        var res = await client.PostAsync("/api/auth/login", body);
        return await ParseAsync<AuthResponse>(res);
    }

    // Sends POST /api/auth/register with user details
    // Role is mapped to integer because the backend's UserRole enum defaults to int-based JSON binding
    // "Designer" → 2, "Admin" → 0 etc. — this was the fix for the registration 400 error
    // Without this: the register form would fail with 400 — role string not recognized by backend
    public async Task<(AuthResponse? data, string? error)> RegisterAsync(RegisterViewModel vm)
    {
        var client = _factory.CreateClient("API");

        // Maps the role string from the dropdown to its integer enum value
        // Without this mapping: sending "Designer" as a string causes 400 Bad Request from backend
        var roleInt = vm.Role switch
        {
            "Admin"    => 0,
            "Manager"  => 1,
            "Designer" => 2,
            "Reviewer" => 3,
            "Client"   => 4,
            _          => 2   // default to Designer if unknown
        };

        var body = JsonContent(new { vm.FullName, vm.Email, vm.Password, role = roleInt });
        var res = await client.PostAsync("/api/auth/register", body);
        return await ParseAsync<AuthResponse>(res);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PROJECTS
    // ════════════════════════════════════════════════════════════════════════

    // Sends GET /api/projects — returns all projects for the current user
    // Uses CreateClient() so the JWT is attached — backend checks auth
    // Without this: the projects list page has no data to show
    public async Task<List<ProjectResponse>> GetProjectsAsync()
    {
        var res = await CreateClient().GetAsync("/api/projects");
        var (data, _) = await ParseAsync<List<ProjectResponse>>(res);
        return data ?? new();  // return empty list if null — prevents NullReferenceException in views
    }

    // Sends GET /api/projects/{id} — returns a single project by ID
    // Without this: the project detail page cannot load — returns null and shows 404
    public async Task<ProjectResponse?> GetProjectAsync(int id)
    {
        var res = await CreateClient().GetAsync($"/api/projects/{id}");
        var (data, _) = await ParseAsync<ProjectResponse>(res);
        return data;
    }

    // Sends POST /api/projects with name and description
    // Returns (data, error) tuple — error is shown in the view if creation fails
    // Without this: the "New Project" form has no backend to save to
    public async Task<(ProjectResponse? data, string? error)> CreateProjectAsync(string name, string description)
    {
        var body = JsonContent(new { name, description });
        var res = await CreateClient().PostAsync("/api/projects", body);
        return await ParseAsync<ProjectResponse>(res);
    }

    // Sends PUT /api/projects/{id}/status with new status string
    // Used by Admin/Manager to move a project through its workflow stages
    // Without this: the "Change Status" modal button has no effect
    public async Task<(ProjectResponse? data, string? error)> UpdateProjectStatusAsync(int id, string status)
    {
        var body = JsonContent(new { status });
        var res = await CreateClient().PutAsync($"/api/projects/{id}/status", body);
        return await ParseAsync<ProjectResponse>(res);
    }

    // Sends DELETE /api/projects/{id} — permanently deletes a project and all its data
    // Returns (bool success, string? error) tuple
    // Without this: the "Delete Project" button has no backend operation to call
    public async Task<(bool success, string? error)> DeleteProjectAsync(int id)
    {
        var res = await CreateClient().DeleteAsync($"/api/projects/{id}");
        if (res.IsSuccessStatusCode) return (true, null);
        var (_, error) = await ParseAsync<object>(res);
        return (false, error);
    }

    // ════════════════════════════════════════════════════════════════════════
    // TASKS
    // ════════════════════════════════════════════════════════════════════════

    // Sends GET /api/projects/{projectId}/tasks — returns all tasks for a project
    // Without this: the Tasks tab in the project detail page shows nothing
    public async Task<List<TaskResponse>> GetTasksAsync(int projectId)
    {
        var res = await CreateClient().GetAsync($"/api/projects/{projectId}/tasks");
        var (data, _) = await ParseAsync<List<TaskResponse>>(res);
        return data ?? new();
    }

    // Sends POST /api/tasks with full task details
    // Used by Admin/Manager to create tasks and assign them to users
    // Without this: the "Add Task" modal form has no backend to save to
    public async Task<(TaskResponse? data, string? error)> CreateTaskAsync(TaskCreateViewModel vm)
    {
        var body = JsonContent(new
        {
            vm.ProjectId, vm.Title, vm.Description, vm.AssignedToUserId, vm.Priority
        });
        var res = await CreateClient().PostAsync("/api/tasks", body);
        return await ParseAsync<TaskResponse>(res);
    }

    // Sends PUT /api/tasks/{id}/status with new status string
    // Used by any user to move a task between Pending, InProgress, Completed, Blocked
    // Without this: the status dropdown on task rows has no effect
    public async Task<(TaskResponse? data, string? error)> UpdateTaskStatusAsync(int id, string status)
    {
        var body = JsonContent(new { status });
        var res = await CreateClient().PutAsync($"/api/tasks/{id}/status", body);
        return await ParseAsync<TaskResponse>(res);
    }

    // Sends DELETE /api/tasks/{id} — deletes a task and all its comments
    // Without this: the trash icon on task rows does nothing
    public async Task<(bool success, string? error)> DeleteTaskAsync(int id)
    {
        var res = await CreateClient().DeleteAsync($"/api/tasks/{id}");
        if (res.IsSuccessStatusCode) return (true, null);
        var (_, error) = await ParseAsync<object>(res);
        return (false, error);
    }

    // ════════════════════════════════════════════════════════════════════════
    // APPROVALS
    // ════════════════════════════════════════════════════════════════════════

    // Sends GET /api/projects/{projectId}/approvals — returns all approval requests for a project
    // Without this: the Approvals tab shows nothing
    public async Task<List<ApprovalResponse>> GetApprovalsAsync(int projectId)
    {
        var res = await CreateClient().GetAsync($"/api/projects/{projectId}/approvals");
        var (data, _) = await ParseAsync<List<ApprovalResponse>>(res);
        return data ?? new();
    }

    // Sends POST /api/approvals to create a new approval request
    // Used by Admin, Manager, Designer roles
    // Without this: the "Request Approval" modal has no backend to save to
    public async Task<(ApprovalResponse? data, string? error)> CreateApprovalAsync(ApprovalCreateViewModel vm)
    {
        var body = JsonContent(new { vm.ProjectId, vm.FileId, vm.Remarks });
        var res = await CreateClient().PostAsync("/api/approvals", body);
        return await ParseAsync<ApprovalResponse>(res);
    }

    // Sends PUT /api/approvals/{id}/approve — marks an approval as Approved
    // remarks is optional — empty string is fine, backend accepts null/empty
    // Without this: the "Approve" button has no backend operation
    public async Task<(ApprovalResponse? data, string? error)> ApproveAsync(int id, string remarks)
    {
        var body = JsonContent(new { remarks });
        var res = await CreateClient().PutAsync($"/api/approvals/{id}/approve", body);
        return await ParseAsync<ApprovalResponse>(res);
    }

    // Sends PUT /api/approvals/{id}/reject — marks an approval as Rejected
    // Without this: the "Reject" button has no backend operation
    public async Task<(ApprovalResponse? data, string? error)> RejectAsync(int id, string remarks)
    {
        var body = JsonContent(new { remarks });
        var res = await CreateClient().PutAsync($"/api/approvals/{id}/reject", body);
        return await ParseAsync<ApprovalResponse>(res);
    }

    // Sends PUT /api/approvals/{id}/changes-requested — marks approval as ChangesRequested
    // Without this: the "Request Changes" button has no backend operation
    public async Task<(ApprovalResponse? data, string? error)> RequestChangesAsync(int id, string remarks)
    {
        var body = JsonContent(new { remarks });
        var res = await CreateClient().PutAsync($"/api/approvals/{id}/changes-requested", body);
        return await ParseAsync<ApprovalResponse>(res);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PROJECT COMMENTS
    // ════════════════════════════════════════════════════════════════════════

    // Sends GET /api/projects/{projectId}/comments — returns all project-level comments
    // Without this: the "Project Comments" tab shows nothing
    public async Task<List<CommentResponse>> GetCommentsAsync(int projectId)
    {
        var res = await CreateClient().GetAsync($"/api/projects/{projectId}/comments");
        var (data, _) = await ParseAsync<List<CommentResponse>>(res);
        return data ?? new();
    }

    // Sends POST /api/comments to create a project-level comment
    // Without this: the comment compose box on the Project Comments tab does nothing
    public async Task<(CommentResponse? data, string? error)> CreateCommentAsync(int projectId, string message)
    {
        var body = JsonContent(new { projectId, message });
        var res = await CreateClient().PostAsync("/api/comments", body);
        return await ParseAsync<CommentResponse>(res);
    }

    // ════════════════════════════════════════════════════════════════════════
    // TASK COMMENTS
    // ════════════════════════════════════════════════════════════════════════

    // Sends GET /api/tasks/{taskId}/comments — returns all comments for a specific task
    // Called for every task when loading the Detail page — all run in parallel via Task.WhenAll
    // Without this: the inline task comment threads are always empty
    public async Task<List<TaskCommentResponse>> GetTaskCommentsAsync(int taskId)
    {
        var res = await CreateClient().GetAsync($"/api/tasks/{taskId}/comments");
        var (data, _) = await ParseAsync<List<TaskCommentResponse>>(res);
        return data ?? new();
    }

    // Sends POST /api/comments/task to create a comment on a specific task
    // Without this: the inline comment compose input on task rows does nothing
    public async Task<(TaskCommentResponse? data, string? error)> CreateTaskCommentAsync(int taskId, string message)
    {
        var body = JsonContent(new { taskId, message });
        var res = await CreateClient().PostAsync("/api/comments/task", body);
        return await ParseAsync<TaskCommentResponse>(res);
    }

    // ════════════════════════════════════════════════════════════════════════
    // FILES
    // ════════════════════════════════════════════════════════════════════════

    // Sends GET /api/projects/{projectId}/files — returns file metadata for a project
    // Without this: the Files tab shows nothing
    public async Task<List<FileListResponse>> GetFilesAsync(int projectId)
    {
        var res = await CreateClient().GetAsync($"/api/projects/{projectId}/files");
        var (data, _) = await ParseAsync<List<FileListResponse>>(res);
        return data ?? new();
    }

    // Sends POST /api/projects/{projectId}/files/upload as multipart/form-data
    // The file is sent as a stream inside a MultipartFormDataContent — same format as a browser form upload
    // Without this: the file upload modal does nothing — files are never saved
    public async Task<(FileListResponse? data, string? error)> UploadFileAsync(int projectId, IFormFile file)
    {
        var client = CreateClient();
        using var form = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();                           // open the uploaded file as a stream
        form.Add(new StreamContent(stream), "file", file.FileName);        // add it to the form with field name "file"
        var res = await client.PostAsync($"/api/projects/{projectId}/files/upload", form);
        return await ParseAsync<FileListResponse>(res);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════════════════════════════

    // Serializes a C# object to a JSON string and wraps it in StringContent with content-type application/json
    // Used to build request bodies for every POST/PUT call
    // Without this: request bodies would be empty or wrong format — backend can't deserialize them
    private static StringContent JsonContent(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    // Reads and parses the HTTP response into a result tuple (data, error)
    // If the response is successful (2xx): deserializes JSON into type T, returns (data, null)
    // If the response failed: tries to extract a human-readable error from three possible error shapes
    //   Shape 1: { "message": "..." } — your custom exception middleware format
    //   Shape 2: { "errors": { "Field": ["msg"] } } — ASP.NET model validation errors
    //   Shape 3: { "title": "..." } — ASP.NET problem details format
    // Without this helper: every API call would need its own error parsing logic — a lot of duplication
    private static async Task<(T? data, string? error)> ParseAsync<T>(HttpResponseMessage res)
    {
        // Read the full response body as a string
        var json = await res.Content.ReadAsStringAsync();

        // If the request succeeded, deserialize the JSON into the expected type
        // Without this branch: successful responses would be treated as errors
        if (res.IsSuccessStatusCode)
        {
            var data = JsonSerializer.Deserialize<T>(json, _json);
            return (data, null);
        }

        // Request failed — try to extract a readable error message
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Shape 1: backend returns { "message": "Email is already registered." }
            // This is what ExceptionHandlingMiddleware produces
            if (root.TryGetProperty("message", out var msg))
                return (default, msg.GetString());

            // Shape 2: ASP.NET model validation returns { "errors": { "Email": ["not valid"] } }
            // This happens when [Required] or [EmailAddress] attributes fail
            if (root.TryGetProperty("errors", out var errors))
            {
                var messages = new List<string>();
                foreach (var field in errors.EnumerateObject())
                    foreach (var m in field.Value.EnumerateArray())
                        messages.Add(m.GetString() ?? "");
                return (default, string.Join(" ", messages));
            }

            // Shape 3: ASP.NET problem details returns { "title": "Bad Request" }
            if (root.TryGetProperty("title", out var title))
                return (default, title.GetString());
        }
        catch { }  // if JSON parsing fails entirely, fall through to the generic message

        // Fallback — return the HTTP status code number as an error message
        // Without this fallback: a non-JSON error response would crash the error parser itself
        return (default, $"Error {(int)res.StatusCode}: request failed.");
    }
}
