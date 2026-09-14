using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;
using ContractorAttendanceWithHealthDeclaration.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ContractorAttendanceWithHealthDeclaration.Tests.Services
{
    public class SystemConfigServiceTests
    {
        private const string Admin = "ADMIN-1";

        private readonly ISystemConfigRepository _systemConfigRepository = Substitute.For<ISystemConfigRepository>();
        private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();

        private SystemConfigService CreateService() => new(
            _systemConfigRepository, _auditLogService, NullLogger<SystemConfigService>.Instance);

        private void StubValue(string key, string value) =>
            _systemConfigRepository.GetByKey(key).Returns(new system_config { key = key, value = value });

        #region GetDebounceThresholdSeconds

        [Fact]
        public async Task Debounce_parses_the_configured_seconds()
        {
            StubValue("DebounceThresholdSeconds", "45");

            var result = await CreateService().GetDebounceThresholdSeconds();

            Assert.True(result.Success);
            Assert.Equal(45.0, result.Data);
        }

        [Fact]
        public async Task Debounce_missing_config_falls_back_to_30()
        {
            var result = await CreateService().GetDebounceThresholdSeconds();

            Assert.False(result.Success);
            Assert.Equal("DebounceThresholdSeconds configuration not found", result.Message);
            Assert.Equal(30.0, result.Data);
        }

        [Fact]
        public async Task Debounce_unparsable_config_falls_back_to_30()
        {
            StubValue("DebounceThresholdSeconds", "thirty");

            var result = await CreateService().GetDebounceThresholdSeconds();

            Assert.False(result.Success);
            Assert.Equal("Invalid configuration value", result.Message);
            Assert.Equal(30.0, result.Data);
        }

        [Fact]
        public async Task Debounce_repository_failure_falls_back_to_30()
        {
            _systemConfigRepository.GetByKey("DebounceThresholdSeconds")
                .ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetDebounceThresholdSeconds();

            Assert.False(result.Success);
            Assert.Equal("Error retrieving configuration", result.Message);
            Assert.Equal(30.0, result.Data);
        }

        #endregion

        #region GetHealthDeclarationWindowSeconds

        [Fact]
        public async Task Window_parses_the_configured_seconds()
        {
            StubValue("HealthDeclarationWindowSeconds", "90");

            var result = await CreateService().GetHealthDeclarationWindowSeconds();

            Assert.True(result.Success);
            Assert.Equal(90.0, result.Data);
        }

        [Fact]
        public async Task Window_missing_config_falls_back_to_120()
        {
            var result = await CreateService().GetHealthDeclarationWindowSeconds();

            Assert.False(result.Success);
            Assert.Equal(120.0, result.Data);
        }

        [Fact]
        public async Task Window_unparsable_config_falls_back_to_120()
        {
            StubValue("HealthDeclarationWindowSeconds", "two minutes");

            var result = await CreateService().GetHealthDeclarationWindowSeconds();

            Assert.False(result.Success);
            Assert.Equal("Invalid configuration value", result.Message);
            Assert.Equal(120.0, result.Data);
        }

        [Fact]
        public async Task Window_repository_failure_falls_back_to_120()
        {
            _systemConfigRepository.GetByKey("HealthDeclarationWindowSeconds")
                .ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetHealthDeclarationWindowSeconds();

            Assert.False(result.Success);
            Assert.Equal(120.0, result.Data);
        }

        #endregion

        #region GetHealthDeclarationValidityHours

        [Fact]
        public async Task ValidityHours_parses_the_configured_hours()
        {
            StubValue("HealthDeclarationValidityHours", "24");

            var result = await CreateService().GetHealthDeclarationValidityHours();

            Assert.True(result.Success);
            Assert.Equal(24, result.Data);
        }

        [Fact]
        public async Task ValidityHours_missing_config_falls_back_to_12()
        {
            var result = await CreateService().GetHealthDeclarationValidityHours();

            Assert.False(result.Success);
            Assert.Equal(12, result.Data);
        }

        [Fact]
        public async Task ValidityHours_unparsable_config_falls_back_to_12()
        {
            StubValue("HealthDeclarationValidityHours", "half a day");

            var result = await CreateService().GetHealthDeclarationValidityHours();

            Assert.False(result.Success);
            Assert.Equal(12, result.Data);
        }

        [Fact]
        public async Task ValidityHours_repository_failure_falls_back_to_12()
        {
            _systemConfigRepository.GetByKey("HealthDeclarationValidityHours")
                .ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetHealthDeclarationValidityHours();

            Assert.False(result.Success);
            Assert.Equal(12, result.Data);
        }

        #endregion

        #region GetAdminADGroup

        [Fact]
        public async Task AdGroup_returns_the_configured_value_verbatim()
        {
            StubValue("AdminADGroup", "app.custom.admin");

            var result = await CreateService().GetAdminADGroup();

            Assert.True(result.Success);
            Assert.Equal("app.custom.admin", result.Data);
        }

        [Fact]
        public async Task AdGroup_missing_config_falls_back_to_the_default_group()
        {
            var result = await CreateService().GetAdminADGroup();

            Assert.False(result.Success);
            Assert.Equal("app.your_app.admin", result.Data);
        }

        [Fact]
        public async Task AdGroup_repository_failure_falls_back_to_the_default_group()
        {
            _systemConfigRepository.GetByKey("AdminADGroup").ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetAdminADGroup();

            Assert.False(result.Success);
            Assert.Equal("app.your_app.admin", result.Data);
        }

        #endregion

        #region GetScanInputReadOnly

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public async Task ScanInputReadOnly_parses_the_configured_flag(string value, bool expected)
        {
            StubValue("ScanInputReadOnly", value);

            var result = await CreateService().GetScanInputReadOnly();

            Assert.True(result.Success);
            Assert.Equal(expected, result.Data);
        }

        [Fact]
        public async Task ScanInputReadOnly_missing_config_defaults_to_true()
        {
            var result = await CreateService().GetScanInputReadOnly();

            Assert.False(result.Success);
            Assert.True(result.Data);   // safer default
        }

        [Fact]
        public async Task ScanInputReadOnly_unparsable_config_defaults_to_true()
        {
            StubValue("ScanInputReadOnly", "yes");

            var result = await CreateService().GetScanInputReadOnly();

            Assert.False(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public async Task ScanInputReadOnly_repository_failure_defaults_to_true()
        {
            _systemConfigRepository.GetByKey("ScanInputReadOnly").ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetScanInputReadOnly();

            Assert.False(result.Success);
            Assert.True(result.Data);
        }

        #endregion

        #region Waiver texts (blank treated as missing)

        [Fact]
        public async Task Waiver_certification_text_returns_the_configured_value()
        {
            StubValue("WaiverCertificationText", "Custom certification wording");

            var result = await CreateService().GetWaiverCertificationText();

            Assert.True(result.Success);
            Assert.Equal("Custom certification wording", result.Data);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Waiver_certification_blank_or_missing_falls_back_to_default(string? value)
        {
            if (value != null) StubValue("WaiverCertificationText", value);

            var result = await CreateService().GetWaiverCertificationText();

            Assert.False(result.Success);
            Assert.Contains("true, complete, and accurate", result.Data);
        }

        [Fact]
        public async Task Waiver_acknowledgment_text_returns_the_configured_value()
        {
            StubValue("WaiverAcknowledgmentText", "Custom acknowledgment");

            var result = await CreateService().GetWaiverAcknowledgmentText();

            Assert.True(result.Success);
            Assert.Equal("Custom acknowledgment", result.Data);
        }

        [Fact]
        public async Task Waiver_acknowledgment_blank_falls_back_to_default()
        {
            StubValue("WaiverAcknowledgmentText", "  ");

            var result = await CreateService().GetWaiverAcknowledgmentText();

            Assert.False(result.Success);
            Assert.Contains("failure to disclose", result.Data);
        }

        [Fact]
        public async Task Waiver_liability_text_returns_the_configured_value()
        {
            StubValue("WaiverLiabilityReleaseText", "Custom liability wording");

            var result = await CreateService().GetWaiverLiabilityReleaseText();

            Assert.True(result.Success);
            Assert.Equal("Custom liability wording", result.Data);
        }

        [Fact]
        public async Task Waiver_liability_missing_falls_back_to_default()
        {
            var result = await CreateService().GetWaiverLiabilityReleaseText();

            Assert.False(result.Success);
            Assert.Contains("release and hold harmless", result.Data);
        }

        #endregion

        #region GetByKey / GetAll

        [Fact]
        public async Task GetByKey_found_returns_the_row()
        {
            var config = new system_config { key = "TST_ConfigKey", value = "1" };
            _systemConfigRepository.GetByKey("TST_ConfigKey").Returns(config);

            var result = await CreateService().GetByKey("TST_ConfigKey");

            Assert.True(result.Success);
            Assert.Same(config, result.Data);
        }

        [Fact]
        public async Task GetByKey_missing_returns_not_found()
        {
            var result = await CreateService().GetByKey("TST_ConfigKey");

            Assert.False(result.Success);
            Assert.Equal("Configuration not found", result.Message);
        }

        [Fact]
        public async Task GetAll_passes_through_and_tolerates_failures()
        {
            var data = new[] { new system_config { key = "K", value = "V" } };
            _systemConfigRepository.GetAll().Returns(data);

            var ok = await CreateService().GetAll();

            Assert.True(ok.Success);
            Assert.Same(data, ok.Data);

            _systemConfigRepository.GetAll().ThrowsAsync(new InvalidOperationException("db down"));
            var failed = await CreateService().GetAll();

            Assert.False(failed.Success);
            Assert.Equal("Error retrieving configurations", failed.Message);
        }

        #endregion

        #region GetHealthDeclarationSicknessItems

        private static readonly string[] DefaultNineItems =
        {
            "Fever", "Cough", "Cold", "Body Pain", "Headache",
            "Sore Throat", "Fatigue", "Nausea", "Diarrhea"
        };

        [Fact]
        public async Task Sickness_missing_config_returns_the_nine_defaults()
        {
            var result = await CreateService().GetHealthDeclarationSicknessItems();

            Assert.False(result.Success);
            Assert.Equal(DefaultNineItems, result.Data);
        }

        [Fact]
        public async Task Sickness_blank_value_returns_the_nine_defaults()
        {
            StubValue("Health_Declaration_Sickness", "   ");

            var result = await CreateService().GetHealthDeclarationSicknessItems();

            Assert.False(result.Success);
            Assert.Equal(DefaultNineItems, result.Data);
        }

        [Fact]
        public async Task Sickness_separators_only_value_returns_the_nine_defaults()
        {
            StubValue("Health_Declaration_Sickness", " ;; ; ");

            var result = await CreateService().GetHealthDeclarationSicknessItems();

            Assert.False(result.Success);
            Assert.Equal("Empty sickness configuration, using defaults", result.Message);
            Assert.Equal(DefaultNineItems, result.Data);
        }

        [Fact]
        public async Task Sickness_values_are_trimmed_and_parsed()
        {
            StubValue("Health_Declaration_Sickness", " Fever ;  Cough  ;Allergic Rhinitis");

            var result = await CreateService().GetHealthDeclarationSicknessItems();

            Assert.True(result.Success);
            Assert.Equal(new[] { "Fever", "Cough", "Allergic Rhinitis" }, result.Data);
        }

        // PIN: an exception returns a DIFFERENT (3-item) default than the missing/blank path.
        [Fact]
        public async Task Sickness_repository_failure_returns_the_three_item_default()
        {
            _systemConfigRepository.GetByKey("Health_Declaration_Sickness")
                .ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetHealthDeclarationSicknessItems();

            Assert.False(result.Success);
            Assert.Equal(new[] { "Fever", "Cough", "Cold" }, result.Data);
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_audits_from_the_previous_value()
        {
            var existing = new system_config { key = "TST_ConfigKey", value = "old" };
            var submitted = new system_config { key = "TST_ConfigKey", value = "new" };
            _systemConfigRepository.GetByKey("TST_ConfigKey").Returns(existing);
            _systemConfigRepository.Update(Arg.Any<system_config>()).Returns(c => c.Arg<system_config>());

            var result = await CreateService().Update(submitted, Admin);

            Assert.True(result.Success);
            Assert.True(result.Data);
            await _auditLogService.Received(1).Log(
                "system_config", "update", "TST_ConfigKey", existing,
                Arg.Is<system_config>(c => c.value == "new"), Admin);
        }

        // A null repository result (key does not exist — the SP matched nothing)
        // must be reported as a failure, without an audit entry.
        [Fact]
        public async Task Update_reports_failure_when_the_save_fails()
        {
            _systemConfigRepository.GetByKey("TST_ConfigKey").Returns((system_config?)null);
            _systemConfigRepository.Update(Arg.Any<system_config>()).Returns((system_config)null!);

            var result = await CreateService().Update(new system_config { key = "TST_ConfigKey", value = "new" }, Admin);

            Assert.False(result.Success);
            Assert.Equal("Error updating configuration", result.Message);
            Assert.False(result.Data);
            await _auditLogService.DidNotReceive().Log(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<string>());
        }

        [Fact]
        public async Task Update_null_config_returns_error_envelope()
        {
            var result = await CreateService().Update(null!, Admin);

            Assert.False(result.Success);
            Assert.Equal("Error updating configuration", result.Message);
        }

        #endregion
    }
}
