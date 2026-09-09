using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using Dapper;
using System.Data;
using System.Text.Json;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public class ContractorEmployeeRepository : IContractorEmployeeRepository
    {
        private readonly IDbConnection _db;

        // snake_case keys so the serialized assignments match the $.project_code /
        // $.position paths of the JSON_TABLE split inside the stored procedures
        // (mirrors the global naming policy in Program.cs).
        private static readonly JsonSerializerOptions AssignmentJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public ContractorEmployeeRepository(IDbConnection db)
        {
            _db = db;
        }

        // Read entities come back with project_positions (JSON object {"code":"position"})
        // and assignments = null. Re-saving such an entity (the project deactivation /
        // reactivation cascades call Update directly with a DB-loaded row) must preserve
        // the mappings, so materialize assignments from the JSON map before serializing
        // p_projects.
        private static void EnsureAssignmentsHydrated(contractor_employee employee)
        {
            if ((employee.assignments == null || employee.assignments.Count == 0)
                && !string.IsNullOrWhiteSpace(employee.project_positions))
            {
                try
                {
                    var map = JsonSerializer.Deserialize<Dictionary<string, string>>(
                        employee.project_positions, AssignmentJsonOptions);
                    employee.assignments = map?
                        .Select(kv => new contractor_project_assignment
                        {
                            project_code = kv.Key,
                            position = kv.Value ?? string.Empty
                        })
                        .ToList() ?? new List<contractor_project_assignment>();
                }
                catch (JsonException)
                {
                    // Unparseable snapshot: fall through with an empty list so the
                    // SP's INNER JOIN guard (not the serialization) decides the outcome.
                    employee.assignments = new List<contractor_project_assignment>();
                }
            }
        }

        public async Task<IEnumerable<contractor_employee>> GetAll()
        {
            const string storedProc = "sp_contractor_employee_GetAll";
            return await _db.QueryAsync<contractor_employee>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }

        // Returns the contractor with joined provider/project names and no active filter
        // in the SP. The scan flow (ProcessScan) and admin Update both rely on this single
        // lookup; the active/inactive distinction is enforced in C#.
        public async Task<contractor_employee?> GetByEmployeeId(string employee_id)
        {
            const string storedProc = "sp_contractor_employee_GetByEmployeeId";
            return await _db.QuerySingleOrDefaultAsync<contractor_employee>(
                storedProc,
                new { p_employee_id = employee_id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<contractor_employee?> Create(contractor_employee employee)
        {
            EnsureAssignmentsHydrated(employee);
            const string storedProc = "sp_contractor_employee_Create";
            return await _db.QuerySingleOrDefaultAsync<contractor_employee>(
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
                        employee.assignments ?? new List<contractor_project_assignment>(), AssignmentJsonOptions),
                    p_active = 1
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<contractor_employee?> Update(contractor_employee employee)
        {
            EnsureAssignmentsHydrated(employee);
            const string storedProc = "sp_contractor_employee_Update";
            return await _db.QuerySingleOrDefaultAsync<contractor_employee>(
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
                        employee.assignments ?? new List<contractor_project_assignment>(), AssignmentJsonOptions),
                    p_active = employee.active
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> SetInactive(string employee_id)
        {
            const string storedProc = "sp_contractor_employee_SetInactive";
            var result = await _db.ExecuteAsync(
                storedProc,
                new { p_employee_id = employee_id },
                commandType: CommandType.StoredProcedure
            );
            return result > 0;
        }

        // Soft delete via sp_contractor_employee_Delete: marks the given employee_ids
        // is_deleted = 1 (single atomic UPDATE, no rows removed, time_logs untouched).
        // Returns the SP result set (success + deleted_count).
        public async Task<contractor_delete_result?> Delete(IEnumerable<string> employee_ids)
        {
            const string storedProc = "sp_contractor_employee_Delete";
            return await _db.QuerySingleOrDefaultAsync<contractor_delete_result>(
                storedProc,
                new { p_employee_ids = string.Join(",", employee_ids) },
                commandType: CommandType.StoredProcedure
            );
        }

        // Active contractors for the admin "View Contractors by Project" modal.
        // Stored procedure enforces the active = 1 filter (no inline SQL).
        public async Task<IEnumerable<contractor_employee>> GetByProjectCode(string project_code)
        {
            const string storedProc = "sp_contractor_employee_GetByProjectCode";
            return await _db.QueryAsync<contractor_employee>(
                storedProc,
                new { p_project_code = project_code },
                commandType: CommandType.StoredProcedure
            );
        }

        // Duplicate-enrollment guard used by ContractorService.Create(). The stored
        // procedure matches name case-insensitively (LOWER(TRIM)) + birthdate within the
        // same provider, including inactive contractors, and returns at most one row.
        public async Task<bool> ExistsByDetails(string provider_code, string name, DateTime birthdate)
        {
            const string storedProc = "sp_contractor_employee_CheckDuplicate";
            var match = await _db.QuerySingleOrDefaultAsync<contractor_employee>(
                storedProc,
                new { p_provider_code = provider_code, p_name = name, p_birthdate = birthdate },
                commandType: CommandType.StoredProcedure
            );
            return match != null;
        }

        public async Task<int> GetActiveCount()
        {
            const string storedProc = "sp_contractor_employee_GetActiveCount";
            return await _db.QuerySingleAsync<int>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}