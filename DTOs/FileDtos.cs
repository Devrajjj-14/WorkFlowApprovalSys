// ── Namespace ─────────────────────────────────────────────────────────────────
// Groups file upload/list response DTOs used by FilesController and FileService
// Without this: file endpoints return untyped objects — frontend deserialization breaks
namespace WorkflowApprovalApi.DTOs;

// ── FileUploadResponse — JSON returned immediately after POST /api/files/upload ─
// Confirms what was saved — includes id and version for follow-up approval links
// Without this class: upload endpoint has no typed response — UI cannot show success details
public class FileUploadResponse
{
    // New UploadedFile row id — used to link approvals to this specific version
    // Without this: UI cannot pass FileId when requesting file-specific approval
    public int Id { get; set; }

    // Project the file was attached to — confirms correct project context after upload
    // Without this: upload success message cannot confirm which project received the file
    public int ProjectId { get; set; }

    // Original filename as stored in the database — shown in file lists
    // Without this: upload confirmation shows no filename — user unsure what uploaded
    public string FileName { get; set; } = string.Empty;

    // Auto-incremented version number for this filename within the project
    // Without this: UI cannot display "v2", "v3" badges on repeated uploads
    public int VersionNumber { get; set; }

    // User id of uploader — from JWT, echoed for client-side audit display
    // Without this: upload history cannot show who performed the upload
    public int UploadedByUserId { get; set; }

    // Server timestamp when upload completed — for sorting and audit trails
    // Without this: "Uploaded at" column on file lists stays empty
    public DateTime UploadedAt { get; set; }
}

// ── FileListResponse — JSON for GET file list endpoints (per project) ───────────
// Slimmer than FileUploadResponse — omits ProjectId because the list is already scoped
// Without this class: project file list endpoint has no consistent response shape
public class FileListResponse
{
    // File row id — used for download URLs and approval FileId references
    // Without this: download and delete actions cannot target the correct file
    public int Id { get; set; }

    // Stored filename for display in the project's file table
    // Without this: file list rows show blank names
    public string FileName { get; set; } = string.Empty;

    // Version number distinguishing multiple uploads of the same logical file
    // Without this: users cannot tell which upload is the latest version
    public int VersionNumber { get; set; }

    // Who uploaded this version — for audit column in the file list
    // Without this: "Uploaded by" column is empty in the UI
    public int UploadedByUserId { get; set; }

    // When this version was uploaded — enables sort-by-date in the file list
    // Without this: file list cannot be ordered chronologically
    public DateTime UploadedAt { get; set; }
}
