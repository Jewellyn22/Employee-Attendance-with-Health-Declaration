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
        // Merge-on-duplicate: when the same name + birthdate is already enrolled under
        // the provider, the existing account is updated (details overwritten,
        // assignments replace-or-append) instead of a new one being created.
        Task<Response<contractor_employee>> Create(contractor_employee employee, string admin_employee_id, bool log_audit = true);
        Task<Response<contractor_employee>> Update(contractor_employee employee, string admin_employee_id);
        Task<Response<bool>> SetInactive(string employee_id);
        // Soft delete: marks the listed contractors is_deleted = 1 (rows + time_logs
        // retained; they vanish from the admin table and kiosk scans). Single delete
        // = one-element list. Writes one audit_log entry (action "delete",
        // data_from = deleted contractors).
        Task<Response<int>> Delete(List<string> employee_ids, string admin_employee_id);
        Task<Response<int>> GetActiveCount();

        // Project-status cascade: deactivate every active contractor whose LAST active
        // project is this one (employees still on another active project are skipped),
        // each audit-logged as contractor/deactivate_by_project so a later project
        // re-activation can restore exactly these contractors. Mirrors the nightly
        // expiry sweep (sp_project_DeactivateExpired) for manual admin deactivation.
        Task<Response<int>> CascadeDeactivateByProject(string project_code, string admin_employee_id);

        // Restore contractors deactivated by a project cascade. Audit-verified:
        // only contractors whose latest active 1->0 transition was a
        // deactivate_by_project entry for this project AND who are still assigned
        // to it are re-activated; contractors deactivated individually by an admin
        // stay In-Active.
        Task<Response<int>> CascadeReactivateByProject(string project_code, string admin_employee_id);

        // Bulk-import contractors for a single provider/project from a parsed file.
        // Reuses Create() per row (field + DOLE 18+ + existence validation, auto employee_id);
        // already-enrolled rows merge into their existing account and are reported
        // separately (merged_count / merged_employee_ids).
        // Returns per-batch totals + per-row errors; writes one audit_log batch row.
        Task<Response<bulk_enrollment_result>> BulkCreate(bulk_enrollment_request request, string admin_employee_id);
    }
}