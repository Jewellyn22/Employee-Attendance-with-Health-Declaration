using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public interface IContractorEmployeeRepository
    {
        Task<IEnumerable<contractor_employee>> GetAll();
        Task<contractor_employee?> GetByEmployeeId(string employee_id);
        // Includes inactive contractors (active = 0). Use ONLY for admin Update existence checks;
        // the scan flow must keep using GetByEmployeeId so terminated contractors cannot scan in.
        Task<contractor_employee?> GetByEmployeeIdForUpdate(string employee_id);
        Task<IEnumerable<contractor_employee>> GetByProjectCode(string project_code);
        Task<contractor_employee?> Create(contractor_employee employee);
        Task<contractor_employee?> Update(contractor_employee employee);
        Task<bool> SetInactive(string employee_id);
        Task<int> GetActiveCount();
    }
}