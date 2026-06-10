namespace WorkflowApprovalApi.DTOs;

public class ApprovalCreateRequest
{
    public int ProjectId { get; set; }
    public int? FileId { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class ApprovalUpdateRequest
{
    public string? Remarks { get; set; }
}

public class ApprovalResponse
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? FileId { get; set; }
    public int RequestedByUserId { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
