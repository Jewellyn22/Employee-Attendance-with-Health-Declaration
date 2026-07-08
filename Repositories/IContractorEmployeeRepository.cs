using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public interface IContractorEmployeeRepository
    {
        Task<IEnumerable<contractor_employee>> GetAll();
        Task<contractor_employee?> GetByEmployeeId(string employee_id);
        Task<contractor_employee?> Create(contractor_employee employee);
        Task<contractor_employee> Update(contractor_employee employee);
        Task<bool> SetInactive(string employee_id);
    }
}