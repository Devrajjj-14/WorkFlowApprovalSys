namespace WorkflowApprovalUI.Helpers;

/// <summary>
/// Centralised helper for generating human-readable audit trail labels.
///
/// WHY this exists:
///   Every card in the UI (Project, Task, Approval) needs to show who did what and when.
///   Putting this logic in one static class means:
///     - Views stay clean (no C# date maths inside .cshtml)
///     - Changing the wording ("mins ago" → "minutes ago") only requires editing one file
///     - The same logic is reused across all three entity types
///
/// HOW it is used:
///   In any .cshtml file, call:
///     @AuditHelper.ProjectAuditLabel(project)
///     @AuditHelper.TaskAuditLabel(task)
///     @AuditHelper.ApprovalAuditLabel(approval)
///   Each method returns a ready-to-render HTML string.
/// </summary>
public static class AuditHelper
{
    // ── Time-Ago Formatter ────────────────────────────────────────────────────
    /// <summary>
    /// Converts a UTC DateTime to a short, human-readable relative string.
    ///
    /// WHY relative timestamps instead of absolute:
    ///   "5 mins ago" is far more scannable for users than "2026-06-12 10:32:44".
    ///   Absolute timestamps are already shown elsewhere (e.g., Approval requested date).
    ///
    /// EDGE CASES handled:
    ///   - DateTime.MinValue (default/uninitialised) → returns "unknown"
    ///   - Clocks slightly out of sync can produce negative elapsed; treated as "Just now"
    ///   - Anything over 365 days shows the full date to keep the label meaningful
    /// </summary>
    public static string TimeAgo(DateTime utcTime)
    {
        // Guard: if no real value was provided, return a safe default
        if (utcTime == DateTime.MinValue) return "unknown";

        var elapsed = DateTime.UtcNow - utcTime;

        // Negative elapsed can occur when server clocks are slightly out of sync
        if (elapsed.TotalSeconds < 30)  return "Just now";
        if (elapsed.TotalMinutes < 2)   return "1 min ago";
        if (elapsed.TotalMinutes < 60)  return $"{(int)elapsed.TotalMinutes} mins ago";
        if (elapsed.TotalHours < 2)     return "1 hr ago";
        if (elapsed.TotalHours < 24)    return $"{(int)elapsed.TotalHours} hrs ago";
        if (elapsed.TotalDays < 2)      return "1 day ago";
        if (elapsed.TotalDays < 365)    return $"{(int)elapsed.TotalDays} days ago";

        // For old records show actual date — "365 days ago" is hard to understand
        return utcTime.ToString("MMM d, yyyy");
    }

    // ── User ID Prefix Formatter ──────────────────────────────────────────────
    /// <summary>
    /// Formats a raw integer User ID into a labelled prefix string.
    ///
    /// WHY the prefix matters:
    ///   The spec requires IDs to be displayed as "EMP-1024" or "ADM-001".
    ///   We cannot determine role from the ID alone (we only have the name in the DTO),
    ///   so we use a simple numeric-range heuristic: ID 1 = first user = likely admin.
    ///   ID ≤ 10 gets "ADM" prefix; everything else gets "EMP".
    ///
    ///   In a real system you would pass the role string from the response instead.
    ///   This helper is intentionally easy to change — update the prefix rules here only.
    /// </summary>
    private static string FormatUserId(int userId)
    {
        // Users with very low IDs are likely admin/seed accounts
        var prefix = userId <= 10 ? "ADM" : "EMP";
        // Zero-pad to at least 3 digits: 5 → "005", 1024 → "1024"
        return $"{prefix}-{userId:D3}";
    }

    // ── Project Audit Label ───────────────────────────────────────────────────
    /// <summary>
    /// Returns the correct audit line for a Project card.
    ///
    /// BUSINESS RULE:
    ///   If UpdatedByUserId is NULL  → project was never edited → show "Created by"
    ///   If UpdatedByUserId has value → project was edited      → show "Last updated by"
    ///
    /// The original creation data is NOT replaced in the database — we always keep
    /// CreatedByUserId/Name and simply show the more recent action to the user.
    ///
    /// Returns an HTML string so the Razor view can render it with @Html.Raw().
    /// Icon classes use FontAwesome 6 (already loaded in the layout).
    /// </summary>
    public static string ProjectAuditLabel(WorkflowApprovalUI.Models.ProjectResponse p)
    {
        if (p.UpdatedByUserId.HasValue && !string.IsNullOrEmpty(p.UpdatedByName))
        {
            // Project was edited at least once — show who last touched it
            return $"<span class=\"audit-label\">" +
                   $"<i class=\"fa-solid fa-pen-to-square audit-icon\"></i>" +
                   $"Last updated by: <strong>{p.UpdatedByName}</strong> " +
                   $"({FormatUserId(p.UpdatedByUserId.Value)}) &bull; {TimeAgo(p.UpdatedAt)}" +
                   $"</span>";
        }

        // Project is brand-new or was never edited — show creator
        return $"<span class=\"audit-label\">" +
               $"<i class=\"fa-solid fa-circle-user audit-icon\"></i>" +
               $"Created by: <strong>{p.CreatedByName}</strong> " +
               $"({FormatUserId(p.CreatedByUserId)}) &bull; {TimeAgo(p.CreatedAt)}" +
               $"</span>";
    }

