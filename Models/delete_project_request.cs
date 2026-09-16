#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters

namespace EmployeeAttendanceWithHealthDeclaration.Models
{
    // Payload POSTed from the Projects admin table delete button.
    public class delete_project_request
    {
        public string project_code { get; set; }
    }
}
