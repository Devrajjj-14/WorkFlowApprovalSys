using WorkflowApprovalApi.DTOs;

namespace WorkflowApprovalApi.Services.Interfaces;

public interface IFileService
{
    Task<FileUploadResponse> UploadAsync(int projectId, IFormFile file, int userId);
    Task<List<FileListResponse>> GetByProjectIdAsync(int projectId);
    Task<FileStream?> DownloadAsync(int fileId);
}
