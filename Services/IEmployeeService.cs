using EmployeeAttendanceWithHealthDeclaration.Models;
using EmployeeAttendanceWithHealthDeclaration.Models.Domain;

namespace EmployeeAttendanceWithHealthDeclaration.Services
{
    public interface IEmployeeService
    {
        Task<Response<employee>> GetByEmployeeId(string employee_id);
        Task<Response<IEnumerable<employee>>> GetAll();
        // Export-only read for the Employees Excel export: the stored QR PNG per
        // enrolled employee (base64 over the wire). QRs are generated once at
        // enrollment; this never generates.
        Task<Response<IEnumerable<employee_qr_code>>> GetQrCodes();
        Task<Response<IEnumerable<employee>>> GetByProjectCode(string project_code);
        // log_audit defaults true for single-row creates; BulkCreate passes false and
        // writes its own single bulk_enrollment summary entry instead.
        // Merge-on-duplicate: when the same name + birthdate is already enrolled under
        // the provider, the existing account is updated (details overwritten,
        // assignments replace-or-append) instead of a new one being created.
        Task<Response<employee>> Create(employee employee, string admin_employee_id, bool log_audit = true);
        Task<Response<employee>> Update(employee employee, string admin_employee_id);
        Task<Response<bool>> SetInactive(string employee_id);
        // Soft delete: marks the listed employees is_deleted = 1 (rows + time_logs
        // retained; they vanish from the admin table and kiosk scans). Single delete
        // = one-element list. Writes one audit_log entry (action "delete",
        // data_from = deleted employees).
        Task<Response<int>> Delete(List<string> employee_ids, string admin_employee_id);
        Task<Response<int>> GetActiveCount();

        // Project-status cascade: deactivate every active employee whose LAST active
        // project is this one (employees still on another active project are skipped),
        // each audit-logged as employee/deactivate_by_project so a later project
        // re-activation can restore exactly these employees. Mirrors the nightly
        // expiry sweep (sp_project_DeactivateExpired) for manual admin deactivation.
        Task<Response<int>> CascadeDeactivateByProject(string project_code, string admin_employee_id);

        // Restore employees deactivated by a project cascade. Audit-verified:
        // only employees whose latest active 1->0 transition was a
        // deactivate_by_project entry for this project AND who are still assigned
        // to it are re-activated; employees deactivated individually by an admin
        // stay In-Active.
        Task<Response<int>> CascadeReactivateByProject(string project_code, string admin_employee_id);

        // Bulk-import employees for a single provider/project from a parsed file.
        // Reuses Create() per row (field + DOLE 18+ + existence validation, auto employee_id);
        // already-enrolled rows merge into their existing account and are reported
        // separately (merged_count / merged_employee_ids).
        // Returns per-batch totals + per-row errors; writes one audit_log batch row.
        Task<Response<bulk_enrollment_result>> BulkCreate(bulk_enrollment_request request, string admin_employee_id);
    }
}