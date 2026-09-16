#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters

namespace EmployeeAttendanceWithHealthDeclaration.Models
{
    // Result set returned by sp_employee_Delete (soft delete: no rows are removed).
    public class employee_delete_result
    {
        public bool success { get; set; }
        public int deleted_count { get; set; }
    }
}
