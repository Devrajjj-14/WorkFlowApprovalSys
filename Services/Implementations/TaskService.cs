// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: Include, AnyAsync, FirstOrDefaultAsync, Add, Remove, SaveChangesAsync don't exist
using Microsoft.EntityFrameworkCore;

// Without this: AppDbContext is unknown — all task database operations fail to compile
using WorkflowApprovalApi.Data;

// Without this: TaskCreateRequest, TaskResponse etc. are unknown — method signatures break
using WorkflowApprovalApi.DTOs;

// Without this: WorkflowTask, Priority, TaskStatus models are unknown
using WorkflowApprovalApi.Models;

// Without this: ITaskService interface is unknown — TaskService can't implement the contract
using WorkflowApprovalApi.Services.Interfaces;

// Alias avoids clash with System.Threading.Tasks.Task — TaskStatus here means workflow task status enum
// Without this alias: TaskStatus would be ambiguous between BCL and our Models namespace
using TaskStatusEnum = WorkflowApprovalApi.Models.TaskStatus;

namespace WorkflowApprovalApi.Services.Implementations;

// TaskService handles task creation, listing, status updates, and deletion within projects
// Called by TasksController — validates project/user exist, tracks audit fields on status change
// Without this class: all /api/tasks endpoints return 500 — DI can't resolve ITaskService
public class TaskService : ITaskService
{
    // EF Core database context for Tasks, Projects, and Users tables
    // Without this field: no database access — every task operation fails
    private readonly AppDbContext _context;

    // Logger for task lifecycle events
    // Without this field: task create/update/delete aren't visible in logs
    private readonly ILogger<TaskService> _logger;

    // Constructor — DI injects database and logger
    // Without this constructor: TaskService can't be constructed — app crashes on startup
    public TaskService(AppDbContext context, ILogger<TaskService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────
    // Creates a task assigned to a user, created by the caller (assignedByUserId)
    // Without this: POST /api/tasks has no backend — tasks can't be created
    public async Task<TaskResponse> CreateAsync(TaskCreateRequest request, int assignedByUserId)
    {
        _logger.LogInformation(
            "Creating task {TaskTitle} for project {ProjectId} by user {UserId}",
            request.Title, request.ProjectId, assignedByUserId);

        // Verify target project exists — prevents orphan tasks with invalid ProjectId FK
        // Without this: insert might fail with DB error instead of clear "Project not found"
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId);
        if (!projectExists)
            throw new InvalidOperationException("Project not found.");

        // Verify assignee user exists — prevents invalid AssignedToUserId FK
        // Without this: task could reference deleted user — broken assignments in UI
        var assignedUserExists = await _context.Users.AnyAsync(u => u.Id == request.AssignedToUserId);
        if (!assignedUserExists)
            throw new InvalidOperationException("Assigned user not found.");

        // Build WorkflowTask entity — starts Pending, updater null until first status change
        // Without this: nothing to persist — CreateAsync completes with no row in DB
        var task = new WorkflowTask
        {
            ProjectId        = request.ProjectId,
            Title            = request.Title,
            Description      = request.Description,
            Status           = TaskStatusEnum.Pending,
            AssignedToUserId = request.AssignedToUserId,
            AssignedByUserId = assignedByUserId,
            // UpdatedByUserId intentionally null — task has never had a status change yet.
            // UI will show "Created by {AssignedByUser}" until first status update.
            UpdatedByUserId  = null,
            Priority         = Enum.Parse<Priority>(request.Priority),
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        };

        // Insert task row into database
        // Without Add + SaveChangesAsync: task never saved
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Reload with navigation properties for audit names in response
        // Without this: AssignedByName missing in API response
        var created = await LoadTaskAsync(task.Id);
        _logger.LogInformation("Task {TaskId} created successfully", created!.Id);
        return MapToResponse(created!);
    }

    // ── GetByProjectIdAsync ───────────────────────────────────────────────────
    // Returns all tasks for one project, newest first, with audit fields
    // Without this: GET /api/projects/{id}/tasks returns empty — Tasks tab has no data
    public async Task<List<TaskResponse>> GetByProjectIdAsync(int projectId)
    {
        _logger.LogDebug("Fetching tasks for project {ProjectId}", projectId);

        // Filter by project, include assigner/updater users for audit display
        // Without Include: names in TaskResponse would be null
        var tasks = await _context.Tasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Include(t => t.UpdatedByUser)          // needed for audit display
            .Where(t => t.ProjectId == projectId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tasks.Select(MapToResponse).ToList();
    }

    // ── UpdateStatusAsync ─────────────────────────────────────────────────────
    // Updates task workflow status and records who changed it
    // userId added — we now record who performed the status change.
    // Without this: PUT /api/tasks/{id}/status does nothing — kanban/status dropdown broken
    public async Task<TaskResponse?> UpdateStatusAsync(int id, TaskUpdateStatusRequest request, int userId)
    {
        _logger.LogInformation("Updating task {TaskId} status to {Status} by user {UserId}", id, request.Status, userId);

        // Load task or return null for 404
        // Without this: status update on missing id would throw or no-op unclearly
        var task = await LoadTaskAsync(id);
        if (task == null) return null;

        // Parse status string (e.g. "InProgress") into enum — invalid strings ignored
        // Without this: Status column never updates
        if (Enum.TryParse<TaskStatusEnum>(request.Status, out var status))
            task.Status = status;

        // Record who made this status change.
        // After this assignment UpdatedByUserId will never be null for this task again.
        task.UpdatedByUserId = userId;
        task.UpdatedAt       = DateTime.UtcNow;

        // Persist status and audit fields
        // Without SaveChangesAsync: changes lost when scoped DbContext disposes
        await _context.SaveChangesAsync();

        // Reload so UpdatedByUser navigation property reflects the new user's full name.
        var updated = await LoadTaskAsync(id);
        return MapToResponse(updated!);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────
    // Removes a task by Id — returns false if not found
    // Without this: DELETE /api/tasks/{id} can't remove tasks
    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting task {TaskId}", id);

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Task {TaskId} deleted successfully", id);
        return true;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    // Centralised loader — always includes all three audit navigation properties.
    // Without this: Create/UpdateStatus would duplicate Include chains — easy to miss a nav property
    private async Task<WorkflowTask?> LoadTaskAsync(int id)
    {
        return await _context.Tasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Include(t => t.UpdatedByUser)  // null-safe — EF handles nullable FK correctly
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    // ── MapToResponse ─────────────────────────────────────────────────────────
    // Business rule for audit display:
    //   UpdatedByUserId == null  → task was never status-changed → UI shows "Created by"
    //   UpdatedByUserId != null  → task was status-changed       → UI shows "Last updated by"
    // Without this: API would return wrong shape — frontend TaskResponse binding fails
    private static TaskResponse MapToResponse(WorkflowTask task)
    {
        return new TaskResponse
        {
            Id               = task.Id,
            ProjectId        = task.ProjectId,
            Title            = task.Title,
            Description      = task.Description,
            Status           = task.Status.ToString(),
            Priority         = task.Priority.ToString(),
            AssignedToUserId = task.AssignedToUserId,

            // Audit – creation (always present)
            AssignedByUserId = task.AssignedByUserId,
            AssignedByName   = task.AssignedByUser.FullName,
            CreatedAt        = task.CreatedAt,

            // Audit – last status update (null until first status change)
            UpdatedByUserId  = task.UpdatedByUserId,
            UpdatedByName    = task.UpdatedByUser?.FullName,    // ?. safe: may be null
            UpdatedAt        = task.UpdatedAt
        };
    }
}
