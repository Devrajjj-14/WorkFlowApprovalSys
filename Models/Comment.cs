// ── Namespace ─────────────────────────────────────────────────────────────────
// Places Comment in WorkflowApprovalApi.Models alongside other EF entities
// Without this: CommentDtos and CommentService imports of the entity type fail
namespace WorkflowApprovalApi.Models;

// ── Comment Entity — maps to the Comments table ───────────────────────────────
// Project-level discussion message (not tied to a specific task — see TaskComment for that)
// Without this class: project chat/discussion features cannot persist messages
public class Comment
{
    // Primary key — unique identifier for each comment row
    // Without this: individual comments cannot be deleted or referenced by id
    public int Id { get; set; }

    // Foreign key — which project this comment belongs to
    // Without this: comments are orphaned — project comment threads return nothing
    public int ProjectId { get; set; }

    // Foreign key — which user wrote the comment (from JWT on POST)
    // Without this: comment author cannot be identified or joined to Users
    public int UserId { get; set; }

    // The actual comment text body shown in the project discussion UI
    // Without this: POST /api/comments has nowhere to store the message content
    public string Message { get; set; } = string.Empty;

    // When the comment was posted — set server-side on create
    // Without this: CommentResponse.CreatedAt is missing — chronological sorting breaks
    public DateTime CreatedAt { get; set; }

    // ── Navigation Properties ─────────────────────────────────────────────────
    // Parent project — loaded via ProjectId for Include-based queries
    // Without this: EF cannot traverse Comment → Project without manual joins
    public Project Project { get; set; } = null!;

    // Author of the comment — loaded via UserId for display name resolution
    // Without this: author full name requires a separate Users lookup per comment
    public User User { get; set; } = null!;
}
