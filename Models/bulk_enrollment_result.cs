#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters
using System.Collections.Generic;

namespace ContractorAttendanceWithHealthDeclaration.Models
{
    // Outcome of one bulk-import batch. Returned to the UI and stored (as JSON)
    // in audit_log.data_to for the batch row, so a single log entry is self-describing.
    // success_count = created + merged; enrolled_employee_ids holds newly created IDs
    // only, merged_employee_ids the existing accounts a duplicate row was merged into.
    public class bulk_enrollment_result
    {
        public int total { get; set; }
        public int success_count { get; set; }
        public int error_count { get; set; }
        public int merged_count { get; set; }
        public string provider_code { get; set; }
        public string project_code { get; set; }
        public string file_name { get; set; }
        public List<bulk_enrollment_error> errors { get; set; } = new();
        public List<string> enrolled_employee_ids { get; set; } = new();
        public List<string> merged_employee_ids { get; set; } = new();
        public int log_id { get; set; }
    }
}
