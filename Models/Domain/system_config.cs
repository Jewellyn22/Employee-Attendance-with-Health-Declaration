namespace ContractorAttendanceWithHealthDeclaration.Models.Domain
{
    public class system_config
    {
        public int id { get; set; }
        public string key { get; set; }
        public string value { get; set; }
        public string description { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}