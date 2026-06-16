// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: ApprovalCreateRequest, ApprovalResponse, and ApprovalUpdateRequest are unknown — signatures fail to compile
using WorkflowApprovalApi.DTOs;

// Groups all service contracts in one namespace so Program.cs can register them with AddScoped<Interface, Implementation>()
namespace WorkflowApprovalApi.Services.Interfaces;

// Contract for the approval workflow — request review, approve, reject, or request changes on a project
// ApprovalService implements this; ApprovalsController calls it for all /api/approvals routes
// Without this interface: DI can't bind IApprovalService → ApprovalService — approval endpoints have no service to call
public interface IApprovalService
{
    // Submits a project for approval — creates a pending approval record linked to the project
    // userId identifies who requested the review — stored for audit trail
    // Without this: POST /api/approvals has no method to call — projects can't be sent for review
    Task<ApprovalResponse> CreateAsync(ApprovalCreateRequest request, int userId);

    // Returns all approval records for a given project — shows review history and current status
    // Without this: GET /api/approvals/project/{projectId} has no method to call — approval history can't be viewed
    Task<List<ApprovalResponse>> GetByProjectIdAsync(int projectId);

    // Marks a pending approval as approved — may advance the project's workflow status
    // userId records the reviewer who approved — written to the audit trail
    // Without this: POST /api/approvals/{id}/approve has no method to call — reviewers can't approve projects
    Task<ApprovalResponse?> ApproveAsync(int id, ApprovalUpdateRequest request, int userId);

    // Marks a pending approval as rejected — typically stops or rolls back the project workflow
    // userId records the reviewer who rejected — written to the audit trail
    // Without this: POST /api/approvals/{id}/reject has no method to call — reviewers can't reject projects
    Task<ApprovalResponse?> RejectAsync(int id, ApprovalUpdateRequest request, int userId);

    // Marks a pending approval as needing changes — sends the project back to the submitter with feedback
    // userId records the reviewer who requested changes — written to the audit trail
    // Without this: POST /api/approvals/{id}/request-changes has no method to call — reviewers can't ask for revisions
    Task<ApprovalResponse?> RequestChangesAsync(int id, ApprovalUpdateRequest request, int userId);
}
