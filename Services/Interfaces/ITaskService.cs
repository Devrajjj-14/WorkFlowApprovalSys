using WorkflowApprovalApi.DTOs;

namespace WorkflowApprovalApi.Services.Interfaces;

public interface ITaskService
{
    Task<TaskResponse> CreateAsync(TaskCreateRequest request, int userId);
    Task<List<TaskResponse>> GetByProjectIdAsync(int projectId);
    // userId added so the service can record who performed the status change for audit trail.
    Task<TaskResponse?> UpdateStatusAsync(int id, TaskUpdateStatusRequest request, int userId);
    Task<bool> DeleteAsync(int id);
}
