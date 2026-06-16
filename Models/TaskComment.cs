// ── Namespace ─────────────────────────────────────────────────────────────────
// Declares TaskComment in WorkflowApprovalApi.Models for EF Core and services
// Without this: task-level comment endpoints have no entity to map to the database
namespace WorkflowApprovalApi.Models;

// ── TaskComment Entity — maps to the TaskComments table ───────────────────────
// Comment attached to a specific WorkflowTask (distinct from project-level Comment)
// Without this class: task discussion threads cannot be stored or returned via API
public class TaskComment
{
    // Primary key — unique identifier for each task comment row
    // Without this: individual task comments cannot be fetched or deleted by id
    public int Id { get; set; }

    // Foreign key — which workflow task this comment is posted on
    // Without this: comments are not linked to tasks — task detail pages show no thread
    public int TaskId { get; set; }

    // Foreign key — which user authored the comment (from JWT on POST)
    // Without this: task comment author id is unknown — attribution breaks
    public int UserId { get; set; }

    // The comment text shown on the task detail page
    // Without this: TaskCommentCreateRequest.Message has nowhere to persist
    public string Message { get; set; } = string.Empty;

    // Server-set timestamp when the comment was created
    // Without this: TaskCommentResponse.CreatedAt is empty — thread ordering fails
    public DateTime CreatedAt { get; set; }

    // ── Navigation Properties ─────────────────────────────────────────────────
    // Parent workflow task — loaded via TaskId
    // Without this: .Include(tc => tc.Task) fails — task title on comment list is unavailable
    public WorkflowTask Task { get; set; } = null!;

    // Comment author — loaded via UserId for full name in responses
    // Without this: UI must manually join Users to show who wrote each task comment
    public User User { get; set; } = null!;
}
