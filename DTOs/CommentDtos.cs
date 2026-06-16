// ── Namespace ─────────────────────────────────────────────────────────────────
// Groups comment DTOs for both project-level and task-level discussion endpoints
// Without this: CommentsController cannot bind create requests or shape list responses
namespace WorkflowApprovalApi.DTOs;

// ── CommentCreateRequest — body for POST /api/comments (project-level) ────────
// UserId comes from JWT server-side — client only sends project id and message text
// Without this class: post project comment endpoint cannot deserialize the request body
public class CommentCreateRequest
{
    // Which project to attach the comment to — must reference an existing Project.Id
    // Without this: comments save with ProjectId 0 — FK violation or wrong project thread
    public int ProjectId { get; set; }

    // Comment body text from the discussion form
    // Without this: empty comments are stored — discussion thread shows blank messages
    public string Message { get; set; } = string.Empty;
}

// ── CommentResponse — JSON returned for project comment list endpoints ────────
// Mirrors Comment entity fields exposed to the client (author id for display lookups)
// Without this class: project discussion UI cannot deserialize comment API responses
public class CommentResponse
{
    // Comment primary key — useful if delete or edit endpoints are added later
    // Without this: UI cannot reference individual comments by id
    public int Id { get; set; }

    // Parent project id — groups comments into the correct project thread
    // Without this: mixed comment threads appear when loading multiple projects
    public int ProjectId { get; set; }

    // Author user id — can be joined client-side to show avatar or profile link
    // Without this: comment attribution falls back to unknown author
    public int UserId { get; set; }

    // Comment text content displayed in the thread
    // Without this: message bubbles render empty in the discussion UI
    public string Message { get; set; } = string.Empty;

    // When the comment was posted — for chronological sorting in the thread
    // Without this: comments appear in random order — timeline UX breaks
    public DateTime CreatedAt { get; set; }
}

// ── Task Comments ─────────────────────────────────────────
// DTOs below mirror CommentDtos but for task-scoped discussion (TaskComments table)

// ── TaskCommentCreateRequest — body for POST /api/tasks/{id}/comments ─────────
// UserId is taken from JWT — client sends task id and message only
// Without this class: post task comment endpoint cannot bind the request body
public class TaskCommentCreateRequest
{
    // Which workflow task to attach the comment to — must reference WorkflowTask.Id
    // Without this: task comments save with TaskId 0 — wrong thread or FK error
    public int TaskId { get; set; }

    // Comment body from the task detail discussion form
    // Without this: task comment rows persist with empty Message values
    public string Message { get; set; } = string.Empty;
}

// ── TaskCommentResponse — JSON returned for task comment list endpoints ─────────
// Same shape as CommentResponse but scoped to TaskId instead of ProjectId
// Without this class: task detail discussion panel cannot deserialize API responses
public class TaskCommentResponse
{
    // Task comment primary key
    // Without this: individual task comments cannot be targeted by id
    public int Id { get; set; }

    // Parent task id — keeps comments in the correct task thread
    // Without this: comments from different tasks may appear mixed together
    public int TaskId { get; set; }

    // Author user id from Comment.UserId / TaskComment.UserId
    // Without this: task comment author cannot be identified in the UI
    public int UserId { get; set; }

    // Comment text shown on the task detail page
    // Without this: task discussion bubbles render with no text
    public string Message { get; set; } = string.Empty;

    // Server-set creation timestamp for ordering the task comment thread
    // Without this: task comments cannot be sorted newest-first or oldest-first
    public DateTime CreatedAt { get; set; }
}
