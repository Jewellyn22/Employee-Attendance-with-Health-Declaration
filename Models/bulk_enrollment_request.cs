#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters
using System.Collections.Generic;

namespace ContractorAttendanceWithHealthDeclaration.Models
{
    // Payload POSTed from the bulk-import modal after client-side parsing.
    public class bulk_enrollment_request
    {
        public string provider_code { get; set; }
        public string project_code { get; set; }
        public string file_name { get; set; }
        public List<bulk_enrollment_row> contractors { get; set; } = new();
    }
}
