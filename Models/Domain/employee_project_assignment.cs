namespace EmployeeAttendanceWithHealthDeclaration.Models.Domain
{
    // One per-project assignment row: the employee's position is specific to the
    // project (e.g., Admin on Project 1, Safety Officer on Project 2). Carried on
    // employee.assignments (inbound create/update payload from the admin
    // add-row picker / bulk enrollment) and serialized to the p_projects JSON param
    // ([{project_code, position}, ...]) of sp_employee_Create/_Update.
    public class employee_project_assignment
    {
        public string project_code { get; set; } = string.Empty;
        public string position { get; set; } = string.Empty;
    }
}
