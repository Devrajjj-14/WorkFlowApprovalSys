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
            request.Title,
            request.ProjectId,
            assignedByUserId);
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId);
        if (!projectExists)
        {
            throw new InvalidOperationException("Project not found.");
        }

        var assignedUserExists = await _context.Users.AnyAsync(u => u.Id == request.AssignedToUserId);
        if (!assignedUserExists)
        {
            throw new InvalidOperationException("Assigned user not found.");
        }

        var task = new WorkflowTask
        {
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            Status = TaskStatusEnum.Pending,
            AssignedToUserId = request.AssignedToUserId,
            AssignedByUserId = assignedByUserId,
            Priority = Enum.Parse<Priority>(request.Priority),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var created = await _context.Tasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .FirstAsync(t => t.Id == task.Id);

        _logger.LogInformation("Task {TaskId} created successfully", created.Id);
        return MapToResponse(created);
    }

    public async Task<List<TaskResponse>> GetByProjectIdAsync(int projectId)
    {
        _logger.LogDebug("Fetching tasks for project {ProjectId}", projectId);
        var tasks = await _context.Tasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Where(t => t.ProjectId == projectId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tasks.Select(MapToResponse).ToList();
    }

    public async Task<TaskResponse?> UpdateStatusAsync(int id, TaskUpdateStatusRequest request)
    {
        _logger.LogInformation("Updating task {TaskId} status to {Status}", id, request.Status);
        var task = await _context.Tasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return null;

        if (Enum.TryParse<TaskStatusEnum>(request.Status, out var status))
            task.Status = status;

        task.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapToResponse(task);
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

    private static TaskResponse MapToResponse(WorkflowTask task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            AssignedToUserId = task.AssignedToUserId,
            AssignedByUserId = task.AssignedByUserId,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}