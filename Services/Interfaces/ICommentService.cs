// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: CommentCreateRequest, CommentResponse, TaskCommentCreateRequest, and TaskCommentResponse are unknown — signatures fail to compile
using WorkflowApprovalApi.DTOs;

// Groups all service contracts in one namespace so Program.cs can register them with AddScoped<Interface, Implementation>()
namespace WorkflowApprovalApi.Services.Interfaces;

// Contract for comments on projects and tasks — add and list discussion threads at both levels
// CommentService implements this; CommentsController calls it for all /api/comments routes
// Without this interface: DI can't bind ICommentService → CommentService — comment endpoints have no service to call
public interface ICommentService
{
    // Adds a new comment to a project with text content
    // userId identifies the author — stored on the comment record
    // Without this: POST /api/comments has no method to call — project discussion threads can't be started
    Task<CommentResponse> CreateAsync(CommentCreateRequest request, int userId);

    // Returns all comments for a given project — used by the project detail comments section
    // Without this: GET /api/comments/project/{projectId} has no method to call — project comments can't be listed
    Task<List<CommentResponse>> GetByProjectIdAsync(int projectId);

    // Adds a new comment to a specific task with text content
    // userId identifies the author — stored on the task comment record
    // Without this: POST /api/comments/task has no method to call — task-level discussion can't happen
    Task<TaskCommentResponse> CreateTaskCommentAsync(TaskCommentCreateRequest request, int userId);

    // Returns all comments for a given task — used by the task detail comments section
    // Without this: GET /api/comments/task/{taskId} has no method to call — task comments can't be listed
    Task<List<TaskCommentResponse>> GetByTaskIdAsync(int taskId);
}
