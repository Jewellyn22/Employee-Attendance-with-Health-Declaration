using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public interface IAuditLogRepository
    {
        Task<audit_log> Create(audit_log entry);
        Task<IEnumerable<audit_log>> GetAll();
    }
}
