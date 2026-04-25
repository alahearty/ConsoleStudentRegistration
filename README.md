# Student Registration System (Console, PostgreSQL, database-first)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL (local or remote)

## Database setup

1. Create a database (name should match your connection string, default `studentregistrationdb`).

2. Run the full script (new installs):

   ```bash
   psql -U postgres -h localhost -d studentregistrationdb -f DatabaseSetup.sql
   ```

3. **If you already created tables with old column names** (`DeptId` instead of `DepartmentId`), run:

   ```bash
   psql -U postgres -h localhost -d studentregistrationdb -f DatabaseMigrate_DeptIdToDepartmentId.sql
   ```

4. **If the database exists but `AuditLogs` is missing**, run:

   ```bash
   psql -U postgres -h localhost -d studentregistrationdb -f Database_AddAuditLogsOnly.sql
   ```

## Configuration

- Copy or edit `appsettings.json` next to the built executable (it is copied to the output folder on build).
- Override the connection string with environment variable:

  `ConnectionStrings__DefaultConnection=Host=...;...`

- **Enrollment:RequireSameDepartment** — when `true`, students may only enroll in courses offered by their department (default `true`).

- **Ui:DefaultPageSize** — page size for long lists (5–200, default 15).

- **Export:Directory** — subfolder under the app base path for CSV exports (default `exports`).

## Run

```bash
dotnet run --project ConsoleStudentRegistration.csproj
```

## Tests

```bash
dotnet test ConsoleStudentRegistration.sln
```

## Re-scaffold models (after schema changes)

```bash
dotnet ef dbcontext scaffold "Host=localhost;..." Npgsql.EntityFrameworkCore.PostgreSQL --output-dir Models --context-dir Data --context AppDbContext --force --no-onconfiguring
```

Then merge back manual files: `AppDbContextFactory.cs`, `AppDbContext.Extensions.cs`, `Models/AuditLog.cs`, and partial extensions under `Models/`.

## Features implemented

- Schools, departments, students, courses, enrollments, results, reports.
- Same-department enrollment policy (configurable).
- Automatic **Completed** enrollment when a result is recorded.
- **Audit log** for key actions; **CSV export**; **paged** long lists.
- **Input validation** (email, phone, date of birth).
- **Optimistic concurrency** on `Student` and `Course` (PostgreSQL `xmin`).
- Structured **logging** to the console (Microsoft.Extensions.Logging).
