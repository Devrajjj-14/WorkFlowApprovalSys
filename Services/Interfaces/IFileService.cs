// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: FileUploadResponse and FileListResponse are unknown — method signatures fail to compile
using WorkflowApprovalApi.DTOs;

// Groups all service contracts in one namespace so Program.cs can register them with AddScoped<Interface, Implementation>()
namespace WorkflowApprovalApi.Services.Interfaces;

// Contract for file storage on projects — upload, list, and download attachments
// FileService implements this; FilesController calls it for all /api/files routes
// Without this interface: DI can't bind IFileService → FileService — file endpoints have no service to call
public interface IFileService
{
    // Saves an uploaded file to disk and records metadata (name, size, path) in the database
    // projectId links the file to a project; userId records who uploaded it for audit trail
    // Without this: POST /api/files/upload has no method to call — users can't attach files to projects
    Task<FileUploadResponse> UploadAsync(int projectId, IFormFile file, int userId);

    // Returns metadata for every file attached to a given project — used by the project files tab
    // Without this: GET /api/files/project/{projectId} has no method to call — file lists stay empty
    Task<List<FileListResponse>> GetByProjectIdAsync(int projectId);

    // Opens a readable stream for the file on disk so the controller can return it as a download
    // Returns null if the file id doesn't exist or the file was deleted from disk
    // Without this: GET /api/files/{fileId}/download has no method to call — files can't be downloaded
    Task<FileStream?> DownloadAsync(int fileId);
}
