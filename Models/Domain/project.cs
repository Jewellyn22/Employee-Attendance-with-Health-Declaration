#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters
namespace ContractorAttendanceWithHealthDeclaration.Models.Domain
{
    public class project
    {
        public string project_code { get; set; }
        public string project_name { get; set; }
        public string provider_code { get; set; }
        public string provider_name { get; set; }
        public string provider_pic { get; set; }
        public string provider_pic_number { get; set; }
        public string area_of_destination { get; set; }
        public DateTime? contract_startdate { get; set; }
        public DateTime? contract_enddate { get; set; }
        public int? contractor_count { get; set; }
        public int active { get; set; }

        // Soft-delete flag: 1 = deleted via sp_project_Delete (hidden everywhere, row retained).
        public int is_deleted { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}