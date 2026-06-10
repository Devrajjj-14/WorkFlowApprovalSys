namespace WorkflowApprovalApi.Models;

public class WorkflowTask
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public Priority Priority { get; set; }
    public int AssignedToUserId { get; set; }
    public int AssignedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Project Project { get; set; } = null!;
    public User AssignedToUser { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
    public ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
}
