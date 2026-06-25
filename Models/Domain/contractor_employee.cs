namespace ContractorAttendanceWithHealthDeclaration.Models.Domain
{
    public class contractor_employee
    {
        public string employee_id { get; set; }
        public string name { get; set; }
        public int age { get; set; }
        public string gender { get; set; }
        public DateTime? birthdate { get; set; }
        public string contact_number { get; set; }
        public string address { get; set; }
        public string area_of_destination { get; set; }
        public string project_code { get; set; }
        public string provider_code { get; set; }
        public string position { get; set; }
        public int active { get; set; }
        public DateTime create_at { get; set; }
        public DateTime update_at { get; set; }
    }
}