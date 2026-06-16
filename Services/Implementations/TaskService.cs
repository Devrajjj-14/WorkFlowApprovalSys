using Microsoft.EntityFrameworkCore;
using WorkflowApprovalApi.Data;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Models;
using WorkflowApprovalApi.Services.Interfaces;
using TaskStatusEnum = WorkflowApprovalApi.Models.TaskStatus;

namespace WorkflowApprovalApi.Services.Implementations;

public class TaskService : ITaskService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TaskService> _logger;

    public TaskService(AppDbContext context, ILogger<TaskService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TaskResponse> CreateAsync(TaskCreateRequest request, int assignedByUserId)
    {
        _logger.LogInformation(
            "Creating task {TaskTitle} for project {ProjectId} by user {UserId}",
            request.Title, request.ProjectId, assignedByUserId);

        var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId);
        if (!projectExists)
            throw new InvalidOperationException("Project not found.");

        var assignedUserExists = await _context.Users.AnyAsync(u => u.Id == request.AssignedToUserId);
        if (!assignedUserExists)
            throw new InvalidOperationException("Assigned user not found.");

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

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var created = await LoadTaskAsync(task.Id);
        _logger.LogInformation("Task {TaskId} created successfully", created!.Id);
        return MapToResponse(created!);
    }

    public async Task<List<TaskResponse>> GetByProjectIdAsync(int projectId)
    {
        _logger.LogDebug("Fetching tasks for project {ProjectId}", projectId);
        var tasks = await _context.Tasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Include(t => t.UpdatedByUser)          // needed for audit display
            .Where(t => t.ProjectId == projectId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tasks.Select(MapToResponse).ToList();
    }

    // userId added — we now record who performed the status change.
    public async Task<TaskResponse?> UpdateStatusAsync(int id, TaskUpdateStatusRequest request, int userId)
    {
        _logger.LogInformation("Updating task {TaskId} status to {Status} by user {UserId}", id, request.Status, userId);
        var task = await LoadTaskAsync(id);
        if (task == null) return null;

        if (Enum.TryParse<TaskStatusEnum>(request.Status, out var status))
            task.Status = status;

        // Record who made this status change.
        // After this assignment UpdatedByUserId will never be null for this task again.
        task.UpdatedByUserId = userId;
        task.UpdatedAt       = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload so UpdatedByUser navigation property reflects the new user's full name.
        var updated = await LoadTaskAsync(id);
        return MapToResponse(updated!);
    }

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