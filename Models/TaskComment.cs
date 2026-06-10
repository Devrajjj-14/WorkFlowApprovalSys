namespace WorkflowApprovalApi.Models;

public class TaskComment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public WorkflowTask Task { get; set; } = null!;
    public User User { get; set; } = null!;
}
