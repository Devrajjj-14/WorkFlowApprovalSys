namespace WorkflowApprovalApi.DTOs;

public class CommentCreateRequest
{
    public int ProjectId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CommentResponse
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ── Task Comments ─────────────────────────────────────────
public class TaskCommentCreateRequest
{
    public int TaskId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class TaskCommentResponse
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
