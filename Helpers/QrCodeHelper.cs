using QRCoder;

namespace ContractorAttendanceWithHealthDeclaration.Helpers
{
    /// <summary>
    /// QR-code PNG generation for contractor ID badges. The QR content is the
    /// employee_id (immutable), so the image is fully derived data: generated once
    /// at enrollment (ContractorService.CreateOrMerge) and persisted to
    /// contractor_employee.qr_code_image — never regenerated at export time.
    /// Uses PngByteQRCode (pure managed PNG writer, no System.Drawing/GDI) so it
    /// stays host-agnostic.
    /// </summary>
    public static class QrCodeHelper
    {
        /// <summary>
        /// Renders the given content as a black-on-white PNG QR code.
        /// ECC level Q = 25% damage recovery (badge printing/wear); 8 px per module
        /// keeps the stored PNG print-sharp (~260 px square, ~1-2 KB, well inside the
        /// 64 KB BLOB column).
        /// </summary>
        public static byte[] GeneratePng(string content, int pixels_per_module = 8)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qr = new PngByteQRCode(data);
            return qr.GetGraphic(pixels_per_module);
        }
    }
}
