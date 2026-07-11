#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters
namespace ContractorAttendanceWithHealthDeclaration.Models.Domain
{
    public class provider
    {
        public string provider_code { get; set; }
        public string provider_name { get; set; }
        public string provider_address { get; set; }
        public int active { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}