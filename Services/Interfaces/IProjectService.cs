using WorkflowApprovalApi.DTOs;

namespace WorkflowApprovalApi.Services.Interfaces;

public interface IProjectService
{
    Task<ProjectResponse> CreateAsync(ProjectCreateRequest request, int userId);
    Task<List<ProjectResponse>> GetAllAsync();
    Task<ProjectResponse?> GetByIdAsync(int id);
    Task<ProjectResponse?> UpdateStatusAsync(int id, ProjectUpdateStatusRequest request);
}
