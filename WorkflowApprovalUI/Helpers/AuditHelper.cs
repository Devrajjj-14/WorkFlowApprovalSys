// ── Namespace ─────────────────────────────────────────────────────────────────
// Without this: AuditHelper wouldn't be in WorkflowApprovalUI.Helpers — views couldn't @using it
namespace WorkflowApprovalUI.Helpers;

// AuditHelper builds human-readable HTML audit lines for project, task, and approval cards
// Centralizes date math and wording so .cshtml stays simple and one place controls labels
// Without this class: every view duplicates audit HTML — inconsistent wording and hard maintenance
public static class AuditHelper
{
    // ── Time-Ago Formatter ────────────────────────────────────────────────────
    // Converts a UTC DateTime to short relative text like "5 mins ago" or "Jun 12, 2026"
    // Without this method: views show raw timestamps or duplicate elapsed-time logic everywhere
    public static string TimeAgo(DateTime utcTime)
    {
        // Default/unset DateTime means no real timestamp — safe fallback for bad data
        // Without this: MinValue shows nonsense like huge negative "days ago"
        if (utcTime == DateTime.MinValue) return "unknown";

        // Difference between now (UTC) and the event time
        // Without this: can't compute minutes/hours/days elapsed
        var elapsed = DateTime.UtcNow - utcTime;

        // Under 30 seconds feels "just happened" — also covers slight clock skew (negative elapsed)
        // Without this: sub-minute events show awkward "0 mins ago"
        if (elapsed.TotalSeconds < 30)  return "Just now";

        // Single minute bucket for 30s–2min range
        // Without this: "1 min ago" never appears — jumps straight to "2 mins ago"
        if (elapsed.TotalMinutes < 2)   return "1 min ago";

        // Whole minutes under one hour
        // Without this: recent comments show hours instead of minutes
        if (elapsed.TotalMinutes < 60)  return $"{(int)elapsed.TotalMinutes} mins ago";

        // Single hour bucket for 60–120 minutes
        // Without this: "1 hr ago" skipped
        if (elapsed.TotalHours < 2)     return "1 hr ago";

        // Whole hours under one day
        // Without this: same-day events show "1 day ago" too early
        if (elapsed.TotalHours < 24)    return $"{(int)elapsed.TotalHours} hrs ago";

        // Single day bucket for 24–48 hours
        // Without this: "1 day ago" never shown
        if (elapsed.TotalDays < 2)      return "1 day ago";

        // Days under a year — still scannable as relative
        // Without this: week-old items jump to full date immediately
        if (elapsed.TotalDays < 365)    return $"{(int)elapsed.TotalDays} days ago";

        // Very old records: absolute date is clearer than "400 days ago"
        // Without this: ancient audit lines become unreadably large day counts
        return utcTime.ToString("MMM d, yyyy");
    }

    // ── User ID Prefix Formatter ──────────────────────────────────────────────
    // Formats numeric user id as ADM-001 or EMP-1024 for display in audit lines
    // Without this method: audit shows raw ids — doesn't match spec-style prefixed labels
    private static string FormatUserId(int userId)
    {
        // Heuristic: low ids (seed/admin) get ADM prefix, others EMP — easy to change in one place
        // Without this: all users show same prefix or raw number only
        var prefix = userId <= 10 ? "ADM" : "EMP";

        // Zero-pad to 3 digits minimum (5 → 005) for consistent width in UI
        // Without this: ids align poorly and don't match ADM-001 style examples
        return $"{prefix}-{userId:D3}";
    }

    // ── Project Audit Label ───────────────────────────────────────────────────
    // Returns HTML span for project card: "Created by" or "Last updated by" with icon and TimeAgo
    // Without this method: project cards show no audit trail — who/when unknown
    public static string ProjectAuditLabel(WorkflowApprovalUI.Models.ProjectResponse p)
    {
        // If project was edited after creation, show last updater line instead of creator-only
        // Without this branch: edits still show "Created by" — misleading audit
        if (p.UpdatedByUserId.HasValue && !string.IsNullOrEmpty(p.UpdatedByName))
        {
            // HTML snippet with edit icon, name, formatted id, and relative update time
            // Without this return: updated projects fall through to creator line incorrectly
            return $"<span class=\"audit-label\">" +
                   $"<i class=\"fa-solid fa-pen-to-square audit-icon\"></i>" +
                   $"Last updated by: <strong>{p.UpdatedByName}</strong> " +
                   $"({FormatUserId(p.UpdatedByUserId.Value)}) &bull; {TimeAgo(p.UpdatedAt)}" +
                   $"</span>";
        }

        // Never edited (or no updater name) — show original creator and creation time
        // Without this return: new projects have no audit line at all
        return $"<span class=\"audit-label\">" +
               $"<i class=\"fa-solid fa-circle-user audit-icon\"></i>" +
               $"Created by: <strong>{p.CreatedByName}</strong> " +
               $"({FormatUserId(p.CreatedByUserId)}) &bull; {TimeAgo(p.CreatedAt)}" +
               $"</span>";
    }

