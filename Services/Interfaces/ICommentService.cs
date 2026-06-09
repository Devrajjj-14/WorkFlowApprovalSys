using WorkflowApprovalApi.DTOs;

namespace WorkflowApprovalApi.Services.Interfaces;

public interface ICommentService
{
    Task<CommentResponse> CreateAsync(CommentCreateRequest request, int userId);
    Task<List<CommentResponse>> GetByProjectIdAsync(int projectId);
}
