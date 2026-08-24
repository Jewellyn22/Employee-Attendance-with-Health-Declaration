#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters

namespace ContractorAttendanceWithHealthDeclaration.Models
{
    // Result set returned by sp_contractor_employee_Delete (soft delete: no rows are removed).
    public class contractor_delete_result
    {
        public bool success { get; set; }
        public int deleted_count { get; set; }
    }
}
