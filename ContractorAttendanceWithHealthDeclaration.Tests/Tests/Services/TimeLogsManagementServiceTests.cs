using ContractorAttendanceWithHealthDeclaration.Helpers;
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
    public class TimeLogsManagementServiceTests
    {
        private const string Admin = "ADMIN-1";

        private readonly ITimeLogsRepository _timeLogsRepository = Substitute.For<ITimeLogsRepository>();
        private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
        private readonly List<time_log> _capturedUpdates = new();

        private TimeLogsManagementService CreateService() => new(
            _timeLogsRepository, _auditLogService, NullLogger<TimeLogsManagementService>.Instance);

        public TimeLogsManagementServiceTests()
        {
            _timeLogsRepository.Update(Arg.Do<time_log>(t => _capturedUpdates.Add(t)))
                .Returns(c => c.Arg<time_log>());
            _timeLogsRepository.Delete(Arg.Any<int>()).Returns(true);
        }

        private static time_log TimeLog(int attendance_id = 1, string health_status = HealthConstants.StatusFit,
            string waiver_consent = HealthConstants.WaiverUnderstood, DateTime? time_in = null, DateTime? time_out = null) =>
            new()
            {
                attendance_id = attendance_id,
                employee_id = "TST-000001",
                time_in = time_in ?? DateTime.Now.AddHours(-1),
                time_out = time_out,
                health_status = health_status,
                waiver_consent = waiver_consent
            };

        #region GetTimeLogById

        [Fact]
        public async Task GetTimeLogById_found_returns_record()
        {
            var timelog = TimeLog();
            _timeLogsRepository.GetByAttendanceId(1).Returns(timelog);

            var result = await CreateService().GetTimeLogById(1);

            Assert.True(result.Success);
            Assert.Same(timelog, result.Data);
        }

        [Fact]
        public async Task GetTimeLogById_missing_returns_not_found()
        {
            _timeLogsRepository.GetByAttendanceId(1).Returns((time_log?)null);

            var result = await CreateService().GetTimeLogById(1);

            Assert.False(result.Success);
            Assert.Equal("Timelog record not found", result.Message);
        }

        [Fact]
        public async Task GetTimeLogById_repository_failure_returns_error_envelope()
        {
            _timeLogsRepository.GetByAttendanceId(1).ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetTimeLogById(1);

            Assert.False(result.Success);
            Assert.Equal("Error retrieving timelog", result.Message);
        }

        #endregion

        #region UpdateTimeLog

        [Fact]
        public async Task UpdateTimeLog_invalid_health_status_is_rejected_without_save()
        {
            var result = await CreateService().UpdateTimeLog(TimeLog(health_status: "SICK"), Admin);

            Assert.False(result.Success);
            Assert.Equal("Invalid health status. Must be 'FIT' or 'UNFIT'", result.Message);
            Assert.Empty(_capturedUpdates);
        }

        [Fact]
        public async Task UpdateTimeLog_invalid_waiver_consent_is_rejected_without_save()
        {
            var result = await CreateService().UpdateTimeLog(TimeLog(waiver_consent: "maybe"), Admin);

            Assert.False(result.Success);
            Assert.Equal("Invalid waiver consent. Must be 'UNDERSTOOD' or 'NOT_UNDERSTOOD'", result.Message);
            Assert.Empty(_capturedUpdates);
        }

        [Fact]
        public async Task UpdateTimeLog_unfit_with_blank_time_out_auto_fills_it_with_time_in()
        {
            var timeIn = DateTime.Now.AddHours(-1);
            var timelog = TimeLog(health_status: HealthConstants.StatusUnfit, time_in: timeIn, time_out: null);

            var result = await CreateService().UpdateTimeLog(timelog, Admin);

            Assert.True(result.Success, result.Message);
            var saved = _capturedUpdates.Single();
            Assert.Equal(timeIn, saved.time_out);   // zero-duration session
        }

        [Fact]
        public async Task UpdateTimeLog_existing_time_out_is_never_overwritten()
        {
            var timeOut = DateTime.Now.AddMinutes(-30);
            var timelog = TimeLog(health_status: HealthConstants.StatusUnfit, time_out: timeOut);

            var result = await CreateService().UpdateTimeLog(timelog, Admin);

            Assert.True(result.Success, result.Message);
            Assert.Equal(timeOut, _capturedUpdates.Single().time_out);
        }

        [Fact]
        public async Task UpdateTimeLog_fit_understood_neither_fills_nor_clears_time_out()
        {
            var timelog = TimeLog(health_status: HealthConstants.StatusFit, time_out: null);

            var result = await CreateService().UpdateTimeLog(timelog, Admin);

            Assert.True(result.Success, result.Message);
            Assert.Null(_capturedUpdates.Single().time_out);
        }

        [Fact]
        public async Task UpdateTimeLog_not_allowed_combo_with_null_time_in_is_left_alone()
        {
            var timelog = TimeLog(health_status: HealthConstants.StatusUnfit);
            timelog.time_in = null;

            var result = await CreateService().UpdateTimeLog(timelog, Admin);

            Assert.True(result.Success, result.Message);
            Assert.Null(_capturedUpdates.Single().time_out);   // nothing to fill from
        }

        [Fact]
        public async Task UpdateTimeLog_stamps_audit_fields_and_audits_the_change()
        {
            var before = DateTime.Now;
            var existing = TimeLog();
            var timelog = TimeLog(health_status: HealthConstants.StatusUnfit);
            _timeLogsRepository.GetByAttendanceId(1).Returns(existing);

            var result = await CreateService().UpdateTimeLog(timelog, Admin);

            Assert.True(result.Success, result.Message);
            var saved = _capturedUpdates.Single();
            Assert.Equal(Admin, saved.updated_by);
            Assert.NotNull(saved.updated_at);
            Assert.InRange(saved.updated_at!.Value, before.AddSeconds(-1), DateTime.Now.AddSeconds(1));

            await _auditLogService.Received(1).Log(
                "timelog", "update", "1", existing,
                Arg.Any<time_log>(), Admin);
        }

        [Fact]
        public async Task UpdateTimeLog_repository_failure_returns_error_envelope()
        {
            _timeLogsRepository.Update(Arg.Any<time_log>()).ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().UpdateTimeLog(TimeLog(), Admin);

            Assert.False(result.Success);
            Assert.Equal("Error updating timelog", result.Message);
        }

        [Fact]
        public async Task UpdateTimeLog_null_timelog_returns_the_error_envelope()
        {
            var result = await CreateService().UpdateTimeLog(null!, Admin);

            Assert.False(result.Success);
            Assert.Equal("Invalid timelog data", result.Message);
            Assert.Empty(_capturedUpdates);
            await _auditLogService.DidNotReceive().Log(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<string>());
        }

        // A stale id (record deleted in another tab) makes the repo Update return null —
        // the service must report failure instead of auditing a no-op as a successful update.
        [Fact]
        public async Task UpdateTimeLog_stale_record_reports_not_found_without_audit()
        {
            _timeLogsRepository.Update(Arg.Any<time_log>()).Returns((time_log)null!);

            var result = await CreateService().UpdateTimeLog(TimeLog(), Admin);

            Assert.False(result.Success);
            Assert.Equal("Timelog record not found", result.Message);
            await _auditLogService.DidNotReceive().Log(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<string>());
        }

        #endregion

        #region DeleteTimeLog

        [Fact]
        public async Task DeleteTimeLog_missing_record_is_rejected()
        {
            _timeLogsRepository.GetByAttendanceId(1).Returns((time_log?)null);

            var result = await CreateService().DeleteTimeLog(1, Admin);

            Assert.False(result.Success);
            Assert.Equal("Timelog record not found", result.Message);
            await _timeLogsRepository.DidNotReceive().Delete(Arg.Any<int>());
        }

        [Fact]
        public async Task DeleteTimeLog_hard_delete_audits_with_null_data_to()
        {
            var existing = TimeLog();
            _timeLogsRepository.GetByAttendanceId(1).Returns(existing);

            var result = await CreateService().DeleteTimeLog(1, Admin);

            Assert.True(result.Success);
            Assert.Equal("Timelog deleted successfully", result.Message);
            await _auditLogService.Received(1).Log(
                "timelog", "delete", "1", existing, null, Admin);
        }

        [Fact]
        public async Task DeleteTimeLog_repository_false_reports_failure()
        {
            _timeLogsRepository.GetByAttendanceId(1).Returns(TimeLog());
            _timeLogsRepository.Delete(1).Returns(false);

            var result = await CreateService().DeleteTimeLog(1, Admin);

            Assert.False(result.Success);
            Assert.Equal("Failed to delete timelog", result.Message);
        }

        [Fact]
        public async Task DeleteTimeLog_repository_failure_returns_error_envelope()
        {
            _timeLogsRepository.GetByAttendanceId(1).Returns(TimeLog());
            _timeLogsRepository.Delete(1).ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().DeleteTimeLog(1, Admin);

            Assert.False(result.Success);
            Assert.Equal("Error deleting timelog", result.Message);
        }

        #endregion

        #region GetAllTimeLogs

        [Fact]
        public async Task GetAllTimeLogs_passes_filters_through()
        {
            var from = DateTime.Today.AddDays(-7);
            var to = DateTime.Today;
            var data = new[] { new attendance_log_with_employee() };
            _timeLogsRepository.GetAllFiltered("TST-000001", from, to, HealthConstants.StatusFit).Returns(data);

            var result = await CreateService().GetAllTimeLogs("TST-000001", from, to, HealthConstants.StatusFit);

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        [Fact]
        public async Task GetAllTimeLogs_repository_failure_returns_error_envelope()
        {
            _timeLogsRepository.GetAllFiltered(Arg.Any<string?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<string?>())
                .ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetAllTimeLogs();

            Assert.False(result.Success);
            Assert.Equal("Error retrieving timelogs", result.Message);
        }

        #endregion
    }
}
