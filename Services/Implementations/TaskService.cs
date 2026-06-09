using Microsoft.EntityFrameworkCore;
using WorkflowApprovalApi.Data;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Models;
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Services.Implementations;

public class TaskService : ITaskService
{
    private readonly AppDbContext _context;

    public TaskService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TaskResponse> CreateAsync(TaskCreateRequest request, int assignedByUserId)
    {
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
            Status = TaskStatus.Pending,
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

        return MapToResponse(created);
    }

    public async Task<List<TaskResponse>> GetByProjectIdAsync(int projectId)
    {
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
        var task = await _context.Tasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
        {
            return null;
        }

        if (Enum.TryParse<TaskStatus>(request.Status, out var status))
        {
            task.Status = status;
        }
        task.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToResponse(task);
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
