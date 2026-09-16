# Employee Attendance with Health Declaration

A web-based attendance monitoring system built on ASP.NET Core 8 MVC. Employees scan their ID card (or its QR code) at an unattended kiosk to record a **TIME IN** / **TIME OUT**; on TIME IN they complete a short self-administered health declaration and waiver consent within a configurable window, and combinations that mean "not allowed to enter" (UNFIT, or FIT + waiver not understood) automatically close the session and block same-day re-entry. Administrators sign in through LDAP to manage providers, projects, and employees, correct attendance records, tune business rules at runtime, and review a full audit trail.

## Key features

- **Kiosk scanning** — ID card / keyboard-wedge scanner input with duplicate-scan debounce and a scanner-only mode
- **Health & waiver gate** — FIT/UNFIT self-declaration with waiver consent, configurable window, automatic TIME OUT on blocked combinations
- **Employee management** — generated employee IDs (`{ProviderCode}-NNNNNN`), multi-project assignments with per-project positions, merge-on-duplicate re-enrollment, bulk Excel import, QR codes generated at enrollment and embedded in the Excel roster export
- **Provider & project management** — auto-generated provider acronym codes and year-scoped sequential project codes (`{ProviderCode}-YY-NNN`), contract dates with expiry auto-deactivation and soft delete
- **Attendance records** — public history logs with filters and Excel export, an admin dashboard with Attendance and Health Declaration charts, and admin TimeLogs corrections
- **Governance** — runtime system configuration (no redeploy) and a complete audit trail with a changes-only diff view

## Technology

- ASP.NET Core 8 MVC · MySQL (all data access via stored procedures with Dapper) · LDAP authentication with session-based admin access
- Frontend: jQuery, Bootstrap 5, DataTables, SweetAlert2, Select2, ApexCharts, SheetJS/ExcelJS — all vendored under `wwwroot/lib/`, no CDN (offline-friendly)
- Automated tests: xUnit + NSubstitute (`EmployeeAttendanceWithHealthDeclaration.Tests`)

## Getting started

**Prerequisites:** .NET 8 SDK, MySQL with the `employee_attendance_with_health_declaration` database, an LDAP endpoint, and the connection string / LDAP host configured in `appsettings.json`.

```powershell
dotnet restore
dotnet build
dotnet run
```

- Kiosk: `http://localhost:5194/`
- History logs: `/Home/HistoryLogs`
- Admin login: `/Auth/Login`

Run `dotnet test` for the automated suite (unit + real-database integration tests).

## Documentation

The full **System Requirements & QA white paper** — system overview, requirements, functionality, user manual, and test cases — is [`wwwroot/docs/system-requirements-qa.html`](wwwroot/docs/system-requirements-qa.html) in this repository. Once the app is running, it is served at the **`/system-requirement`** URL.
