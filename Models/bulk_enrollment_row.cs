#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters
namespace EmployeeAttendanceWithHealthDeclaration.Models
{
    // One parsed row from a bulk-import CSV/Excel file.
    // provider_code/project_code come from the modal selects (see bulk_enrollment_request),
    // NOT from the file. Area of Destination comes from the selected project (registered
    // on the project since v3.0.0.0). employee_id and active are assigned by the service/SP on insert.
    public class bulk_enrollment_row
    {
        // 1-based row number in the source file (for error reporting).
        public int row_number { get; set; }
        public string name { get; set; }
        public string gender { get; set; }                 // Male | Female
        public DateTime? birthdate { get; set; }           // YYYY-MM-DD
        public string contact_number { get; set; }         // optional
        public string address { get; set; }                // optional
        public string position { get; set; }
    }
}
