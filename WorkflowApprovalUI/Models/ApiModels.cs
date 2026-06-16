// ── Namespace ─────────────────────────────────────────────────────────────────
// Without this: types wouldn't live in WorkflowApprovalUI.Models — controllers and ApiService couldn't import them
namespace WorkflowApprovalUI.Models;

// ── Auth ──────────────────────────────────────────────────────────────────────

// LoginViewModel binds the email/password fields on Auth/Login.cshtml
// Without this class: login form can't model-bind — POST Login action receives empty values
public class LoginViewModel
{
    // User's email address from the login form
    // Without this: email isn't captured — API login called with blank email
    public string Email { get; set; } = string.Empty;

    // User's password from the login form
    // Without this: password isn't captured — API login always fails authentication
    public string Password { get; set; } = string.Empty;
}

// RegisterViewModel binds the sign-up form on Auth/Register.cshtml
// Without this class: register POST can't bind FullName, Email, Password, Role — registration breaks
public class RegisterViewModel
{
    // Display name chosen at registration
    // Without this: new users have no name in API payload — profile shows blank
    public string FullName { get; set; } = string.Empty;

    // Email for the new account — also used as login identifier
    // Without this: backend can't create user with unique email
    public string Email { get; set; } = string.Empty;

    // Password for the new account
    // Without this: account created without password — login impossible
    public string Password { get; set; } = string.Empty;

    // Role selected in register dropdown (default Designer)
    // Without this: new users get wrong or empty role — authorization rules break
    public string Role { get; set; } = "Designer";
}

// AuthResponse mirrors JSON returned by POST /api/auth/login and register success paths
// Without this class: ApiService can't deserialize login response — token and user info lost
public class AuthResponse
{
    // Database id of the authenticated user
    // Without this: session can't store UserId — identity claims incomplete
    public int UserId { get; set; }

    // Human-readable name shown in layout and claims
    // Without this: UI shows empty name after login
    public string FullName { get; set; } = string.Empty;

    // Email returned from API for claims and display
    // Without this: ClaimTypes.Email missing — email-based UI breaks
    public string Email { get; set; } = string.Empty;

    // Role string (Admin, Manager, Designer, etc.) for [Authorize(Roles=...)]
    // Without this: role-based pages block everyone or allow wrong access
    public string Role { get; set; } = string.Empty;

    // JWT token stored in session for ApiService to attach to backend calls
    // Without this: session has no token — all API calls return 401 after login
    public string Token { get; set; } = string.Empty;
}

// ── Projects ──────────────────────────────────────────────────────────────────

// ProjectResponse mirrors a single project from GET /api/projects and GET /api/projects/{id}
// Without this class: project list and detail can't deserialize API JSON — pages show empty data
public class ProjectResponse
{
    // Primary key of the project
    // Without this: links to detail, delete, and updates use wrong id
    public int Id { get; set; }

    // Project title shown on cards and detail header
    // Without this: UI shows blank project names
    public string Name { get; set; } = string.Empty;

    // Longer description text on detail page
    // Without this: description section empty
    public string Description { get; set; } = string.Empty;

    // Workflow status (e.g. Active, OnHold, Completed)
    // Without this: status badges and filters don't work
    public string Status { get; set; } = string.Empty;

    // User id who originally created the project — used by AuditHelper
    // Without this: "Created by" audit line can't show creator id prefix
    public int CreatedByUserId { get; set; }

    // Display name of creator — shown in audit label
    // Without this: audit shows empty creator name
    public string CreatedByName { get; set; } = string.Empty;

    // UTC timestamp when project was created — TimeAgo in audit
    // Without this: "Created ... ago" text missing
    public DateTime CreatedAt { get; set; }

    // Null if project was never edited after creation — AuditHelper uses this to pick label
    // Without this: can't distinguish created-only vs last-updated audit line
    public int? UpdatedByUserId { get; set; }

    // Name of last editor — null when never updated
    // Without this: "Last updated by" line has no name
    public string? UpdatedByName { get; set; }

    // Timestamp of last update — used when showing edit audit
    // Without this: last-updated relative time can't be computed
    public DateTime UpdatedAt { get; set; }
}

// ProjectCreateViewModel binds the "new project" form fields
// Without this class: Create POST can't read Name and Description from form
public class ProjectCreateViewModel
{
    // Name input on create project page
    // Without this: API receives empty project name
    public string Name { get; set; } = string.Empty;

