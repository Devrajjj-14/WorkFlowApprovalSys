namespace WorkflowApprovalApi.Models;

public class UploadedFile
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int VersionNumber { get; set; } = 1;
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }

    public Project Project { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}
