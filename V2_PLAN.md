# V2 Plan

Future improvements for **WorkflowApprovalApi** (not included in V1).

---

## Planned Features

### 1. Notification
- Email or in-app notifications when:
  - Task is assigned
  - File is uploaded
  - Approval is requested
  - Approval status changes

### 2. AuditLog / Activity History
- Track who changed what and when
- Store old/new values for project status, task status, approvals
- Timeline API per project

### 3. WorkflowStep / Stage
- Configurable workflow stages per project type
- Enforce stage order (e.g. Draft → Design → Review → Approved)
- Stage-based permissions

### 4. Repository Pattern
- Introduce `IRepository<T>` layer between Service and DbContext
- Improve testability and separation of concerns

### 5. Advanced Permissions
- Fine-grained permissions beyond simple roles
- Project-level membership (owner, contributor, viewer)
- Policy-based authorization per action

### 6. Dashboard APIs
- Project counts by status
- Pending approvals summary
- Overdue tasks report
- User workload overview

---

V1 intentionally stays simple for learning and company demos.
