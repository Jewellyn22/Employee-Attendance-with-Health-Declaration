#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters

namespace EmployeeAttendanceWithHealthDeclaration.Models
{
    // Payload POSTed from the Employees admin table. A single-row delete
    // sends a one-element list; the Delete Selected button sends every
    // checked employee_id (across pages/filters).
    public class delete_employees_request
    {
        public List<string> employee_ids { get; set; } = new List<string>();
    }
}
