namespace ContractorAttendanceWithHealthDeclaration.Models.Domain
{
    public class time_log
    {
        public int attendance_id { get; set; }
        public string employee_id { get; set; }
        public DateTime? time_in { get; set; }
        public DateTime? time_out { get; set; }
        public string health_status { get; set; }  //'FIT', 'UNFIT'
        public DateTime created_at { get; set; }
        public string updated_by { get; set; }
        public DateTime? updated_at { get; set; }
    }
}