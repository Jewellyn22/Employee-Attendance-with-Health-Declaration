using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public interface IAuditLogRepository
    {
        Task<audit_log> Create(audit_log entry);
        Task<IEnumerable<audit_log>> GetAll();
        // Entries for one entity_type + action, ordered by log_id (stable ordering
        // for "latest transition" logic in the project re-activation restore).
        Task<IEnumerable<audit_log>> GetByEntity(string entity_type, string action);
    }
}
