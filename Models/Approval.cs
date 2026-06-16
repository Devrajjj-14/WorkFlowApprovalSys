// ── Namespace ─────────────────────────────────────────────────────────────────
// Declares the Approval entity in WorkflowApprovalApi.Models for EF Core mapping
// Without this: AppDbContext.Approvals and approval services cannot reference this type
namespace WorkflowApprovalApi.Models;

// ── Approval Entity — maps to the Approvals table ─────────────────────────────
// Records a formal review request on a project (optionally tied to a specific file version)
// Without this class: the approve/reject/changes-requested workflow has no database home
public class Approval
{
    // Primary key — unique identifier for each approval request
    // Without this: individual approval rows cannot be updated or fetched by id
    public int Id { get; set; }

    // Foreign key — which project is being submitted for review
    // Without this: approvals are orphaned — project approval history is empty
    public int ProjectId { get; set; }

    // Optional foreign key — specific file version under review; null means whole-project approval
    // Without this: you cannot tie an approval to a particular uploaded document version
    public int? FileId { get; set; }

    // Foreign key to Users.Id — who submitted the approval request
    // Without this: RequestedByName is unknown — UI cannot show "Requested by Jane"
    public int RequestedByUserId { get; set; }

    // Nullable foreign key — who reviewed the request; null while Status is still Pending
    // Without this: ReviewedByName stays empty even after a reviewer acts
    public int? ReviewedByUserId { get; set; }

    // Current decision state — defaults to Pending until a reviewer updates it
    // Without this: pending vs decided approvals look identical in the database
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    // Free-text notes from requester or reviewer (context, rejection reason, etc.)
    // Without this: remarks from create/update requests have nowhere to persist
    public string Remarks { get; set; } = string.Empty;

    // When the approval request was first created
    // Without this: ApprovalResponse.RequestedAt is missing — timeline UI breaks
    public DateTime RequestedAt { get; set; }

    // When the reviewer made a decision — null while still Pending
    // Without this: you cannot show how long an approval waited or when it was decided
    public DateTime? ReviewedAt { get; set; }

    // ── Navigation Properties ─────────────────────────────────────────────────
    // Parent project being reviewed — loaded via ProjectId
    // Without this: .Include(a => a.Project) fails — project context on approval detail is lost
    public Project Project { get; set; } = null!;

    // Optional linked file — null when approval covers the whole project, not one file
    // Without this: file-specific approvals cannot load filename/version via navigation
    public UploadedFile? File { get; set; }

    // User who requested the approval — loaded via RequestedByUserId
    // Without this: RequestedByName requires an extra Users query on every approval list
    public User RequestedByUser { get; set; } = null!;

    // User who reviewed — null until ReviewedByUserId is set after a decision
    // Without this: ReviewedByName cannot be resolved from EF Include alone
    public User? ReviewedByUser { get; set; }
}
