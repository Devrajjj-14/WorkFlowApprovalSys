using WorkflowApprovalApi.DTOs;

namespace WorkflowApprovalApi.Services.Interfaces;

public interface IProjectService
{
    Task<ProjectResponse> CreateAsync(ProjectCreateRequest request, int userId);
    Task<List<ProjectResponse>> GetAllAsync();
    Task<ProjectResponse?> GetByIdAsync(int id);
    // userId added so the service can record who performed the status change for audit trail.
    Task<ProjectResponse?> UpdateStatusAsync(int id, ProjectUpdateStatusRequest request, int userId);
    Task<bool> DeleteAsync(int id);
}
