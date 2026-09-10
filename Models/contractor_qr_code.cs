#pragma warning disable CS8981 // Type name only contains lower-cased ascii characters

namespace ContractorAttendanceWithHealthDeclaration.Models
{
    // Lightweight projection served by GET /Admin/GetContractorQrCodes for the
    // Contractors Excel export only: the stored QR PNG (generated at enrollment,
    // never at export time) as employee_id -> base64 pairs. Kept separate from the
    // contractor_employee domain model so the blob never reaches the grid payload
    // or the audit_log JSON snapshots.
    public class contractor_qr_code
    {
        public string employee_id { get; set; }
        // System.Text.Json serializes byte[] as a base64 string — exactly what
        // ExcelJS addImage({ base64 }) expects (no "data:" prefix).
        public byte[] qr_code_image { get; set; }
    }
}
