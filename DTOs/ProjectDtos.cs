// ── Namespace ─────────────────────────────────────────────────────────────────
// Groups project-related request/response DTOs consumed by ProjectsController and ProjectService
// Without this: project endpoints cannot bind JSON bodies or shape API responses
namespace WorkflowApprovalApi.DTOs;

// ── ProjectCreateRequest — body for POST /api/projects ────────────────────────
// Only name and description come from the client — creator id and timestamps are set server-side
// Without this class: create project endpoint has no typed input — new projects cannot be created
public class ProjectCreateRequest
{
    // Project title entered on the create form
    // Without this: new projects are saved with empty names — lists become unusable
    public string Name { get; set; } = string.Empty;

    // Optional longer description of the project scope
    // Without this: description field on create form is dropped — detail pages stay blank
    public string Description { get; set; } = string.Empty;
}

// ── ProjectUpdateStatusRequest — body for PATCH status endpoints ───────────────
// Status is sent as a string (e.g. "InProgress") and parsed to ProjectStatus enum in the service
// Without this class: status update endpoint cannot read the new status from the request body
public class ProjectUpdateStatusRequest
{
    // Target status name matching ProjectStatus enum (Draft, InProgress, InReview, etc.)
    // Without this: UpdateStatusAsync receives empty string — enum parse fails with 400
    public string Status { get; set; } = string.Empty;
}

// ── ProjectResponse — JSON returned for project list and detail endpoints ───────
// Includes audit fields (creator/updater names) mapped from the Project entity + User joins
// Without this class: frontend ProjectResponse deserialization fails — project pages show nothing
public class ProjectResponse
{
    // Project primary key — used in URLs like /projects/{id}
    // Without this: UI cannot navigate to project detail or pass id to related API calls
    public int Id { get; set; }

    // Project title for list cards and page headers
    // Without this: project name column in the UI is always blank
    public string Name { get; set; } = string.Empty;

    // Project description text shown on detail views
    // Without this: description section on project page has no data
    public string Description { get; set; } = string.Empty;

    // Current status as a string (enum .ToString()) for badges and filters
    // Without this: status chips and workflow filters cannot display project state
    public string Status { get; set; } = string.Empty;

    // ── Audit: Creation ──────────────────────────────────────────────────────
    // These two fields are always populated — every project has a creator.
    // User id of whoever created the project — from Project.CreatedByUserId
    // Without this: audit UI cannot link "Created by" to a user record
    public int CreatedByUserId { get; set; }

    // Full name from Users table via CreatedByUser navigation — denormalized for the API
    // Without this: UI shows numeric id instead of "Created by Jane Smith"
    public string CreatedByName { get; set; } = string.Empty;   // Full name from Users table

    // When the project was first created — from Project.CreatedAt
    // Without this: "Created on" date is missing from project detail
    public DateTime CreatedAt { get; set; }

    // ── Audit: Last Update ───────────────────────────────────────────────────
    // These are nullable — null means the project was never edited after creation.
    // The UI uses: if UpdatedByUserId has value → show "Last updated by", else "Created by".
    // User id of whoever last changed status — null if never updated
    // Without this: UI cannot decide whether to show "Created by" vs "Last updated by"
    public int? UpdatedByUserId { get; set; }

    // Full name of last updater — null when UpdatedByUserId is null
    // Without this: "Last updated by" label has no name to display
    public string? UpdatedByName { get; set; }                  // null when never updated

    // Timestamp of last status change — still sent even when never updated (entity default)
    // Without this: relative "updated ago" timestamps are unavailable
    public DateTime UpdatedAt { get; set; }
}
