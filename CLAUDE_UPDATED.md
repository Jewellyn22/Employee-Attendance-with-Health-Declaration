# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with this repository.

## Project Overview

**Contractor Attendance and Health Declaration Monitoring** is an ASP.NET Core MVC web application targeting .NET 8.0. The system monitors contractor attendance through ID scanning, time-in/time-out recording, and health declaration validation with a simplified self-administered FIT/UNFIT workflow.

## Project Status: Production-Ready (Version 2.0.0.0)

**Current State**: The application is fully implemented with all core features functional. 

**Implemented**:
- ✅ Complete ASP.NET Core MVC project structure with 3 controllers
- ✅ Service layer (business logic) with Response<T> pattern
- ✅ Repository layer (Dapper data access)
- ✅ Domain models (snake_case naming convention)
- ✅ LDAP authentication infrastructure
- ✅ Session-based admin authorization
- ✅ Admin interface (Providers, Projects, Contractors, SystemConfig, TimeLogs management)
- ✅ Public kiosk interface (ID scanning + health declaration)
- ✅ Public History Logs page (filtering, sorting, Excel export)
- ✅ Custom authorization attributes
- ✅ SignalR Hub infrastructure (for future real-time features)
- ✅ Frontend libraries (jQuery, Bootstrap 5, DataTables, SweetAlert2, Select2, SheetJS)

**Current Version**: 2.0.0.0 (tracked in `.csproj`)

**Architecture Evolution**: The project was simplified from the original ICOPS pattern to use only 3 controllers instead of 9, eliminated Safety Officers/waiver workflow, and moved to a self-administered health gatekeeping model.

## Development Commands

### Build and Run
```powershell
# Build the project
dotnet build

# DO NOT RUN - User will test manually
# dotnet run  <-- NEVER execute this
```

### Testing
```powershell
# Run unit tests only (Service layer business logic)
dotnet test

# No integration tests - all data access is mocked
```

### Other Commands
```powershell
# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore

# Publish for deployment
dotnet publish -c Release -o ./publish
```

### Version Management
**CRITICAL**: System version follows `X.X.X.X` format (Major.Feature.Function.Bugfix). Update `.csproj` version before EVERY commit.

## Architecture: Layered with Repository/Service Pattern

### Overall Architecture
```
Controllers/ → Services/ → Repositories/ → Database
     ↓              ↓              ↓
   UI          Business Logic    Data Access
```

### Simplified Controller Structure
**3 Controllers Only** (consolidated from original 9-controller design):

1. **HomeController** (Public, no authentication):
   - Main kiosk interface (`/`) - ID scanning + health declaration
   - Public History Logs page (`/Home/HistoryLogs`) - read-only viewing + export
   - AJAX endpoints: `/Home/Scan`, `/Home/UpdateHealthStatus`, `/Home/GetHistoryLogs`

2. **AuthController** (Admin authentication):
   - Admin login (`/Auth/Login`) - LDAP-based
   - Admin logout (`/Auth/Logout`)

3. **AdminController** (Admin authentication required):
   - Consolidated admin operations using `[AuthorizeAdmin]` attribute
   - Provider/Project/Contractor CRUD operations
   - System Configuration management
   - TimeLogs edit/delete with audit trail

### Project Structure
```
Controllers/
  - HomeController.cs (ID scanning, health declaration, history logs)
  - AuthController.cs (admin login/logout)
  - AdminController.cs (consolidated admin operations)

Attributes/
  - AuthorizeAdminAttribute.cs (custom authorization filter)

Services/
  - IAttendanceService.cs / AttendanceService.cs
  - IHistoryLogsService.cs / HistoryLogsService.cs
  - IProviderService.cs / ProviderService.cs
  - IProjectService.cs / ProjectService.cs
  - IContractorService.cs / ContractorService.cs
  - ISystemConfigService.cs / SystemConfigService.cs
  - ITimeLogsManagementService.cs / TimeLogsManagementService.cs

Repositories/
  - ITimeLogsRepository.cs / TimeLogsRepository.cs
  - IProviderRepository.cs / ProviderRepository.cs
  - IProjectRepository.cs / ProjectRepository.cs
  - IContractorEmployeeRepository.cs / ContractorEmployeeRepository.cs
  - ISystemConfigRepository.cs / SystemConfigRepository.cs

Models/
  - Response.cs (generic Response<T> pattern)
  - Domain/ (snake_case models matching DB tables)
  - DTOs/ (if needed for API responses)
  - ViewModels/ (if needed for UI)

ExternalApis/
  - Ldap/ (Services, Repositories, Models - reuses ICOPS pattern)

Hubs/
  - AttendanceHub.cs (SignalR infrastructure for future real-time features)
```

