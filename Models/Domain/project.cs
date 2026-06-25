namespace ContractorAttendanceWithHealthDeclaration.Models.Domain
{
    public class project
    {
        public string project_code { get; set; }
        public string project_name { get; set; }
        public string provider_code { get; set; }
        public DateTime? contract_startdate { get; set; }
        public DateTime? contract_enddate { get; set; }
        public int active { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}