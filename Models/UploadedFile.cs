// ── Namespace ─────────────────────────────────────────────────────────────────
// Keeps UploadedFile in WorkflowApprovalApi.Models for EF Core and file services
// Without this: FilesController and FileService cannot reference the entity type
namespace WorkflowApprovalApi.Models;

// ── UploadedFile Entity — maps to the UploadedFiles table ─────────────────────
// Metadata for a file stored on disk under wwwroot/uploads — one row per uploaded version
// Without this class: file upload/download and file-linked approvals have no DB record
public class UploadedFile
{
    // Primary key — unique identifier for each file metadata row
    // Without this: download and approval endpoints cannot look up files by id
    public int Id { get; set; }

    // Foreign key — which project owns this file
    // Without this: files are orphaned — project file lists return empty
    public int ProjectId { get; set; }

    // Original filename as uploaded by the user (e.g. "design-v2.pdf")
    // Without this: download responses and UI lists show blank filenames
    public string FileName { get; set; } = string.Empty;

    // Relative path on disk under wwwroot (e.g. uploads/project-1/guid.pdf)
    // Without this: the server cannot locate the physical file for download or delete
    public string FilePath { get; set; } = string.Empty;

    // Incrementing version number per project+filename — defaults to 1 on first upload
    // Without this: multiple versions of the same file cannot be distinguished
    public int VersionNumber { get; set; } = 1;

    // Foreign key — which user performed the upload (from JWT claim)
    // Without this: upload audit trail loses who uploaded each file version
    public int UploadedByUserId { get; set; }

    // When the file was uploaded — set server-side on POST
    // Without this: FileUploadResponse.UploadedAt is missing — sort-by-date breaks
    public DateTime UploadedAt { get; set; }

    // ── Navigation Properties ─────────────────────────────────────────────────
    // Parent project — loaded via ProjectId
    // Without this: .Include(f => f.Project) fails — project context on file detail is lost
    public Project Project { get; set; } = null!;

    // User who uploaded the file — loaded via UploadedByUserId
    // Without this: uploader name requires a manual Users join on every file list
    public User UploadedByUser { get; set; } = null!;

    // Approval requests that reference this specific file (FileId on Approval)
    // Without this: file-specific approval history cannot be eager-loaded with the file
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}
