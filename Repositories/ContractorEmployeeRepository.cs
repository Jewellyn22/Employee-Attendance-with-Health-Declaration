using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using Dapper;
using System.Data;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public class ContractorEmployeeRepository : IContractorEmployeeRepository
    {
        private readonly IDbConnection _db;

        public ContractorEmployeeRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<contractor_employee>> GetAll()
        {
            const string storedProc = "sp_contractor_employee_GetAll";
            return await _db.QueryAsync<contractor_employee>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<contractor_employee?> GetByEmployeeId(string employee_id)
        {
            const string storedProc = "sp_contractor_employee_GetByEmployeeId";
            return await _db.QuerySingleOrDefaultAsync<contractor_employee>(
                storedProc,
                new { p_employee_id = employee_id },
                commandType: CommandType.StoredProcedure
            );
        }

        // Finds a contractor regardless of active status (no active filter in the SP).
        // Used ONLY by ContractorService.Update() existence check so inactive contractors
        // can be edited/reactivated. Scan flow keeps using GetByEmployeeId (active-only).
        public async Task<contractor_employee?> GetByEmployeeIdForUpdate(string employee_id)
        {
            const string storedProc = "sp_contractor_employee_GetByEmployeeIdForUpdate";
            return await _db.QuerySingleOrDefaultAsync<contractor_employee>(
                storedProc,
                new { p_employee_id = employee_id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<contractor_employee?> Create(contractor_employee employee)
        {
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
                    p_area_of_destination = employee.area_of_destination,
                    p_project_code = employee.project_code,
                    p_provider_code = employee.provider_code,
                    p_position = employee.position,
                    p_active = 1
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<contractor_employee?> Update(contractor_employee employee)
        {
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
                    p_area_of_destination = employee.area_of_destination,
                    p_project_code = employee.project_code,
                    p_provider_code = employee.provider_code,
                    p_position = employee.position,
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

        public async Task<IEnumerable<contractor_employee>> GetByProjectCode(string project_code)
        {
            const string query = @"
                SELECT employee_id, name, position, area_of_destination
                FROM contractor_employee
                WHERE project_code = @project_code AND active = 1";

            return await _db.QueryAsync<contractor_employee>(
                query,
                new { project_code = project_code },
                commandType: CommandType.Text
            );
        }

        // Duplicate-enrollment guard used by ContractorService.Create(). The stored
        // procedure matches name case-insensitively (LOWER(TRIM)) + birthdate within the
        // same project, including inactive contractors, and returns at most one row.
        public async Task<bool> ExistsByDetails(string project_code, string name, DateTime birthdate)
        {
            const string storedProc = "sp_contractor_employee_CheckDuplicate";
            var match = await _db.QuerySingleOrDefaultAsync<contractor_employee>(
                storedProc,
                new { p_project_code = project_code, p_name = name, p_birthdate = birthdate },
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