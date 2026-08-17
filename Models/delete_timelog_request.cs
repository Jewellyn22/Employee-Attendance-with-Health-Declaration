#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters

namespace ContractorAttendanceWithHealthDeclaration.Models
{
    // Payload POSTed from the TimeLogs admin table delete button.
    public class delete_timelog_request
    {
        public int attendance_id { get; set; }
    }
}
