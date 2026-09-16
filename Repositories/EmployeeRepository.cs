using EmployeeAttendanceWithHealthDeclaration.Models;
using EmployeeAttendanceWithHealthDeclaration.Models.Domain;
using Dapper;
using System.Data;
using System.Text.Json;

namespace EmployeeAttendanceWithHealthDeclaration.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly IDbConnection _db;

        // snake_case keys so the serialized assignments match the $.project_code /
        // $.position paths of the JSON_TABLE split inside the stored procedures
        // (mirrors the global naming policy in Program.cs).
        private static readonly JsonSerializerOptions AssignmentJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public EmployeeRepository(IDbConnection db)
        {
            _db = db;
        }

        // Read entities come back with project_positions (JSON object {"code":"position"})
        // and assignments = null. Re-saving such an entity (the project deactivation /
        // reactivation cascades call Update directly with a DB-loaded row) must preserve
        // the mappings, so materialize assignments from the JSON map before serializing
        // p_projects.
        private static void EnsureAssignmentsHydrated(employee employee)
        {
            if ((employee.assignments == null || employee.assignments.Count == 0)
                && !string.IsNullOrWhiteSpace(employee.project_positions))
            {
                try
                {
                    var map = JsonSerializer.Deserialize<Dictionary<string, string>>(
                        employee.project_positions, AssignmentJsonOptions);
                    employee.assignments = map?
                        .Select(kv => new employee_project_assignment
                        {
                            project_code = kv.Key,
                            position = kv.Value ?? string.Empty
                        })
                        .ToList() ?? new List<employee_project_assignment>();
                }
                catch (JsonException)
                {
                    // Unparseable snapshot: fall through with an empty list so the
                    // SP's INNER JOIN guard (not the serialization) decides the outcome.
                    employee.assignments = new List<employee_project_assignment>();
                }
            }
        }

        public async Task<IEnumerable<employee>> GetAll()
        {
            const string storedProc = "sp_employee_GetAll";
            return await _db.QueryAsync<employee>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }

        // Returns the employee with joined provider/project names and no active filter
        // in the SP. The scan flow (ProcessScan) and admin Update both rely on this single
        // lookup; the active/inactive distinction is enforced in C#.
        public async Task<employee?> GetByEmployeeId(string employee_id)
        {
            const string storedProc = "sp_employee_GetByEmployeeId";
            return await _db.QuerySingleOrDefaultAsync<employee>(
                storedProc,
                new { p_employee_id = employee_id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<employee?> Create(employee employee)
        {
            EnsureAssignmentsHydrated(employee);
            const string storedProc = "sp_employee_Create";
            return await _db.QuerySingleOrDefaultAsync<employee>(
                storedProc,
                new
                {
                    p_name = employee.name,
                    p_gender = employee.gender,
                    p_birthdate = employee.birthdate,
                    p_contact_number = employee.contact_number,
                    p_address = employee.address,
                    p_provider_code = employee.provider_code,
                    p_projects = JsonSerializer.Serialize(
                        employee.assignments ?? new List<employee_project_assignment>(), AssignmentJsonOptions),
                    p_active = 1
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<employee?> Update(employee employee)
        {
            EnsureAssignmentsHydrated(employee);
            const string storedProc = "sp_employee_Update";
            return await _db.QuerySingleOrDefaultAsync<employee>(
                storedProc,
                new
                {
                    p_employee_id = employee.employee_id,
                    p_name = employee.name,
                    p_gender = employee.gender,
                    p_birthdate = employee.birthdate,
                    p_contact_number = employee.contact_number,
                    p_address = employee.address,
                    p_provider_code = employee.provider_code,
                    p_projects = JsonSerializer.Serialize(
                        employee.assignments ?? new List<employee_project_assignment>(), AssignmentJsonOptions),
                    p_active = employee.active
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> SetInactive(string employee_id)
        {
            const string storedProc = "sp_employee_SetInactive";
            var result = await _db.ExecuteAsync(
                storedProc,
                new { p_employee_id = employee_id },
                commandType: CommandType.StoredProcedure
            );
            return result > 0;
        }

        // Stores the QR PNG (content = the immutable employee_id) generated by
        // QrCodeHelper at enrollment. sp_employee_SetQrCode touches ONLY
        // qr_code_image (update_at stays put — derived data, not a record edit).
        public async Task<bool> SetQrCode(string employee_id, byte[] qr_image)
        {
            const string storedProc = "sp_employee_SetQrCode";
            var result = await _db.ExecuteAsync(
                storedProc,
                new { p_employee_id = employee_id, p_qr_image = qr_image },
                commandType: CommandType.StoredProcedure
            );
            return result > 0;
        }

        // Export-only read (sp_employee_GetQrCodes): returns the stored
        // QR blob per enrolled employee. Dapper maps it straight into the
        // employee_qr_code DTO; the domain model deliberately has no blob property
        // so audit snapshots and the grid payload never carry base64.
        public async Task<IEnumerable<employee_qr_code>> GetQrCodes()
        {
            const string storedProc = "sp_employee_GetQrCodes";
            return await _db.QueryAsync<employee_qr_code>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }

        // Soft delete via sp_employee_Delete: marks the given employee_ids
        // is_deleted = 1 (single atomic UPDATE, no rows removed, time_logs untouched).
        // Returns the SP result set (success + deleted_count).
        public async Task<employee_delete_result?> Delete(IEnumerable<string> employee_ids)
        {
            const string storedProc = "sp_employee_Delete";
            return await _db.QuerySingleOrDefaultAsync<employee_delete_result>(
                storedProc,
                new { p_employee_ids = string.Join(",", employee_ids) },
                commandType: CommandType.StoredProcedure
            );
        }

        // Active employees for the admin "View Employees by Project" modal.
        // Stored procedure enforces the active = 1 filter (no inline SQL).
        public async Task<IEnumerable<employee>> GetByProjectCode(string project_code)
        {
            const string storedProc = "sp_employee_GetByProjectCode";
            return await _db.QueryAsync<employee>(
                storedProc,
                new { p_project_code = project_code },
                commandType: CommandType.StoredProcedure
            );
        }

        // Duplicate-enrollment lookup used by EmployeeService.Create(). The stored
        // procedure matches name case-insensitively (LOWER(TRIM)) + birthdate within the
        // same provider, including inactive employees, and returns at most one row
        // (employee_id, name, birthdate, active) — a partial entity; the service fetches
        // the full row via GetByEmployeeId to drive the merge.
        public async Task<employee?> FindDuplicate(string provider_code, string name, DateTime birthdate)
        {
            const string storedProc = "sp_employee_CheckDuplicate";
            return await _db.QuerySingleOrDefaultAsync<employee>(
                storedProc,
                new { p_provider_code = provider_code, p_name = name, p_birthdate = birthdate },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> GetActiveCount()
        {
            const string storedProc = "sp_employee_GetActiveCount";
            return await _db.QuerySingleAsync<int>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}