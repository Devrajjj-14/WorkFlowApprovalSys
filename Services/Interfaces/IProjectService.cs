// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: ProjectCreateRequest, ProjectResponse, and ProjectUpdateStatusRequest are unknown — signatures fail to compile
using WorkflowApprovalApi.DTOs;

// Groups all service contracts in one namespace so Program.cs can register them with AddScoped<Interface, Implementation>()
namespace WorkflowApprovalApi.Services.Interfaces;

// Contract for project CRUD and status workflow — create, list, fetch, update status, and delete projects
// ProjectService implements this; ProjectsController calls it for all /api/projects routes
// Without this interface: DI can't bind IProjectService → ProjectService — project endpoints have no service to call
public interface IProjectService
{
    // Creates a new project with title, description, and initial status
    // userId identifies who created it — used for audit trail and ownership checks
    // Without this: POST /api/projects has no method to call — new projects can't be created
    Task<ProjectResponse> CreateAsync(ProjectCreateRequest request, int userId);

    // Returns every project in the system — used by the dashboard and project list views
    // Without this: GET /api/projects has no method to call — the project list stays empty
    Task<List<ProjectResponse>> GetAllAsync();

    // Fetches a single project by its database id — returns null if not found
    // Without this: GET /api/projects/{id} has no method to call — project detail pages can't load
    Task<ProjectResponse?> GetByIdAsync(int id);

    // Changes a project's workflow status (e.g. Draft → InReview → Approved)
    // userId records who performed the change — written to the audit trail
    // Without this: PATCH /api/projects/{id}/status has no method to call — status transitions can't happen
    Task<ProjectResponse?> UpdateStatusAsync(int id, ProjectUpdateStatusRequest request, int userId);

    // Permanently removes a project and its related data from the database
    // Returns true if deleted, false if the id didn't exist
    // Without this: DELETE /api/projects/{id} has no method to call — projects can't be removed
    Task<bool> DeleteAsync(int id);
}
