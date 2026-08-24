using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;
using System.Text.Json;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _repository;
        private readonly ILogger<AuditLogService> _logger;

        // Snake_case JSON so audit payloads match the rest of the API serialization.
        // WriteIndented so the stored data_from/data_to are human-readable in the DB
        // (the columns are LONGTEXT, which preserves indentation unlike MySQL JSON).
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };

        public AuditLogService(IAuditLogRepository repository, ILogger<AuditLogService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Response<audit_log>> Log(
            string entity_type,
            string action,
            string reference_id,
            object? data_from,
            object? data_to,
            string updated_by)
        {
            try
            {
                var entry = new audit_log
                {
                    entity_type = entity_type,
                    action = action,
                    reference_id = reference_id,
                    data_from = data_from == null ? null : JsonSerializer.Serialize(data_from, JsonOptions),
                    data_to = data_to == null ? null : JsonSerializer.Serialize(data_to, JsonOptions),
                    updated_by = string.IsNullOrWhiteSpace(updated_by) ? "System" : updated_by
                };

                var created = await _repository.Create(entry);
                return new Response<audit_log> { Success = true, Data = created };
            }
            catch (Exception ex)
            {
                // Logging must never break the calling operation; surface failure in the Response.
                _logger.LogError(ex, "Error writing audit log ({EntityType}/{Action})", entity_type, action);
                return new Response<audit_log> { Success = false, Message = "Error writing audit log" };
            }
        }

        public async Task<Response<IEnumerable<audit_log>>> GetAll()
        {
            try
            {
                var entries = await _repository.GetAll();
                return new Response<IEnumerable<audit_log>>
                {
                    Success = true,
                    Message = "Audit logs retrieved successfully",
                    Data = entries
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                return new Response<IEnumerable<audit_log>>
                {
                    Success = false,
                    Message = "Error retrieving audit logs"
                };
            }
        }

        public async Task<Response<IEnumerable<audit_log>>> GetByEntity(string entity_type, string action)
        {
            try
            {
                var entries = await _repository.GetByEntity(entity_type, action);
                return new Response<IEnumerable<audit_log>>
                {
                    Success = true,
                    Message = "Audit logs retrieved successfully",
                    Data = entries
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs ({EntityType}/{Action})", entity_type, action);
                return new Response<IEnumerable<audit_log>>
                {
                    Success = false,
                    Message = "Error retrieving audit logs"
                };
            }
        }
    }
}
