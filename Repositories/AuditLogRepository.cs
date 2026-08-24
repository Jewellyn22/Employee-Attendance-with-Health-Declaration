using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using Dapper;
using System.Data;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly IDbConnection _db;

        public AuditLogRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<audit_log> Create(audit_log entry)
        {
            const string storedProc = "sp_audit_log_Create";
            return await _db.QuerySingleOrDefaultAsync<audit_log>(
                storedProc,
                new
                {
                    p_entity_type = entry.entity_type,
                    p_action = entry.action,
                    p_reference_id = entry.reference_id,
                    // MySQL accepts a valid JSON string for a JSON column/parameter.
                    p_data_from = entry.data_from,
                    p_data_to = entry.data_to,
                    p_updated_by = entry.updated_by
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<audit_log>> GetAll()
        {
            const string storedProc = "sp_audit_log_GetAll";
            return await _db.QueryAsync<audit_log>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<audit_log>> GetByEntity(string entity_type, string action)
        {
            const string storedProc = "sp_audit_log_GetByEntity";
            return await _db.QueryAsync<audit_log>(
                storedProc,
                new { p_entity_type = entity_type, p_action = action },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
