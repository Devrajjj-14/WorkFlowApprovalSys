using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WorkflowApprovalUI.Models;

namespace WorkflowApprovalUI.Services;

public class ApiService
{
    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(IHttpClientFactory factory, IHttpContextAccessor httpContextAccessor)
    {
        _factory = factory;
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient("API");
        var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── Auth ─────────────────────────────────────────────────────────────
    public async Task<(AuthResponse? data, string? error)> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient("API");
        var body = JsonContent(new { email, password });
        var res = await client.PostAsync("/api/auth/login", body);
        return await ParseAsync<AuthResponse>(res);
    }

    public async Task<(AuthResponse? data, string? error)> RegisterAsync(RegisterViewModel vm)
    {
        var client = _factory.CreateClient("API");
        // Map role string to enum int so the backend always accepts it
        var roleInt = vm.Role switch
        {
            "Admin"    => 0,
            "Manager"  => 1,
            "Designer" => 2,
            "Reviewer" => 3,
            "Client"   => 4,
            _          => 2
        };
        var body = JsonContent(new { vm.FullName, vm.Email, vm.Password, role = roleInt });
        var res = await client.PostAsync("/api/auth/register", body);
        return await ParseAsync<AuthResponse>(res);
    }

    // ── Projects ─────────────────────────────────────────────────────────
    public async Task<List<ProjectResponse>> GetProjectsAsync()
    {
        var res = await CreateClient().GetAsync("/api/projects");
        var (data, _) = await ParseAsync<List<ProjectResponse>>(res);
        return data ?? new();
    }

    public async Task<ProjectResponse?> GetProjectAsync(int id)
    {
        var res = await CreateClient().GetAsync($"/api/projects/{id}");
        var (data, _) = await ParseAsync<ProjectResponse>(res);
        return data;
    }

    public async Task<(ProjectResponse? data, string? error)> CreateProjectAsync(string name, string description)
    {
        var body = JsonContent(new { name, description });
        var res = await CreateClient().PostAsync("/api/projects", body);
        return await ParseAsync<ProjectResponse>(res);
    }

    public async Task<(ProjectResponse? data, string? error)> UpdateProjectStatusAsync(int id, string status)
    {
        var body = JsonContent(new { status });
        var res = await CreateClient().PutAsync($"/api/projects/{id}/status", body);
        return await ParseAsync<ProjectResponse>(res);
    }

    // ── Tasks ─────────────────────────────────────────────────────────────
    public async Task<List<TaskResponse>> GetTasksAsync(int projectId)
    {
        var res = await CreateClient().GetAsync($"/api/projects/{projectId}/tasks");
        var (data, _) = await ParseAsync<List<TaskResponse>>(res);
        return data ?? new();
    }

    public async Task<(TaskResponse? data, string? error)> CreateTaskAsync(TaskCreateViewModel vm)
    {
        var body = JsonContent(new
        {
            vm.ProjectId, vm.Title, vm.Description, vm.AssignedToUserId, vm.Priority
        });
        var res = await CreateClient().PostAsync("/api/tasks", body);
        return await ParseAsync<TaskResponse>(res);
    }

    public async Task<(TaskResponse? data, string? error)> UpdateTaskStatusAsync(int id, string status)
    {
        var body = JsonContent(new { status });
        var res = await CreateClient().PutAsync($"/api/tasks/{id}/status", body);
        return await ParseAsync<TaskResponse>(res);
    }

    // ── Approvals ─────────────────────────────────────────────────────────
    public async Task<List<ApprovalResponse>> GetApprovalsAsync(int projectId)
    {
        var res = await CreateClient().GetAsync($"/api/projects/{projectId}/approvals");
        var (data, _) = await ParseAsync<List<ApprovalResponse>>(res);
        return data ?? new();
    }

    public async Task<(ApprovalResponse? data, string? error)> CreateApprovalAsync(ApprovalCreateViewModel vm)
    {
        var body = JsonContent(new { vm.ProjectId, vm.FileId, vm.Remarks });
        var res = await CreateClient().PostAsync("/api/approvals", body);
        return await ParseAsync<ApprovalResponse>(res);
    }

    public async Task<(ApprovalResponse? data, string? error)> ApproveAsync(int id, string remarks)
    {
        var body = JsonContent(new { remarks });
        var res = await CreateClient().PutAsync($"/api/approvals/{id}/approve", body);
        return await ParseAsync<ApprovalResponse>(res);
    }

    public async Task<(ApprovalResponse? data, string? error)> RejectAsync(int id, string remarks)
    {
        var body = JsonContent(new { remarks });
        var res = await CreateClient().PutAsync($"/api/approvals/{id}/reject", body);
        return await ParseAsync<ApprovalResponse>(res);
    }

    public async Task<(ApprovalResponse? data, string? error)> RequestChangesAsync(int id, string remarks)
    {
        var body = JsonContent(new { remarks });
        var res = await CreateClient().PutAsync($"/api/approvals/{id}/changes-requested", body);
        return await ParseAsync<ApprovalResponse>(res);
    }

    // ── Comments ──────────────────────────────────────────────────────────
    public async Task<List<CommentResponse>> GetCommentsAsync(int projectId)
    {
        var res = await CreateClient().GetAsync($"/api/projects/{projectId}/comments");
        var (data, _) = await ParseAsync<List<CommentResponse>>(res);
        return data ?? new();
    }

    public async Task<(CommentResponse? data, string? error)> CreateCommentAsync(int projectId, string message)
    {
        var body = JsonContent(new { projectId, message });
        var res = await CreateClient().PostAsync("/api/comments", body);
        return await ParseAsync<CommentResponse>(res);
    }

    // ── Files ─────────────────────────────────────────────────────────────
    public async Task<List<FileListResponse>> GetFilesAsync(int projectId)
    {
        var res = await CreateClient().GetAsync($"/api/projects/{projectId}/files");
        var (data, _) = await ParseAsync<List<FileListResponse>>(res);
        return data ?? new();
    }

    public async Task<(FileListResponse? data, string? error)> UploadFileAsync(int projectId, IFormFile file)
    {
        var client = CreateClient();
        using var form = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        form.Add(new StreamContent(stream), "file", file.FileName);
        var res = await client.PostAsync($"/api/projects/{projectId}/files/upload", form);
        return await ParseAsync<FileListResponse>(res);
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static StringContent JsonContent(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    private static async Task<(T? data, string? error)> ParseAsync<T>(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();
        if (res.IsSuccessStatusCode)
        {
            var data = JsonSerializer.Deserialize<T>(json, _json);
            return (data, null);
        }

        // Try to extract a readable error message from any backend error shape
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Shape 1: { "message": "..." }
            if (root.TryGetProperty("message", out var msg))
                return (default, msg.GetString());

            // Shape 2: ASP.NET validation { "errors": { "Field": ["msg"] } }
            if (root.TryGetProperty("errors", out var errors))
            {
                var messages = new List<string>();
                foreach (var field in errors.EnumerateObject())
                    foreach (var m in field.Value.EnumerateArray())
                        messages.Add(m.GetString() ?? "");
                return (default, string.Join(" ", messages));
            }

            // Shape 3: { "title": "..." }
            if (root.TryGetProperty("title", out var title))
                return (default, title.GetString());
        }
        catch { }

        return (default, $"Error {(int)res.StatusCode}: request failed.");
    }
}
