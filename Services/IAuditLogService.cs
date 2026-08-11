using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public interface IAuditLogService
    {
        // Generic, reusable audit logger. data_from/data_to are serialized to JSON
        // (snake_case to match the rest of the API). Pass null for data_from on
        // pure creates. Returns the created audit_log row (incl. log_id).
        Task<Response<audit_log>> Log(
            string entity_type,
            string action,
            string reference_id,
            object? data_from,
            object? data_to,
            string updated_by);

        Task<Response<IEnumerable<audit_log>>> GetAll();
    }
}
