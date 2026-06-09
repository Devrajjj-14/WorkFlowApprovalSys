using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalUI.Models;
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly ApiService _api;
    public ProjectsController(ApiService api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var projects = await _api.GetProjectsAsync();
        return View(projects);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(ProjectCreateViewModel vm)
    {
        var (data, error) = await _api.CreateProjectAsync(vm.Name, vm.Description);
        if (data == null)
        {
            ViewBag.Error = error;
            return View(vm);
        }
        TempData["Success"] = "Project created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detail(int id)
    {
        var project = await _api.GetProjectAsync(id);
        if (project == null) return NotFound();

        var tasks     = await _api.GetTasksAsync(id);
        var approvals = await _api.GetApprovalsAsync(id);
        var comments  = await _api.GetCommentsAsync(id);
        var files     = await _api.GetFilesAsync(id);

        var vm = new ProjectDetailViewModel
        {
            Project   = project,
            Tasks     = tasks,
            Approvals = approvals,
            Comments  = comments,
            Files     = files
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        await _api.UpdateProjectStatusAsync(id, status);
        return RedirectToAction(nameof(Detail), new { id });
    }
}
