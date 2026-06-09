namespace WorkflowApprovalApi.Models;

public class Approval
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? FileId { get; set; }
    public int RequestedByUserId { get; set; }
    public int? ReviewedByUserId { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string Remarks { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public Project Project { get; set; } = null!;
    public UploadedFile? File { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
