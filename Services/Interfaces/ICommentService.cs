using WorkflowApprovalApi.DTOs;

namespace WorkflowApprovalApi.Services.Interfaces;

public interface ICommentService
{
    Task<CommentResponse> CreateAsync(CommentCreateRequest request, int userId);
    Task<List<CommentResponse>> GetByProjectIdAsync(int projectId);

    Task<TaskCommentResponse> CreateTaskCommentAsync(TaskCommentCreateRequest request, int userId);
    Task<List<TaskCommentResponse>> GetByTaskIdAsync(int taskId);
}
