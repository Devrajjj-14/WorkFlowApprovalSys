using Microsoft.EntityFrameworkCore;
using WorkflowApprovalApi.Data;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Models;
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Services.Implementations;

public class FileService : IFileService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileService> _logger;

    public FileService(AppDbContext context, IWebHostEnvironment environment, ILogger<FileService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<FileUploadResponse> UploadAsync(int projectId, IFormFile file, int userId)
    {
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
        {
            throw new InvalidOperationException("Project not found.");
        }

        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("File is required.");
        }

        _logger.LogInformation(
            "Uploading file {FileName} to project {ProjectId} by user {UserId}",
            file.FileName,
            projectId,
            userId);

        var maxVersion = await _context.UploadedFiles
            .Where(f => f.ProjectId == projectId)
            .Select(f => (int?)f.VersionNumber)
            .MaxAsync() ?? 0;

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var physicalPath = Path.Combine(uploadsFolder, storedFileName);

        await using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var uploadedFile = new UploadedFile
        {
            ProjectId = projectId,
            FileName = file.FileName,
            FilePath = Path.Combine("uploads", storedFileName).Replace("\\", "/"),
            VersionNumber = maxVersion + 1,
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow
        };

        _context.UploadedFiles.Add(uploadedFile);
        await _context.SaveChangesAsync();

        var saved = await _context.UploadedFiles
            .Include(f => f.UploadedByUser)
            .FirstAsync(f => f.Id == uploadedFile.Id);

        _logger.LogInformation("File {FileId} uploaded successfully", saved.Id);
        return MapToResponse(saved);
    }

    public async Task<List<FileListResponse>> GetByProjectIdAsync(int projectId)
    {
        _logger.LogDebug("Fetching files for project {ProjectId}", projectId);
        var files = await _context.UploadedFiles
            .Include(f => f.UploadedByUser)
            .Where(f => f.ProjectId == projectId)
            .OrderByDescending(f => f.VersionNumber)
            .ToListAsync();

        return files.Select(MapToListResponse).ToList();
    }

    public async Task<FileStream?> DownloadAsync(int fileId)
    {
        _logger.LogDebug("Downloading file {FileId}", fileId);
        var file = await _context.UploadedFiles.FirstOrDefaultAsync(f => f.Id == fileId);
        if (file == null)
        {
            return null;
        }

        var physicalPath = Path.Combine(_environment.WebRootPath, file.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!System.IO.File.Exists(physicalPath))
        {
            throw new FileNotFoundException("Physical file not found on disk.");
        }

        return new FileStream(physicalPath, FileMode.Open, FileAccess.Read);
    }

    private static FileUploadResponse MapToResponse(UploadedFile file)
    {
        return new FileUploadResponse
        {
            Id = file.Id,
            ProjectId = file.ProjectId,
            FileName = file.FileName,
            VersionNumber = file.VersionNumber,
            UploadedByUserId = file.UploadedByUserId,
            UploadedAt = file.UploadedAt
        };
    }

    private static FileListResponse MapToListResponse(UploadedFile file)
    {
        return new FileListResponse
        {
            Id = file.Id,
            FileName = file.FileName,
            VersionNumber = file.VersionNumber,
            UploadedByUserId = file.UploadedByUserId,
            UploadedAt = file.UploadedAt
        };
    }
}
