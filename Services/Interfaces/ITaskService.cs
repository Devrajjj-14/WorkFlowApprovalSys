using WorkflowApprovalApi.DTOs;

namespace WorkflowApprovalApi.Services.Interfaces;

public interface ITaskService
{
    Task<TaskResponse> CreateAsync(TaskCreateRequest request, int userId);
    Task<List<TaskResponse>> GetByProjectIdAsync(int projectId);
    Task<TaskResponse?> UpdateStatusAsync(int id, TaskUpdateStatusRequest request);
}
