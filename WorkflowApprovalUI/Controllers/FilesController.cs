using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

[Authorize]
public class FilesController : Controller
{
    private readonly ApiService _api;
    public FilesController(ApiService api) => _api = api;

    [HttpPost]
    public async Task<IActionResult> Upload(int projectId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file.";
            return RedirectToAction("Detail", "Projects", new { id = projectId });
        }
        var (_, error) = await _api.UploadFileAsync(projectId, file);
        if (error != null) TempData["Error"] = error;
        return RedirectToAction("Detail", "Projects", new { id = projectId });
    }
}
