using ContractorAttendanceWithHealthDeclaration.Helpers;
using Xunit;

namespace ContractorAttendanceWithHealthDeclaration.Tests.Helpers
{
    public class QrCodeHelperTests
    {
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        [Fact]
        public void Same_content_produces_byte_identical_png()
        {
            var first = QrCodeHelper.GeneratePng("TST-000001");
            var second = QrCodeHelper.GeneratePng("TST-000001");

            Assert.True(first.SequenceEqual(second), "QR generation must be deterministic for the same content");
        }

        [Fact]
        public void Generated_image_is_a_valid_png()
        {
            var png = QrCodeHelper.GeneratePng("TST-000001");

            Assert.NotEmpty(png);
            Assert.True(png.Length > 100, $"PNG suspiciously small: {png.Length} bytes");
            Assert.True(png.Length < 64 * 1024, $"PNG exceeds the 64 KB qr_code_image BLOB: {png.Length} bytes");
            Assert.Equal(PngSignature, png.Take(PngSignature.Length).ToArray());
        }

        [Fact]
        public void Different_content_produces_different_bytes()
        {
            var a = QrCodeHelper.GeneratePng("TST-000001");
            var b = QrCodeHelper.GeneratePng("TST-000002");

            Assert.False(a.SequenceEqual(b));
        }

        [Fact]
        public void Smaller_pixels_per_module_produces_a_smaller_png()
        {
            var large = QrCodeHelper.GeneratePng("TST-000001", pixels_per_module: 8);
            var small = QrCodeHelper.GeneratePng("TST-000001", pixels_per_module: 4);

            Assert.True(small.Length < large.Length,
                $"4px/module ({small.Length}B) should be smaller than 8px/module ({large.Length}B)");
        }

        [Fact]
        public void Default_pixels_per_module_matches_documented_default()
        {
            var explicitDefault = QrCodeHelper.GeneratePng("TST-000001", 8);
            var implicitDefault = QrCodeHelper.GeneratePng("TST-000001");

            Assert.True(explicitDefault.SequenceEqual(implicitDefault));
        }
    }
}