    // Description textarea on create project page
    // Without this: new projects saved without description
    public string Description { get; set; } = string.Empty;
}

// ── Tasks ─────────────────────────────────────────────────────────────────────

// TaskResponse mirrors task JSON from GET /api/projects/{id}/tasks
// Without this class: task rows on project detail can't bind API data
public class TaskResponse
{
    // Task primary key — used in status update and delete forms
    // Without this: task actions target wrong or zero id
    public int Id { get; set; }

    // Parent project id — used in redirects and create forms
    // Without this: task can't be associated back to project in UI
    public int ProjectId { get; set; }

    // Short task title in task list
    // Without this: task rows show blank titles
    public string Title { get; set; } = string.Empty;

    // Task body text / instructions
    // Without this: detail text for task missing
    public string Description { get; set; } = string.Empty;

    // Current task status (Todo, InProgress, Done, etc.)
    // Without this: status dropdown and badges empty
    public string Status { get; set; } = string.Empty;

    // Priority level (Low, Medium, High)
    // Without this: priority labels don't render
    public string Priority { get; set; } = string.Empty;

    // User id task is assigned to
    // Without this: assignee column empty
    public int AssignedToUserId { get; set; }

    // User id who created/assigned the task — audit "Created by"
    // Without this: AuditHelper can't format creator id for tasks
    public int AssignedByUserId { get; set; }

    // Name of assigner — shown as creator in audit when never updated
    // Without this: task audit "Created by" name blank
    public string AssignedByName { get; set; } = string.Empty;

    // When task was created — relative time in audit
    // Without this: "Created ... ago" missing on tasks
    public DateTime CreatedAt { get; set; }

    // Null if task status never changed — audit shows creator instead of updater
    // Without this: can't tell if task was only created vs later updated
    public int? UpdatedByUserId { get; set; }

    // Name of user who last changed status
    // Without this: "Last updated by" line empty
    public string? UpdatedByName { get; set; }

    // When status was last changed
    // Without this: last-update TimeAgo wrong or missing
    public DateTime UpdatedAt { get; set; }
}

// TaskCreateViewModel binds "add task" form on project detail
// Without this class: TasksController.Create can't read form fields
public class TaskCreateViewModel
{
    // Hidden or route field tying task to current project
    // Without this: new task not linked to project — redirect and API fail
    public int ProjectId { get; set; }

    // Task title from form
    // Without this: API creates task with empty title
    public string Title { get; set; } = string.Empty;

    // Task description from form
    // Without this: task saved without description text
    public string Description { get; set; } = string.Empty;

    // Selected assignee user id from dropdown
    // Without this: task unassigned in backend
    public int AssignedToUserId { get; set; }

    // Priority chosen on form (defaults Medium)
    // Without this: all tasks get wrong default priority
    public string Priority { get; set; } = "Medium";
}

// ── Approvals ─────────────────────────────────────────────────────────────────

// ApprovalResponse mirrors approval cards from GET /api/projects/{id}/approvals
// Without this class: approvals tab can't show status, remarks, or audit trail
public class ApprovalResponse
{
    // Approval record id — used by Approve/Reject/RequestChanges POSTs
    // Without this: review buttons submit wrong id
    public int Id { get; set; }

    // Project this approval belongs to
    // Without this: redirects after review go to wrong project
    public int ProjectId { get; set; }

    // Optional linked file id when approval is for a specific upload
    // Without this: can't show which file version is under review
    public int? FileId { get; set; }

    // Pending, Approved, Rejected, ChangesRequested
    // Without this: status badges and AuditHelper verb selection break
    public string Status { get; set; } = string.Empty;

    // Free-text remarks from requester or reviewer
    // Without this: approval cards show no comments
    public string Remarks { get; set; } = string.Empty;

    // User who submitted the approval request
    // Without this: requester audit id prefix missing
    public int RequestedByUserId { get; set; }

    // Requester display name — ApprovalRequesterLabel
    // Without this: "Requested by" line has no name
    public string RequestedByName { get; set; } = string.Empty;

    // When approval was requested — TimeAgo on requester line
    // Without this: request timestamp missing in UI
    public DateTime RequestedAt { get; set; }

    // Null while still Pending — reviewer label hidden until set
    // Without this: AuditHelper can't know if review happened
    public int? ReviewedByUserId { get; set; }

