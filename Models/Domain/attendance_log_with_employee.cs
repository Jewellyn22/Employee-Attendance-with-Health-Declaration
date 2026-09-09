namespace ContractorAttendanceWithHealthDeclaration.Models.Domain
{
    public class attendance_log_with_employee
    {
        public int attendance_id { get; set; }
        public string employee_id { get; set; }
        public string name { get; set; }          // From contractor_employee
        public string provider_code { get; set; }  // From contractor_employee
        public string provider_name { get; set; }  // From providers table
        public string project_code { get; set; }   // CSV via contractor_project (multi-project)
        public string project_name { get; set; }   // CSV via contractor_project (multi-project)
        public DateTime? time_in { get; set; }
        public DateTime? time_out { get; set; }
        public string health_status { get; set; }
        public string waiver_consent { get; set; }  // 'UNDERSTOOD', 'NOT_UNDERSTOOD'
    }
}