## Critical Coding Standards

### 1. Naming Convention: ALL snake_case
**ALL models use snake_case** - Domain models, DTOs, ViewModels, everything. This matches the database exactly.

```csharp
public class contractor_employee
{
    public string employee_id { get; set; }
    public string name { get; set; }
    public string provider_code { get; set; }
    public int active { get; set; }
}

public class time_log
{
    public int attendance_id { get; set; }
    public string employee_id { get; set; }
    public DateTime time_in { get; set; }
    public DateTime? time_out { get; set; }
    public string health_status { get; set; }  // "FIT" or "UNFIT"
}
```

### 2. Database Access: Dapper + Stored Procedures ONLY
**NO hardcoded SQL queries** - ALL database access must use stored procedures.

```csharp
public class contractor_employeeRepository : Icontractor_employeeRepository
{
    public async Task<contractor_employee> GetByEmployeeId(string employee_id)
    {
        const string storedProc = "sp_contractor_employee_GetByEmployeeId";
        return await _db.QuerySingleOrDefaultAsync<contractor_employee>(
            storedProc,
            new { employee_id },
            commandType: CommandType.StoredProcedure
        );
    }
}
```

**No OUT parameters** - Stored procedures return result sets only.

### 3. JSON Serialization: Global snake_case
Configure ONCE in `Program.cs` - NO attributes needed.

```csharp
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Use snake_case for all JSON property names
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });
```

### 4. Error Handling: Response<T> Pattern + Try-Catch
**All Service methods return `Response<T>`** (matches ICOPS LDAP pattern).

```csharp
public class Response<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}
```

**Manual try-catch in EVERY method** - no global exception middleware.

```csharp
public async Task<Response<contractor_employee>> Create(contractor_employee employee)
{
    try
    {
        var result = await _repository.Create(employee);
        return new Response<contractor_employee> 
        { 
            Success = true, 
            Message = "Contractor created successfully",
            Data = result 
        };
    }
    catch (MySqlException ex)
    {
        _logger.LogError(ex, "Database error creating contractor: {EmployeeId}", employee.employee_id);
        return new Response<contractor_employee> 
        { 
            Success = false, 
            Message = "Database error occurred. Please contact support.",
            Data = null 
        };
    }
}
```

### 5. Validation: Manual in Service Layer
**NO data annotations, NO external libraries** - all validation is manual in Service layer.

```csharp
public async Task<Response<time_log>> ProcessScan(string employee_id)
{
    // Step 1: Validate contractor exists and is active
    var employee = await _contractorRepository.GetByEmployeeId(employee_id);
    if (employee == null)
    {
        return new Response<time_log> 
        { 
            Success = false, 
            Message = "Contractor not found",
            Data = null 
        };
    }

    if (employee.active == 0)
    {
        return new Response<time_log> 
        { 
            Success = false, 
            Message = "Contractor is inactive",
            Data = null 
        };
    }

    // Continue with business logic...
}
```

### 6. Controllers: Hybrid API-Ready
Controllers return **both Views (Razor pages) AND JSON responses**.

```csharp
// HomeController
[HttpGet]
public IActionResult Index() → Returns Razor View (kiosk interface)

[HttpPost]
public async Task<IActionResult> Scan(string employee_id) → Returns JSON (API response)

[HttpGet]
public IActionResult HistoryLogs() → Returns Razor View (public history page)

[HttpGet]
public async Task<IActionResult> GetHistoryLogs(...) → Returns JSON (for DataTable)
```

### 7. Authorization: Custom Attribute + Session
Admin interface uses **custom `[AuthorizeAdmin]` attribute** with session-based authorization.

```csharp
[AuthorizeAdmin]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        ViewBag.AdminName = HttpContext.Session.GetString("DisplayName");
        return View();
    }
}
```

**Kiosk interface**: NO authentication required (public access).

### 8. Authentication: LDAP + Session
Admin users authenticate via **LDAP API** + **Session storage**.

```csharp
// AuthController - Login flow
var ldap_user = await _ldapService.Login(username, password);
if (!ldap_user.Success) { /* deny */ }

// Check admin group membership
string admin_group = await _system_config.GetAdminADGroup();
if (!ldap_user.Data.member_of.Contains(admin_group)) { /* deny */ }

// Set session
HttpContext.Session.SetString("EmployeeNumber", ldap_user.Data.office);
HttpContext.Session.SetString("DisplayName", ldap_user.Data.displayName);
HttpContext.Session.SetString("Role", "Admin");
```

### 9. Configuration: Hybrid Static + Runtime
**Static configuration** in `appsettings.json` (infrastructure):
- Connection strings
- LDAP API URL: `http://127.0.0.1:9008`