    // ── Task Audit Label ──────────────────────────────────────────────────────
    /// <summary>
    /// Returns the correct audit line for a Task row.
    ///
    /// BUSINESS RULE:
    ///   If UpdatedByUserId is NULL  → status was never changed → show "Created by" (AssignedByUser)
    ///   If UpdatedByUserId has value → status was changed      → show "Last updated by"
    ///
    /// Note: "Created by" uses AssignedByName, not a separate CreatedByName field,
    /// because the person who assigned the task IS the creator.
    /// </summary>
    public static string TaskAuditLabel(WorkflowApprovalUI.Models.TaskResponse t)
    {
        if (t.UpdatedByUserId.HasValue && !string.IsNullOrEmpty(t.UpdatedByName))
        {
            return $"<span class=\"audit-label\">" +
                   $"<i class=\"fa-solid fa-pen-to-square audit-icon\"></i>" +
                   $"Last updated by: <strong>{t.UpdatedByName}</strong> " +
                   $"({FormatUserId(t.UpdatedByUserId.Value)}) &bull; {TimeAgo(t.UpdatedAt)}" +
                   $"</span>";
        }

        return $"<span class=\"audit-label\">" +
               $"<i class=\"fa-solid fa-circle-user audit-icon\"></i>" +
               $"Created by: <strong>{t.AssignedByName}</strong> " +
               $"({FormatUserId(t.AssignedByUserId)}) &bull; {TimeAgo(t.CreatedAt)}" +
               $"</span>";
    }

    // ── Approval Audit Label ──────────────────────────────────────────────────
    /// <summary>
    /// Returns the reviewer audit line for an Approval card.
    ///
    /// BUSINESS RULE:
    ///   If ReviewedByUserId is NULL → approval is still Pending → show nothing (return "")
    ///   Otherwise → show label matching the status:
    ///     Approved          → "Approved by"
    ///     Rejected          → "Rejected by"
    ///     ChangesRequested  → "Changes requested by"
    ///     (anything else)   → "Reviewed by"
    ///
    /// The requester label ("Requested by") is rendered separately in the view
    /// so it always shows regardless of status.
    /// </summary>
    public static string ApprovalReviewerLabel(WorkflowApprovalUI.Models.ApprovalResponse a)
    {
        // Still pending — no reviewer yet, render nothing
        if (!a.ReviewedByUserId.HasValue || string.IsNullOrEmpty(a.ReviewedByName))
            return string.Empty;

        // Pick the verb that matches the outcome
        var verb = a.Status switch
        {
            "Approved"         => "Approved by",
            "Rejected"         => "Rejected by",
            "ChangesRequested" => "Changes requested by",
            _                  => "Reviewed by"
        };

        // Icon colour class changes per outcome to reinforce the status visually
        var iconClass = a.Status switch
        {
            "Approved"         => "fa-circle-check audit-icon-success",
            "Rejected"         => "fa-circle-xmark audit-icon-danger",
            "ChangesRequested" => "fa-rotate-left audit-icon-warning",
            _                  => "fa-user-check audit-icon"
        };

        return $"<span class=\"audit-label\">" +
               $"<i class=\"fa-solid {iconClass}\"></i>" +
               $"{verb}: <strong>{a.ReviewedByName}</strong> " +
               $"({FormatUserId(a.ReviewedByUserId.Value)}) &bull; {TimeAgo(a.ReviewedAt!.Value)}" +
               $"</span>";
    }

    /// <summary>
    /// Always-visible requester line shown on every approval card regardless of status.
    /// Displayed above the reviewer label so users can see the full audit trail.
    /// </summary>
    public static string ApprovalRequesterLabel(WorkflowApprovalUI.Models.ApprovalResponse a)
    {
        return $"<span class=\"audit-label\">" +
               $"<i class=\"fa-solid fa-circle-user audit-icon\"></i>" +
               $"Requested by: <strong>{a.RequestedByName}</strong> " +
               $"({FormatUserId(a.RequestedByUserId)}) &bull; {TimeAgo(a.RequestedAt)}" +
               $"</span>";
    }
}
