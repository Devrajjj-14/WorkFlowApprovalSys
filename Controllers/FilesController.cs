using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Controllers;

[Authorize]
[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("/api/projects/{projectId}/files/upload")]
    public async Task<ActionResult<FileUploadResponse>> Upload(int projectId, IFormFile file)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _fileService.UploadAsync(projectId, file, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("/api/projects/{projectId}/files")]
    public async Task<ActionResult<List<FileListResponse>>> GetByProject(int projectId)
    {
        var result = await _fileService.GetByProjectIdAsync(projectId);
        return Ok(result);
    }

    [HttpGet("{fileId}/download")]
    public async Task<IActionResult> Download(int fileId)
    {
        try
        {
            var result = await _fileService.DownloadAsync(fileId);
            if (result == null)
            {
                return NotFound(new { message = "File not found." });
            }

            return File(result, "application/octet-stream", "file");
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim!);
    }
}
