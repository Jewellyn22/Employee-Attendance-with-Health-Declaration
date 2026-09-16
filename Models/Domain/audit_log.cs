#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters
namespace EmployeeAttendanceWithHealthDeclaration.Models.Domain
{
    // Generic audit-log row. Reusable across features via IAuditLogService.Log(...).
    // data_from / data_to are stored as JSON in MySQL and mapped to strings here;
    // AuditLogService handles (de)serialization.
    public class audit_log
    {
        public int log_id { get; set; }
        public string entity_type { get; set; }       // e.g. bulk_enrollment, employee, timelog
        public string action { get; set; }            // e.g. bulk_create, create, update
        public string reference_id { get; set; }      // e.g. project_code / employee_id
        public string? data_from { get; set; }        // JSON: previous state (null on creates)
        public string? data_to { get; set; }          // JSON: new state / payload
        public string updated_by { get; set; }        // admin EmployeeNumber from session
        public DateTime created_at { get; set; }
    }
}
