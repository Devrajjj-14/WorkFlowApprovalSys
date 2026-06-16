// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: Include, AnyAsync, FirstOrDefaultAsync, FirstAsync, MaxAsync, Add, SaveChangesAsync don't exist
using Microsoft.EntityFrameworkCore;

// Without this: AppDbContext is unknown — file metadata DB operations fail to compile
using WorkflowApprovalApi.Data;

// Without this: FileUploadResponse, FileListResponse are unknown — return types break
using WorkflowApprovalApi.DTOs;

// Without this: UploadedFile model is unknown — entity creation fails
using WorkflowApprovalApi.Models;

// Without this: IFileService interface is unknown — FileService can't implement the contract
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Services.Implementations;

// FileService handles file upload to disk, metadata in DB, listing, and download streams
// Called by FilesController — stores files under wwwroot/uploads with auto-incrementing version numbers
// Without this class: all /api/projects/{id}/files endpoints return 500 — uploads and downloads break
public class FileService : IFileService
{
    // EF Core context for UploadedFiles and Projects tables
    // Without this field: file metadata can't be saved or queried
    private readonly AppDbContext _context;

    // Provides WebRootPath (wwwroot folder) for physical file storage
    // Without this field: we don't know where on disk to save uploaded files
    private readonly IWebHostEnvironment _environment;

    // Logger for upload/download events and errors
    // Without this field: file operations aren't traceable in logs
    private readonly ILogger<FileService> _logger;

    // Constructor — DI injects database, hosting environment, and logger
    // Without this constructor: FileService can't be constructed — app crashes on startup
    public FileService(AppDbContext context, IWebHostEnvironment environment, ILogger<FileService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    // ── UploadAsync ───────────────────────────────────────────────────────────
    // Saves file to wwwroot/uploads, records metadata with next version number for the project
    // Without this: POST /api/projects/{id}/files/upload does nothing — Files tab can't receive files
    public async Task<FileUploadResponse> UploadAsync(int projectId, IFormFile file, int userId)
    {
        // Confirm project exists before accepting file — avoids orphan UploadedFile rows
        // Without this: FK violation or files attached to non-existent projects
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
        {
            throw new InvalidOperationException("Project not found.");
        }

        // Reject empty uploads — IFormFile can be null or zero-length from bad client requests
        // Without this: empty files would consume version numbers and disk paths
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("File is required.");
        }

        _logger.LogInformation(
            "Uploading file {FileName} to project {ProjectId} by user {UserId}",
            file.FileName,
            projectId,
            userId);

        // Find highest existing version for this project — new upload gets max + 1
        // MaxAsync on empty set returns null — ?? 0 starts versioning at 1
        // Without this: every upload would get VersionNumber 1 — version history broken
        var maxVersion = await _context.UploadedFiles
            .Where(f => f.ProjectId == projectId)
            .Select(f => (int?)f.VersionNumber)
            .MaxAsync() ?? 0;

        // Physical folder: wwwroot/uploads — created if missing
        // Without CreateDirectory: first upload on fresh deploy throws DirectoryNotFoundException
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsFolder);

        // Unique stored name = GUID + original extension — avoids collisions and hides original names on disk
        // Without GUID: two uploads with same name would overwrite each other
        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var physicalPath = Path.Combine(uploadsFolder, storedFileName);

        // Write uploaded stream to disk — await using disposes stream after write
        // Without this block: file bytes never reach disk — DB row would point to missing file
        await using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Build metadata row — FilePath is web-relative (uploads/...) for static file serving
        // Without this: no DB record — download endpoint can't find file by Id
        var uploadedFile = new UploadedFile
        {
            ProjectId = projectId,
            FileName = file.FileName,
            FilePath = Path.Combine("uploads", storedFileName).Replace("\\", "/"),
            VersionNumber = maxVersion + 1,
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow
        };

        // Persist metadata to MySQL
        // Without Add + SaveChangesAsync: file exists on disk but API can't list or download it
        _context.UploadedFiles.Add(uploadedFile);
        await _context.SaveChangesAsync();

        // Reload with UploadedByUser for complete response (if needed by mapper later)
        // Without Include: navigation properties empty — mapper still works but pattern matches other services
        var saved = await _context.UploadedFiles
            .Include(f => f.UploadedByUser)
            .FirstAsync(f => f.Id == uploadedFile.Id);

        _logger.LogInformation("File {FileId} uploaded successfully", saved.Id);
        return MapToResponse(saved);
    }

    // ── GetByProjectIdAsync ───────────────────────────────────────────────────
    // Lists all file metadata for a project, highest version first
    // Without this: GET /api/projects/{id}/files returns empty — Files tab shows nothing
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

    // ── DownloadAsync ─────────────────────────────────────────────────────────
    // Opens a read stream for the physical file — caller (controller) disposes the stream
    // Without this: GET download endpoint can't return file bytes — downloads always fail
    public async Task<FileStream?> DownloadAsync(int fileId)
    {
        _logger.LogDebug("Downloading file {FileId}", fileId);

        // Look up metadata by Id — null if unknown file Id
        // Without this: we'd try to open a path for a non-existent record
        var file = await _context.UploadedFiles.FirstOrDefaultAsync(f => f.Id == fileId);
        if (file == null)
        {
            return null;
        }

        // Resolve web-relative path to OS path under wwwroot
        // Replace "/" with platform separator for Windows/Linux compatibility
        // Without this: wrong path on Windows — File.Exists fails even when file exists
        var physicalPath = Path.Combine(_environment.WebRootPath, file.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!System.IO.File.Exists(physicalPath))
        {
            throw new FileNotFoundException("Physical file not found on disk.");
        }

        // Return open read stream — controller streams this to HTTP response
        // Without this return: download endpoint has no content to send
        return new FileStream(physicalPath, FileMode.Open, FileAccess.Read);
    }

    // ── MapToResponse ─────────────────────────────────────────────────────────
    // Maps UploadedFile entity to upload response DTO (returned immediately after upload)
    // Without this: upload endpoint returns wrong JSON shape
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

    // ── MapToListResponse ─────────────────────────────────────────────────────
    // Maps UploadedFile entity to list item DTO (used in project file listing)
    // Without this: list endpoint would expose full entity or wrong fields
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
