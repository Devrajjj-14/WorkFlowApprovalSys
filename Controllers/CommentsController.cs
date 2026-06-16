// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: ClaimTypes.NameIdentifier is unknown — GetCurrentUserId() won't compile
using System.Security.Claims;

// Without this: [Authorize] doesn't exist — JWT requirement on this controller fails to compile
using Microsoft.AspNetCore.Authorization;

// Without this: [ApiController], [Route], [HttpPost], ControllerBase etc. don't exist — controller won't compile
using Microsoft.AspNetCore.Mvc;

// Without this: CommentCreateRequest, CommentResponse, TaskCommentCreateRequest etc. are unknown
using WorkflowApprovalApi.DTOs;

// Without this: ICommentService is unknown — CommentsController can't be constructed via DI
using WorkflowApprovalApi.Services.Interfaces;

// Without this: CommentsController lives in the global namespace — breaks project structure
namespace WorkflowApprovalApi.Controllers;

// Requires a valid JWT for every action — anonymous users can't read or post comments
// Without this: anyone could post comments without logging in — spam and impersonation risk
[Authorize]

// Marks this class as an API controller — enables automatic model validation on comment bodies
// Without this: invalid JSON on create may not return clean 400 errors
[ApiController]

// Sets the base URL prefix for conventional routes on this controller to /api/comments
// Without this: POST /api/comments and POST /api/comments/task won't resolve correctly
[Route("api/comments")]

// CommentsController handles project-level and task-level comment threads
// Without this class: no comment endpoints exist — discussion features on projects/tasks are dead
public class CommentsController : ControllerBase
{
    // Holds the injected comment service that persists comments to the database
    // Without this field: every action crashes — no comments can be saved or loaded
    private readonly ICommentService _commentService;

    // Constructor — DI injects ICommentService (registered as CommentService in Program.cs)
    // Without this constructor: DI can't create CommentsController — all comment endpoints return 500
    public CommentsController(ICommentService commentService)
    {
        // Stores the service instance for use in all action methods
        // Without this assignment: _commentService stays null — NullReferenceException on every request
        _commentService = commentService;
    }

    // ── Project Comments ────────────────────────────────────────────────────────

    // Maps POST /api/comments to this method — creates a comment on a project
    // Without this: new project comments can't be submitted via the API
    [HttpPost]

    // Adds a text comment to a project, attributed to the current user
    // Without this method: POST /api/comments returns 404 — project comment form fails
    public async Task<ActionResult<CommentResponse>> Create([FromBody] CommentCreateRequest request)
    {
        // Wraps service call so validation errors (e.g. project not found) become clean 400 responses
        // Without this try: InvalidOperationException becomes an unhandled 500 error
        try
        {
            // Delegates to CommentService — inserts comment row linked to projectId, returns CommentResponse
            // GetCurrentUserId() supplies author id from JWT — without it comments have no author
            // Without this call: comment text is never saved to the database
            var result = await _commentService.CreateAsync(request, GetCurrentUserId());

            // Returns HTTP 200 with the new comment JSON (id, text, author, timestamp)
            // Without this: client gets no body — UI can't append the comment to the thread
            return Ok(result);
        }
        // Catches business-rule failures like "project does not exist"
        // Without this catch: validation failures crash with 500 instead of readable 400
        catch (InvalidOperationException ex)
        {
            // Returns HTTP 400 with { message: "..." } — frontend can show why comment failed
            // Without this: client can't tell invalid project from server error
            return BadRequest(new { message = ex.Message });
        }
    }

    // Uses absolute route GET /api/projects/{projectId}/comments
    // Without this full path: frontend project comment list calls wouldn't match this action
    [HttpGet("/api/projects/{projectId}/comments")]

    // Returns all comments on a specific project, usually ordered by date
    // Without this method: project discussion panel has no data — thread stays empty
    public async Task<ActionResult<List<CommentResponse>>> GetByProject(int projectId)
    {
        // Delegates to CommentService — queries comments WHERE ProjectId = projectId
        // Without this: comment list is never fetched — response is always empty/crash
        var result = await _commentService.GetByProjectIdAsync(projectId);

        // Returns HTTP 200 with JSON array of comments
        // Without this: frontend can't render the project comment thread
        return Ok(result);
    }

    // ── Task Comments ───────────────────────────────────────────────────────────

    // Maps POST /api/comments/task to this method — separate from project comments
    // Without this route suffix: task and project comments would share the same POST URL — ambiguous
    [HttpPost("task")]

    // Adds a text comment to a specific task, attributed to the current user
    // Without this method: task-level discussion can't be posted — task detail comments broken
    public async Task<ActionResult<TaskCommentResponse>> CreateTaskComment([FromBody] TaskCommentCreateRequest request)
    {
        // Wraps service call so validation errors (e.g. task not found) become clean 400 responses
        // Without this try: InvalidOperationException becomes an unhandled 500 error
        try
        {
            // Delegates to CommentService — inserts task comment row, returns TaskCommentResponse
            // GetCurrentUserId() supplies author id — without it task comments have no author
            // Without this call: task comment is never persisted
            var result = await _commentService.CreateTaskCommentAsync(request, GetCurrentUserId());

            // Returns HTTP 200 with the new task comment JSON
            // Without this: client gets no body — UI can't show the new task comment
            return Ok(result);
        }
        // Catches business-rule failures like "task does not exist"
        // Without this catch: validation failures crash with 500 instead of readable 400
        catch (InvalidOperationException ex)
        {
            // Returns HTTP 400 with { message: "..." } — frontend can display the error
            // Without this: client can't distinguish bad task id from server error
            return BadRequest(new { message = ex.Message });
        }
    }

    // Uses absolute route GET /api/tasks/{taskId}/comments
    // Without this full path: frontend task comment list calls wouldn't match this action
    [HttpGet("/api/tasks/{taskId}/comments")]

    // Returns all comments on a specific task
    // Without this method: task detail comment section has no data
    public async Task<ActionResult<List<TaskCommentResponse>>> GetByTask(int taskId)
    {
        // Delegates to CommentService — queries task comments WHERE TaskId = taskId
        // Without this: task comment list is never fetched
        var result = await _commentService.GetByTaskIdAsync(taskId);

        // Returns HTTP 200 with JSON array of task comments
        // Without this: frontend can't render the task comment thread
        return Ok(result);
    }

    // ── Helper — Current User Id ──────────────────────────────────────────────────

    // Extracts the numeric user ID from the JWT token's NameIdentifier claim
    // Without this method: Create and CreateTaskComment can't attribute comments to the author
    private int GetCurrentUserId()
    {
        // Reads NameIdentifier claim set by TokenService when the JWT was issued
        // Without this: userIdClaim is null — int.Parse below throws and request returns 500
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Converts claim string to int — valid JWT always includes this claim
        // Without this: comments are saved without a valid AuthorId
        return int.Parse(userIdClaim!);
    }
}