    // ── Task Audit Label ──────────────────────────────────────────────────────
    // Same pattern as project: creator (assigner) vs last status updater on task rows
    // Without this method: task rows lack who created or last changed status
    public static string TaskAuditLabel(WorkflowApprovalUI.Models.TaskResponse t)
    {
        // Status was changed at least once — show updater
        // Without this branch: status changes still show only assigner as "Created by"
        if (t.UpdatedByUserId.HasValue && !string.IsNullOrEmpty(t.UpdatedByName))
        {
            // Edit icon + last updater name, id prefix, and TimeAgo on UpdatedAt
            // Without this return: updated tasks missing audit HTML
            return $"<span class=\"audit-label\">" +
                   $"<i class=\"fa-solid fa-pen-to-square audit-icon\"></i>" +
                   $"Last updated by: <strong>{t.UpdatedByName}</strong> " +
                   $"({FormatUserId(t.UpdatedByUserId.Value)}) &bull; {TimeAgo(t.UpdatedAt)}" +
                   $"</span>";
        }

        // Task never had status update — assigner is treated as creator
        // Without this return: tasks with no updates show blank audit
        return $"<span class=\"audit-label\">" +
               $"<i class=\"fa-solid fa-circle-user audit-icon\"></i>" +
               $"Created by: <strong>{t.AssignedByName}</strong> " +
               $"({FormatUserId(t.AssignedByUserId)}) &bull; {TimeAgo(t.CreatedAt)}" +
               $"</span>";
    }

    // ── Approval Reviewer Label ───────────────────────────────────────────────
    // Reviewer line after approve/reject/request changes — empty while still Pending
    // Without this method: approval cards never show who reviewed or outcome-specific verb
    public static string ApprovalReviewerLabel(WorkflowApprovalUI.Models.ApprovalResponse a)
    {
        // No reviewer yet — pending approvals should not show a reviewer line
        // Without this: pending cards show empty or bogus reviewer text
        if (!a.ReviewedByUserId.HasValue || string.IsNullOrEmpty(a.ReviewedByName))
            return string.Empty;

        // Verb matches outcome: Approved by, Rejected by, Changes requested by, etc.
        // Without this: all outcomes say generic "Reviewed by" — less clear UX
        var verb = a.Status switch
        {
            // Without this case: approved items don't say "Approved by"
            "Approved"         => "Approved by",

            // Without this case: rejected items don't say "Rejected by"
            "Rejected"         => "Rejected by",

            // Without this case: change requests don't say "Changes requested by"
            "ChangesRequested" => "Changes requested by",

            // Without this default: unknown statuses have no label text
            _                  => "Reviewed by"
        };

        // Font Awesome icon class varies by outcome for color-coded visual cue
        // Without this: all reviewer lines use same icon — status less scannable
        var iconClass = a.Status switch
        {
            // Without this case: approved lines miss green check icon
            "Approved"         => "fa-circle-check audit-icon-success",

            // Without this case: rejected lines miss red X icon
            "Rejected"         => "fa-circle-xmark audit-icon-danger",

            // Without this case: change-request lines miss warning rotate icon
            "ChangesRequested" => "fa-rotate-left audit-icon-warning",

            // Without this default: fallback generic user-check icon
            _                  => "fa-user-check audit-icon"
        };

        // Full HTML span for reviewer — views use @Html.Raw on this string
        // Without this return: reviewer name and time never rendered on card
        return $"<span class=\"audit-label\">" +
               $"<i class=\"fa-solid {iconClass}\"></i>" +
               $"{verb}: <strong>{a.ReviewedByName}</strong> " +
               $"({FormatUserId(a.ReviewedByUserId.Value)}) &bull; {TimeAgo(a.ReviewedAt!.Value)}" +
               $"</span>";
    }

    // ── Approval Requester Label ────────────────────────────────────────────────
    // Always shown on every approval card — who submitted the request and when
    // Without this method: approval cards miss the requester half of the audit trail
    public static string ApprovalRequesterLabel(WorkflowApprovalUI.Models.ApprovalResponse a)
    {
        // Requester line with user icon — separate from reviewer so both show on completed approvals
        // Without this return: "Requested by" line missing — incomplete history
        return $"<span class=\"audit-label\">" +
               $"<i class=\"fa-solid fa-circle-user audit-icon\"></i>" +
               $"Requested by: <strong>{a.RequestedByName}</strong> " +
               $"({FormatUserId(a.RequestedByUserId)}) &bull; {TimeAgo(a.RequestedAt)}" +
               $"</span>";
    }
}