    // Reviewer name after approve/reject/request changes
    // Without this: reviewer audit line empty
    public string? ReviewedByName { get; set; }

    // When review action occurred — null until reviewed
    // Without this: "Approved ... ago" time missing
    public DateTime? ReviewedAt { get; set; }
}

// ApprovalCreateViewModel binds "request approval" form on project detail
// Without this class: ApprovalsController.Create can't bind project, file, remarks
public class ApprovalCreateViewModel
{
    // Current project id from hidden field
    // Without this: approval created for wrong project or fails validation
    public int ProjectId { get; set; }

    // Optional file to attach to approval request
    // Without this: approvals can't reference a specific uploaded file
    public int? FileId { get; set; }

    // Initial remarks when submitting request
    // Without this: request sent with empty message
    public string Remarks { get; set; } = string.Empty;
}

// ── Comments ──────────────────────────────────────────────────────────────────

// CommentResponse mirrors project-level comment from GET /api/projects/{id}/comments
// Without this class: comments tab can't render thread from API
public class CommentResponse
{
    // Comment id (display or future delete/edit)
    // Without this: can't uniquely identify comment rows
    public int Id { get; set; }

    // Project the comment belongs to
    // Without this: comment context lost if aggregated elsewhere
    public int ProjectId { get; set; }

    // Author user id
    // Without this: can't show who wrote the comment if name not denormalized
    public int UserId { get; set; }

    // Comment body text
    // Without this: comment bubbles empty
    public string Message { get; set; } = string.Empty;

    // When comment was posted
    // Without this: no timestamp on comment rows
    public DateTime CreatedAt { get; set; }
}

// ── Task Comments ─────────────────────────────────────────────────────────────

// TaskCommentResponse mirrors GET /api/tasks/{id}/comments entries
// Without this class: inline task comments under each task can't deserialize
public class TaskCommentResponse
{
    // Comment primary key
    // Without this: task comment rows not uniquely identifiable
    public int Id { get; set; }

    // Parent task id
    // Without this: comments can't be grouped under correct task in dictionary
    public int TaskId { get; set; }

    // Author user id
    // Without this: author attribution missing
    public int UserId { get; set; }

    // Comment message text
    // Without this: task thread shows blank messages
    public string Message { get; set; } = string.Empty;

    // Post timestamp
    // Without this: task comments show no "when"
    public DateTime CreatedAt { get; set; }
}

// ── Files ─────────────────────────────────────────────────────────────────────

// FileListResponse mirrors file metadata from GET /api/projects/{id}/files
// Without this class: files tab can't list uploads from API
public class FileListResponse
{
    // File record id — used when linking approval to a file
    // Without this: file-specific approvals can't reference upload
    public int Id { get; set; }

    // Original filename shown in files list
    // Without this: downloads list shows blank names
    public string FileName { get; set; } = string.Empty;

    // Version number for same logical file re-uploads
    // Without this: version history not visible in UI
    public int VersionNumber { get; set; }

    // Who uploaded the file
    // Without this: uploader column empty
    public int UploadedByUserId { get; set; }

    // When file was uploaded
    // Without this: no upload date on file rows
    public DateTime UploadedAt { get; set; }
}

// ── Project Detail (composite) ────────────────────────────────────────────────

// ProjectDetailViewModel aggregates everything Detail.cshtml needs across tabs in one object
// Without this class: Detail view can't receive project + tasks + approvals + comments + files together
public class ProjectDetailViewModel
{
    // Header project info and audit fields
    // Without this: detail page has no project title/status block
    public ProjectResponse Project { get; set; } = null!;

    // All tasks for Tasks tab
    // Without this: Tasks tab model null — empty task list
    public List<TaskResponse> Tasks { get; set; } = new();

    // All approvals for Approvals tab
    // Without this: Approvals tab empty
    public List<ApprovalResponse> Approvals { get; set; } = new();

    // Project-wide comments for Comments tab
    // Without this: Comments tab empty
    public List<CommentResponse> Comments { get; set; } = new();

    // Uploaded files for Files tab
    // Without this: Files tab empty
    public List<FileListResponse> Files { get; set; } = new();

    // Maps each task id to its comment list for nested display under task rows
    // Without this: per-task comment threads can't be looked up in the view
    public Dictionary<int, List<TaskCommentResponse>> TaskComments { get; set; } = new();
}
