using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public interface IContractorService
    {
        Task<Response<contractor_employee>> GetByEmployeeId(string employee_id);
        Task<Response<IEnumerable<contractor_employee>>> GetAll();
        Task<Response<IEnumerable<contractor_employee>>> GetByProjectCode(string project_code);
        // log_audit defaults true for single-row creates; BulkCreate passes false and
        // writes its own single bulk_enrollment summary entry instead.
        Task<Response<contractor_employee>> Create(contractor_employee employee, string admin_employee_id, bool log_audit = true);
        Task<Response<contractor_employee>> Update(contractor_employee employee, string admin_employee_id);
        Task<Response<bool>> SetInactive(string employee_id);
        Task<Response<int>> GetActiveCount();

        // Bulk-import contractors for a single provider/project from a parsed file.
        // Reuses Create() per row (field + DOLE 18+ + existence validation, auto employee_id).
        // Returns per-batch totals + per-row errors; writes one audit_log batch row.
        Task<Response<bulk_enrollment_result>> BulkCreate(bulk_enrollment_request request, string admin_employee_id);
    }
}