using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public interface IContractorEmployeeRepository
    {
        Task<IEnumerable<contractor_employee>> GetAll();
        // Returns the contractor regardless of active status (active filter is enforced in
        // C#). Used by both the scan flow (ProcessScan checks active) and admin Update.
        Task<contractor_employee?> GetByEmployeeId(string employee_id);
        Task<IEnumerable<contractor_employee>> GetByProjectCode(string project_code);
        // Duplicate-enrollment check: true if a contractor with the same name
        // (case-insensitive) + birthdate already exists under the provider (active or not).
        Task<bool> ExistsByDetails(string provider_code, string name, DateTime birthdate);
        Task<contractor_employee?> Create(contractor_employee employee);
        Task<contractor_employee?> Update(contractor_employee employee);
        Task<bool> SetInactive(string employee_id);
        // Soft delete via sp_contractor_employee_Delete: marks the given ids is_deleted = 1.
        // p_employee_ids is a CSV matched with FIND_IN_SET; single delete = one-element list.
        Task<contractor_delete_result?> Delete(IEnumerable<string> employee_ids);
        Task<int> GetActiveCount();
    }
}