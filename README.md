# WorkflowApprovalApi

A beginner-friendly **ASP.NET Core Web API** backend for a mini Dragonfly/Mediabox-style **workflow approval system**.

## What This Project Does

This API lets teams manage creative/workflow projects from draft to approval:

1. Users register and log in with JWT tokens.
2. Managers create projects and update project status.
3. Tasks are assigned to designers/reviewers.
4. Files are uploaded with automatic version numbers.
5. Team members leave comments on projects.
6. Reviewers/clients approve, reject, or request changes.

**Architecture (V1):** `Controller → Service → DbContext → MySQL`

No repository pattern, MediatR, CQRS, AutoMapper, or ASP.NET Identity in V1.

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Language | C# |
| Framework | ASP.NET Core Web API (.NET 8) |
| ORM | Entity Framework Core |
| Database | MySQL (Pomelo.EntityFrameworkCore.MySql) |
| Auth | JWT Bearer + custom User table + BCrypt password hashing |
| API Docs | Swagger / Swashbuckle |
| File Storage | `wwwroot/uploads` |

---

## Modules

| # | Module | Purpose |
|---|--------|----------|
| 1 | Auth | Register and login |
| 2 | UserRole | Admin, Manager, Designer, Reviewer, Client |
| 3 | Project | Create and track project lifecycle |
| 4 | Task | Assign and update workflow tasks |
| 5 | Approval | Request and review approvals |
| 6 | Comment | Project discussion |
| 7 | FileUpload | Upload files with `VersionNumber` |

---

## How to Run

### 1. Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL Server (local or remote)
- EF Core CLI tools

Install EF tools once:

```bash
dotnet tool install --global dotnet-ef
```

### 2. Configure Connection String

Edit `appsettings.json` and replace `YOUR_PASSWORD` with your MySQL root password:

```json
"ConnectionStrings": {
  "DefaultConnection": "server=localhost;port=3306;database=workflow_approval_db;user=root;password=YOUR_PASSWORD;"
}
```

### 3. Create Database

```sql
CREATE DATABASE workflow_approval_db;
```

### 4. Restore, Migrate, Run

```bash
cd WorkflowApprovalApi
dotnet restore
dotnet ef database update
dotnet run
```

### 5. Open Swagger

Browser: `http://localhost:5000/swagger`

---

## API Endpoints

### Auth
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token

### Projects
- `POST /api/projects` - Create project
- `GET /api/projects` - List all projects
- `GET /api/projects/{id}` - Get project details
- `PUT /api/projects/{id}/status` - Update project status (Admin/Manager only)

### Tasks
- `POST /api/tasks` - Create task (Admin/Manager)
- `GET /api/projects/{projectId}/tasks` - Get tasks for project
- `PUT /api/tasks/{id}/status` - Update task status

### Files
- `POST /api/projects/{projectId}/files/upload` - Upload file
- `GET /api/projects/{projectId}/files` - List files
- `GET /api/files/{fileId}/download` - Download file

### Comments
- `POST /api/comments` - Add comment
- `GET /api/projects/{projectId}/comments` - Get comments

### Approvals
- `POST /api/approvals` - Request approval
- `GET /api/projects/{projectId}/approvals` - Get approvals
- `PUT /api/approvals/{id}/approve` - Approve
- `PUT /api/approvals/{id}/reject` - Reject
- `PUT /api/approvals/{id}/changes-requested` - Request changes

---

## Workflow Testing in Swagger

1. **Register** at least 3 users: Manager, Designer, Reviewer
2. **Login** as Manager and copy JWT token
3. **Authorize** in Swagger: `Bearer {token}`
4. **Create project** via `POST /api/projects`
5. **Create task** and assign to Designer
6. **Upload file** to project
7. **Add comment** on project
8. **Request approval** for file
9. **Login** as Reviewer and approve/reject
10. **Update project status** to Approved

---

## Architecture Pattern

```
HTTP Request
    ↓
Controller   → receives request, checks JWT/roles, calls service
    ↓
Service      → business logic, validation, mapping to DTOs
    ↓
DbContext    → EF Core talks to database tables
    ↓
MySQL        → stores Users, Projects, Tasks, Files, Comments, Approvals
```

---

## Future Improvements (V2)

See `V2_PLAN.md` for planned features like notifications, audit logs, workflow stages, and more.
