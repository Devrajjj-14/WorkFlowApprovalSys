// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: [Authorize] attribute doesn't exist — anonymous users could upload files
using Microsoft.AspNetCore.Authorization;

// Without this: Controller, IActionResult, IFormFile, RedirectToAction don't exist — upload won't compile
using Microsoft.AspNetCore.Mvc;

// Without this: ApiService is unknown — this controller can't forward files to the backend
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

// FilesController handles file uploads on the project detail page
// Without this class: upload forms have no endpoint — project files can't be added from the UI
[Authorize]
public class FilesController : Controller
{
    // Injected ApiService for multipart POST /api/projects/{id}/files
    // Without this field: Upload action can't stream the file to the backend API
    private readonly ApiService _api;

    // DI injects ApiService per request
    // Without this constructor: FilesController can't be built — upload route fails at startup/use
    public FilesController(ApiService api) => _api = api;

    // ── Upload File ───────────────────────────────────────────────────────────
    // Validates the selected file and sends it to the backend for the given project
    // Without this action: file input form POST has no handler — uploads silently fail
    [HttpPost]
    public async Task<IActionResult> Upload(int projectId, IFormFile file)
    {
        // Rejects empty or missing file selection before calling the API
        // Without this check: backend gets invalid upload — user sees cryptic API error instead of clear message
        if (file == null || file.Length == 0)
        {
            // Flash error shown once on project detail after redirect
            // Without this: user gets no feedback that they forgot to pick a file
            TempData["Error"] = "Please select a file.";

            // Returns to project detail without calling the API
            // Without this: action continues and may throw or call API with bad data
            return RedirectToAction("Detail", "Projects", new { id = projectId });
        }

        // Forwards the file stream to the backend upload endpoint
        // Without this: file never leaves the browser server — project file list stays empty
        var (_, error) = await _api.UploadFileAsync(projectId, file);

        // If API returned an error (size, type, permissions), show it on the next page load
        // Without this: upload failures look like success — user thinks file was saved
        if (error != null) TempData["Error"] = error;

        // Always returns to project detail so the files section refreshes (success or error message)
        // Without this: browser shows raw POST response — user doesn't see updated file list
        return RedirectToAction("Detail", "Projects", new { id = projectId });
    }
}
