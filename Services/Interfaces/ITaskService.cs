// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: TaskCreateRequest, TaskResponse, and TaskUpdateStatusRequest are unknown — signatures fail to compile
using WorkflowApprovalApi.DTOs;

// Groups all service contracts in one namespace so Program.cs can register them with AddScoped<Interface, Implementation>()
namespace WorkflowApprovalApi.Services.Interfaces;

// Contract for task management within projects — create, list, update status, and delete tasks
// TaskService implements this; TasksController calls it for all /api/tasks routes
// Without this interface: DI can't bind ITaskService → TaskService — task endpoints have no service to call
public interface ITaskService
{
    // Creates a new task linked to a project with title, description, and assignee
    // userId identifies who created it — used for audit trail
    // Without this: POST /api/tasks has no method to call — new tasks can't be added to a project
    Task<TaskResponse> CreateAsync(TaskCreateRequest request, int userId);

    // Returns all tasks belonging to a given project — used by the project detail task list
    // Without this: GET /api/tasks/project/{projectId} has no method to call — tasks for a project can't be listed
    Task<List<TaskResponse>> GetByProjectIdAsync(int projectId);

    // Changes a task's workflow status (e.g. Pending → InProgress → Done)
    // userId records who performed the change — written to the audit trail
    // Without this: PATCH /api/tasks/{id}/status has no method to call — task status transitions can't happen
    Task<TaskResponse?> UpdateStatusAsync(int id, TaskUpdateStatusRequest request, int userId);

    // Permanently removes a task from the database
    // Returns true if deleted, false if the id didn't exist
    // Without this: DELETE /api/tasks/{id} has no method to call — tasks can't be removed
    Task<bool> DeleteAsync(int id);
}
