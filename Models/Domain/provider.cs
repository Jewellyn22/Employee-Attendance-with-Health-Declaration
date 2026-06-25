namespace ContractorAttendanceWithHealthDeclaration.Models.Domain
{
    public class provider
    {
        public string provider_code { get; set; }
        public string provider_name { get; set; }
        public string provider_address { get; set; }
        public string provider_pic { get; set; }
        public string provider_pic_number { get; set; }
        public int active { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}