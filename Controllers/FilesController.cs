// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: ClaimTypes.NameIdentifier is unknown — GetCurrentUserId() won't compile
using System.Security.Claims;

// Without this: [Authorize] doesn't exist — JWT requirement on this controller fails to compile
using Microsoft.AspNetCore.Authorization;

// Without this: [ApiController], [Route], IFormFile, File() result etc. don't exist — controller won't compile
using Microsoft.AspNetCore.Mvc;

// Without this: FileUploadResponse, FileListResponse are unknown — action return types fail
using WorkflowApprovalApi.DTOs;

// Without this: IFileService is unknown — FilesController can't be constructed via DI
using WorkflowApprovalApi.Services.Interfaces;

// Without this: FilesController lives in the global namespace — breaks project structure
namespace WorkflowApprovalApi.Controllers;

// Requires a valid JWT for every action — anonymous users can't upload or download files
// Without this: anyone could upload/download project files without logging in — major security risk
[Authorize]

// Marks this class as an API controller — enables multipart form binding for file uploads
// Without this: IFormFile binding on Upload may not work correctly
[ApiController]

// Sets the base URL prefix for conventional routes on this controller to /api/files
// Without this: relative routes like {fileId}/download won't resolve correctly
[Route("api/files")]

// FilesController handles file upload, listing by project, and download from wwwroot/uploads
// Without this class: no file endpoints exist — project attachments feature is completely broken
public class FilesController : ControllerBase
{
    // Holds the injected file service that saves to disk and records metadata in the database
    // Without this field: every action crashes — no file operations can run
    private readonly IFileService _fileService;

    // Constructor — DI injects IFileService (registered as FileService in Program.cs)
    // Without this constructor: DI can't create FilesController — all file endpoints return 500
    public FilesController(IFileService fileService)
    {
        // Stores the service instance for use in Upload, GetByProject, and Download
        // Without this assignment: _fileService stays null — NullReferenceException on every request
        _fileService = fileService;
    }

    // ── Upload File ─────────────────────────────────────────────────────────────

    // Uses absolute route POST /api/projects/{projectId}/files/upload — nested under projects
    // Without this full path: URL wouldn't match frontend multipart upload to /api/projects/3/files/upload
    [HttpPost("/api/projects/{projectId}/files/upload")]

    // Accepts a multipart file upload and attaches it to a project
    // Without this method: users can't attach documents, images, or assets to projects
    public async Task<ActionResult<FileUploadResponse>> Upload(int projectId, IFormFile file)
    {
        // Wraps service call so validation errors (e.g. empty file, project not found) become 400 responses
        // Without this try: InvalidOperationException becomes an unhandled 500 error
        try
        {
            // Reads the logged-in user's ID — recorded as uploader in the service/database
            // Without this: file metadata has no owner — audit trail is incomplete
            var userId = GetCurrentUserId();

            // Delegates to FileService — saves file to wwwroot/uploads/, inserts DB row, returns metadata
            // Without this: file never lands on disk or in the database
            var result = await _fileService.UploadAsync(projectId, file, userId);

            // Returns HTTP 200 with file id, name, size, upload date etc.
            // Without this: client gets no confirmation — UI can't show the new attachment
            return Ok(result);
        }
        // Catches business-rule failures like "file too large" or "invalid project"
        // Without this catch: validation failures crash with 500 instead of readable 400
        catch (InvalidOperationException ex)
        {
            // Returns HTTP 400 with { message: "..." } — frontend can show why upload failed
            // Without this: client can't distinguish bad file from server error
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── List Files By Project ─────────────────────────────────────────────────────

    // Uses absolute route GET /api/projects/{projectId}/files
    // Without this full path: frontend file list calls wouldn't match this action
    [HttpGet("/api/projects/{projectId}/files")]

    // Returns metadata for all files attached to a project (not the file bytes themselves)
    // Without this method: project file list page has no data — attachments panel stays empty
    public async Task<ActionResult<List<FileListResponse>>> GetByProject(int projectId)
    {
        // Delegates to FileService — queries file records WHERE ProjectId = projectId
        // Without this: file list is never fetched — response is always empty/crash
        var result = await _fileService.GetByProjectIdAsync(projectId);

        // Returns HTTP 200 with JSON array of file metadata
        // Without this: frontend can't render the attachments list
        return Ok(result);
    }

    // ── Download File ─────────────────────────────────────────────────────────────

    // Maps GET /api/files/{fileId}/download to this method
    // Without this: users can't retrieve file bytes — download links return 404
    [HttpGet("{fileId}/download")]

    // Streams the physical file from disk to the client as a binary download
    // Without this method: clicking download in the UI does nothing useful
    public async Task<IActionResult> Download(int fileId)
    {
        // Wraps service call so missing files on disk become clean 404 responses
        // Without this try: FileNotFoundException becomes an unhandled 500 error
        try
        {
            // Delegates to FileService — looks up DB record, reads bytes from wwwroot/uploads/
            // Without this: file path is never resolved — download always fails
            var result = await _fileService.DownloadAsync(fileId);

            // Checks whether the file record or physical file exists
            // Without this if: missing files might stream null — client gets corrupt empty download
            if (result == null)
            {
                // Returns HTTP 404 — frontend can show "File not found"
                // Without this: client gets 200 with empty body — confusing failed download
                return NotFound(new { message = "File not found." });
            }

            // Returns HTTP 200 with raw file bytes as application/octet-stream attachment
            // Without this: bytes never reach the browser — download never completes
            return File(result, "application/octet-stream", "file");
        }
        // Catches when DB record exists but physical file was deleted from disk
        // Without this catch: missing disk file crashes with 500 instead of 404
        catch (FileNotFoundException ex)
        {
            // Returns HTTP 404 with the exception message — explains file missing on server
            // Without this: client can't tell disk-missing from permission error
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Helper — Current User Id ──────────────────────────────────────────────────

    // Extracts the numeric user ID from the JWT token's NameIdentifier claim
    // Without this method: Upload can't record who uploaded the file
    private int GetCurrentUserId()
    {
        // Reads NameIdentifier claim set by TokenService when the JWT was issued
        // Without this: userIdClaim is null — int.Parse below throws and request returns 500
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Converts claim string to int — valid JWT always includes this claim
        // Without this: FileService never receives the uploader's user id
        return int.Parse(userIdClaim!);
    }
}