**Runtime configuration** in `system_config` table (business rules):
- `DebounceThresholdSeconds`: 30 (duplicate scan prevention)
- `HealthDeclarationWindowSeconds`: 30 (health declaration form visibility)
- `AdminADGroup`: `app.your_app.admin` (LDAP group name)

### 10. Frontend Stack
**Libraries in `wwwroot/lib/`**: jQuery, Bootstrap 5, SignalR, DataTables, SweetAlert2, Select2, SheetJS (xlsx).

**Usage Patterns**:
- DataTables for sortable/filterable tables (History Logs, Admin pages)
- SweetAlert2 for beautiful alerts and confirmations
- Select2 for enhanced dropdowns in admin forms
- SheetJS for client-side Excel export
- SignalR infrastructure available (not currently used for real-time updates)

## Domain Understanding

**Refer to `CONTEXT.md`** for complete domain glossary, business rules, and data structures. Key concepts:

- **Provider**: Company that hires contractors
- **Project**: Work assignment under a Provider
- **Contractor**: Worker assigned to Provider + Project
- **Attendance Cycle**: Determined dynamically by time-in/time-out scans (no pre-configured schedules)
- **Health Status**: FIT (default) or UNFIT (self-administered via radio buttons)
- **2-Minute Rules**: 
  - Duplicate scan debounce (configurable via `DebounceThresholdSeconds`)
  - Health declaration window (configurable via `HealthDeclarationWindowSeconds`)
- **History Logs**: Publicly accessible, read-only view with export (no authentication)
- **Admin Correction**: Authenticated admins can edit/delete timelogs with audit trail

## Simplified Business Workflow

**Single Scan Workflow** (no time-out scanning):
1. Contractor scans ID → System creates TIME IN record with FIT status
2. Health declaration form appears (FIT pre-selected, 2-minute window)
3. If contractor selects UNFIT → System sets TIME OUT + blocks entry
4. Contractor can toggle FIT↔UNFIT within 2-minute window
5. After 2 minutes OR next contractor scan → Form disappears, status locked
6. Only System Admin can correct locked statuses

**No Safety Officers**: Eliminated waiver approval workflow entirely. Health gatekeeping is self-administered.

## Database

### Tables
- `providers` - Provider companies (soft delete with active field)
- `project` - Projects under providers (soft delete with active field)
- `contractor_employee` - Contractors with snake_case columns (soft delete with active field)
- `time_logs` - Attendance records (health_status, updated_by, updated_at for audit trail)
- `system_config` - Runtime configuration (key-value pairs)
- `health_declaration` - (Table exists but not actively used in simplified workflow)

### Database Rule
**NEVER execute SQL directly** - always create stored procedures for user to run manually. Place SQL scripts in `.claude/sql-scripts/` for user execution.

## Development Workflow Rules

**CRITICAL**: Follow these rules from `.claude/memory/development-workflow-rules.md`:

1. **No Auto-Run**: Do NOT run `dotnet run` - user will manually test all changes
2. **Version Management**: System version in `.csproj` as `X.X.X.X` (Major.Feature.Function.Bugfix)
3. **Every Commit Requires Version Increment**: Update `.csproj` before every commit
4. **Git Commit Process**: Plan commit message → Ask approval → Wait for confirmation
5. **Database Rule**: Never execute SQL directly. Create scripts in `.claude/sql-scripts/` for manual execution
6. **File Organization**: Non-source files go under `.claude/` folder

## Testing

**Unit tests only** - Test Service layer business logic with mocked repositories. NO integration tests.

**Manual UI Testing Areas**:
- Kiosk interface (scan processing, health declaration form, 2-minute windows)
- History Logs (filtering, sorting, export)
- Admin interface (CRUD operations, authentication, authorization)
- Admin corrections (edit/delete timelogs with audit trail)

## Development URLs
- HTTP: `http://localhost:5194`
- HTTPS: `https://localhost:7090` 
- See `Properties/launchSettings.json` for configuration

## Key Architectural Decisions (Simplified from ICOPS)

1. **3 Controllers Only** - Consolidated from 9 controllers for simpler maintenance
2. **Single Scan Workflow** - No time-out scanning; contractors scan once per attendance cycle
3. **Self-Administered Health** - Removed Safety Officers and waiver approval workflow
4. **Public History Access** - No authentication required for viewing attendance history
5. **2-Minute Windows** - Unified approach for duplicate debounce + health declaration window
6. **Admin Audit Trail** - TimeLogs include `updated_by` and `updated_at` for accountability
7. **Client-Side Export** - Excel export using SheetJS in browser (no server load)
8. **Session-Based Auth** - Simple session storage for admin authentication (no complex auth framework)
