#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters
namespace ContractorAttendanceWithHealthDeclaration.Models
{
    // A single failed row in a bulk import, with the reason.
    public class bulk_enrollment_error
    {
        public int row { get; set; }
        public string name { get; set; }
        public string message { get; set; }
    }
}
