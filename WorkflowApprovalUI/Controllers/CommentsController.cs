using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private readonly ApiService _api;
    public CommentsController(ApiService api) => _api = api;

    [HttpPost]
    public async Task<IActionResult> Create(int projectId, string message)
    {
        await _api.CreateCommentAsync(projectId, message);
        return RedirectToAction("Detail", "Projects", new { id = projectId, tab = "comments" });
    }
}
