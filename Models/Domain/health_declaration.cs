namespace ContractorAttendanceWithHealthDeclaration.Models.Domain
{
    public class health_declaration
    {
        public int id { get; set; }
        public int attendance_id { get; set; }
        public string employee_id { get; set; }
        public string declaration { get; set; }  // JSON string
        public string waiver { get; set; }  // JSON string
        public DateTime created_at { get; set; }
    }
}