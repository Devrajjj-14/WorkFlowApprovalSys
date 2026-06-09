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
