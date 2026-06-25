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

        public async Task<contractor_employee> Create(contractor_employee employee)
        {
            const string storedProc = "sp_contractor_employee_Create";
            return await _db.QuerySingleOrDefaultAsync<contractor_employee>(
                storedProc,
                new
                {
                    p_employee_id = employee.employee_id,
                    p_name = employee.name,
                    p_age = employee.age,
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

        public async Task<contractor_employee> Update(contractor_employee employee)
        {
            const string storedProc = "sp_contractor_employee_Update";
            return await _db.QuerySingleOrDefaultAsync<contractor_employee>(
                storedProc,
                new
                {
                    p_employee_id = employee.employee_id,
                    p_name = employee.name,
                    p_age = employee.age,
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
    }
}