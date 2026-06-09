using WorkflowApprovalApi.DTOs;

namespace WorkflowApprovalApi.Services.Interfaces;

public interface IApprovalService
{
    Task<ApprovalResponse> CreateAsync(ApprovalCreateRequest request, int userId);
    Task<List<ApprovalResponse>> GetByProjectIdAsync(int projectId);
    Task<ApprovalResponse?> ApproveAsync(int id, ApprovalUpdateRequest request, int userId);
    Task<ApprovalResponse?> RejectAsync(int id, ApprovalUpdateRequest request, int userId);
    Task<ApprovalResponse?> RequestChangesAsync(int id, ApprovalUpdateRequest request, int userId);
}
