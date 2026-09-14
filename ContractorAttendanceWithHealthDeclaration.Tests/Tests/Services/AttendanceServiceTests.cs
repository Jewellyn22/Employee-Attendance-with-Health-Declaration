using ContractorAttendanceWithHealthDeclaration.Helpers;
using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;
using ContractorAttendanceWithHealthDeclaration.Services;
using ContractorAttendanceWithHealthDeclaration.Tests.TestDoubles.Builders;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ContractorAttendanceWithHealthDeclaration.Tests.Services
{
    public class AttendanceServiceTests
    {
        private readonly IContractorEmployeeRepository _contractorRepository = Substitute.For<IContractorEmployeeRepository>();
        private readonly ITimeLogsRepository _timeLogsRepository = Substitute.For<ITimeLogsRepository>();
        private readonly IProviderRepository _providerRepository = Substitute.For<IProviderRepository>();
        private readonly ISystemConfigService _systemConfigService = Substitute.For<ISystemConfigService>();

        private AttendanceService CreateService() => new(
            _contractorRepository, _timeLogsRepository, _providerRepository, _systemConfigService,
            NullLogger<AttendanceService>.Instance);

        public AttendanceServiceTests()
        {
            // Debounce/window defaults used by most tests; individual tests override.
            _systemConfigService.GetDebounceThresholdSeconds()
                .Returns(new Response<double> { Success = true, Data = 30 });
            _systemConfigService.GetHealthDeclarationWindowSeconds()
                .Returns(new Response<double> { Success = true, Data = 120 });
        }

        // Registers Arg.Do capture + pass-through return on the time-log Update call.
        private List<time_log> CaptureUpdates()
        {
            var captured = new List<time_log>();
            _timeLogsRepository.Update(Arg.Do<time_log>(t => captured.Add(t)))
                .Returns(c => c.Arg<time_log>());
            return captured;
        }

        private List<time_log> CaptureCreates()
        {
            var captured = new List<time_log>();
            _timeLogsRepository.Create(Arg.Do<time_log>(t => captured.Add(t)))
                .Returns(c => c.Arg<time_log>());
            return captured;
        }

        private void StubActiveContractor(string employee_id = "TST-000001", string name = "Juan Dela Cruz")
        {
            _contractorRepository.GetByEmployeeId(employee_id)
                .Returns(new ContractorBuilder().WithEmployeeId(employee_id).WithName(name).Build());
        }

        #region ProcessScan

        [Fact]
        public async Task ProcessScan_unknown_contractor_is_rejected()
        {
            _contractorRepository.GetByEmployeeId("TST-999999").Returns((contractor_employee?)null);

            var result = await CreateService().ProcessScan("TST-999999");

            Assert.False(result.Success);
            Assert.Equal("Contractor not found", result.Message);
            Assert.Null(result.Data);
            await _timeLogsRepository.DidNotReceive().Create(Arg.Any<time_log>());
        }

        [Fact]
        public async Task ProcessScan_inactive_contractor_is_rejected()
        {
            _contractorRepository.GetByEmployeeId("TST-000001")
                .Returns(new ContractorBuilder().AsInactive().Build());

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.False(result.Success);
            Assert.Equal("Juan Dela Cruz - Contractor is inactive", result.Message);
        }

        [Fact]
        public async Task ProcessScan_recent_open_session_blocks_as_duplicate_scan()
        {
            StubActiveContractor();
            _timeLogsRepository.GetLastScan("TST-000001").Returns(new time_log
            {
                attendance_id = 1,
                employee_id = "TST-000001",
                time_in = DateTime.Now.AddSeconds(-5),
                time_out = null,
                health_status = HealthConstants.StatusFit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.False(result.Success);
            Assert.Equal("Juan Dela Cruz - Duplicate scan - wait 30 seconds.", result.Message);
        }

        [Fact]
        public async Task ProcessScan_recent_time_out_wins_over_old_time_in_for_debounce()
        {
            // Completed session 5 minutes ago, closed 5 seconds ago: the most recent
            // event (time_out) drives the debounce check.
            StubActiveContractor();
            _timeLogsRepository.GetLastScan("TST-000001").Returns(new time_log
            {
                attendance_id = 1,
                employee_id = "TST-000001",
                time_in = DateTime.Now.AddMinutes(-5),
                time_out = DateTime.Now.AddSeconds(-5),
                health_status = HealthConstants.StatusFit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.False(result.Success);
            Assert.Contains("Duplicate scan", result.Message);
        }

        [Fact]
        public async Task ProcessScan_expired_debounce_allows_new_scan()
        {
            StubActiveContractor();
            _timeLogsRepository.GetLastScan("TST-000001").Returns(new time_log
            {
                attendance_id = 1,
                employee_id = "TST-000001",
                time_in = DateTime.Now.AddMinutes(-10),
                time_out = DateTime.Now.AddSeconds(-60),
                health_status = HealthConstants.StatusFit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });
            _timeLogsRepository.GetTodayTimeIn("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetOpenSession("TST-000001").Returns((time_log?)null);
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            CaptureCreates();

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.True(result.Success);
            await _timeLogsRepository.Received(1).Create(Arg.Any<time_log>());
        }

        [Fact]
        public async Task ProcessScan_today_unfit_time_in_blocks_reentry()
        {
            StubActiveContractor();
            _timeLogsRepository.GetLastScan("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetTodayTimeIn("TST-000001").Returns(new time_log
            {
                attendance_id = 1,
                employee_id = "TST-000001",
                time_in = DateTime.Now.AddHours(-1),
                health_status = HealthConstants.StatusUnfit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.False(result.Success);
            Assert.Equal("Juan Dela Cruz - Not Allowed to Enter. You are UNFIT.", result.Message);
        }

        [Fact]
        public async Task ProcessScan_today_fit_not_understood_time_in_blocks_reentry()
        {
            StubActiveContractor();
            _timeLogsRepository.GetLastScan("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetTodayTimeIn("TST-000001").Returns(new time_log
            {
                attendance_id = 1,
                employee_id = "TST-000001",
                time_in = DateTime.Now.AddHours(-1),
                health_status = HealthConstants.StatusFit,
                waiver_consent = HealthConstants.WaiverNotUnderstood
            });

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.False(result.Success);
            Assert.Equal("Juan Dela Cruz - Not Allowed to Enter. You did not understand waiver.", result.Message);
        }

        [Fact]
        public async Task ProcessScan_today_fit_understood_time_in_does_not_block()
        {
            StubActiveContractor();
            _timeLogsRepository.GetLastScan("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetTodayTimeIn("TST-000001").Returns(new time_log
            {
                attendance_id = 1,
                employee_id = "TST-000001",
                time_in = DateTime.Now.AddHours(-1),
                health_status = HealthConstants.StatusFit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });
            _timeLogsRepository.GetOpenSession("TST-000001").Returns((time_log?)null);
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            CaptureCreates();

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.True(result.Success, result.Message);
        }

        [Fact]
        public async Task ProcessScan_open_session_records_time_out()
        {
            var before = DateTime.Now;
            StubActiveContractor();
            _timeLogsRepository.GetLastScan("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetTodayTimeIn("TST-000001").Returns(new time_log
            {
                attendance_id = 7,
                time_in = DateTime.Now.AddHours(-1),
                health_status = HealthConstants.StatusFit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });
            _timeLogsRepository.GetOpenSession("TST-000001").Returns(new time_log
            {
                attendance_id = 7,
                employee_id = "TST-000001",
                time_in = DateTime.Now.AddHours(-1),
                time_out = null,
                health_status = HealthConstants.StatusFit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });
            var captured = CaptureUpdates();
            var after = DateTime.Now;

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.True(result.Success);
            Assert.Equal("Juan Dela Cruz - SUCCESS TIME OUT", result.Message);
            await _timeLogsRepository.Received(1).Update(Arg.Any<time_log>());
            var updated = captured.Single();
            Assert.Equal(7, updated.attendance_id);
            Assert.Equal("TST-000001", updated.updated_by); // self-service scan
            Assert.NotNull(updated.time_out);
            Assert.InRange(updated.time_out!.Value, before.AddSeconds(-1), after.AddSeconds(1));
            Assert.NotNull(updated.updated_at);
        }

        [Fact]
        public async Task ProcessScan_time_out_skips_project_and_provider_gates()
        {
            // Open session + zero active projects + inactive provider: the contractor
            // must still be able to TIME OUT — gates are only enforced for TIME IN.
            StubActiveContractor();
            _timeLogsRepository.GetLastScan("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetTodayTimeIn("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetOpenSession("TST-000001").Returns(new time_log
            {
                attendance_id = 9,
                employee_id = "TST-000001",
                time_in = DateTime.Now.AddHours(-1),
                health_status = HealthConstants.StatusFit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });
            CaptureUpdates();

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.True(result.Success, result.Message);
            Assert.Contains("SUCCESS TIME OUT", result.Message);
            await _providerRepository.DidNotReceive().GetByProviderCode(Arg.Any<string>());
            await _timeLogsRepository.DidNotReceive().Create(Arg.Any<time_log>());
        }

        [Fact]
        public async Task ProcessScan_zero_active_projects_blocks_time_in()
        {
            // Has assignment rows but none of their projects are active (all expired
            // or deactivated) -> the hydrated active_project_count is 0.
            _contractorRepository.GetByEmployeeId("TST-000001")
                .Returns(new ContractorBuilder().WithActiveProjectCount(0).Build());
            _timeLogsRepository.GetLastScan("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetTodayTimeIn("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetOpenSession("TST-000001").Returns((time_log?)null);

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.False(result.Success);
            Assert.Equal("Juan Dela Cruz - Not Allowed to Enter. Project is In-Active.", result.Message);
            await _providerRepository.DidNotReceive().GetByProviderCode(Arg.Any<string>());
        }

        [Fact]
        public async Task ProcessScan_missing_provider_blocks_time_in()
        {
            StubActiveContractor();
            _timeLogsRepository.GetLastScan("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetTodayTimeIn("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetOpenSession("TST-000001").Returns((time_log?)null);
            _providerRepository.GetByProviderCode("TST").Returns((provider?)null);

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.False(result.Success);
            Assert.Equal("Juan Dela Cruz - Not Allowed to Enter. Provider is In-Active.", result.Message);
        }

        [Fact]
        public async Task ProcessScan_inactive_provider_blocks_time_in()
        {
            StubActiveContractor();
            _timeLogsRepository.GetLastScan("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetTodayTimeIn("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetOpenSession("TST-000001").Returns((time_log?)null);
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 0 });

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.False(result.Success);
            Assert.Equal("Juan Dela Cruz - Not Allowed to Enter. Provider is In-Active.", result.Message);
        }

        [Fact]
        public async Task ProcessScan_happy_path_creates_time_in_with_fit_understood_defaults()
        {
            var before = DateTime.Now;
            StubActiveContractor();
            _timeLogsRepository.GetLastScan("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetTodayTimeIn("TST-000001").Returns((time_log?)null);
            _timeLogsRepository.GetOpenSession("TST-000001").Returns((time_log?)null);
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            var captured = CaptureCreates();
            var after = DateTime.Now;

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.True(result.Success);
            Assert.Equal("Juan Dela Cruz - SUCCESS TIME IN", result.Message);
            await _timeLogsRepository.Received(1).Create(Arg.Any<time_log>());
            var created = captured.Single();
            Assert.Equal("TST-000001", created.employee_id);
            Assert.Equal(HealthConstants.StatusFit, created.health_status);
            Assert.Equal(HealthConstants.WaiverUnderstood, created.waiver_consent);
            Assert.Null(created.time_out);
            Assert.NotNull(created.time_in);
            Assert.InRange(created.time_in!.Value, before.AddSeconds(-1), after.AddSeconds(1));
        }

        [Fact]
        public async Task ProcessScan_repository_failure_returns_error_envelope()
        {
            _contractorRepository.GetByEmployeeId("TST-000001").ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().ProcessScan("TST-000001");

            Assert.False(result.Success);
            Assert.Equal("Error processing scan", result.Message);
            Assert.Null(result.Data);
        }

        #endregion

        #region UpdateHealthStatus

        [Fact]
        public async Task UpdateHealthStatus_invalid_health_status_rejected_without_lookup()
        {
            var result = await CreateService().UpdateHealthStatus(1, "SICK", HealthConstants.WaiverUnderstood);

            Assert.False(result.Success);
            Assert.Equal("Invalid health status. Must be 'FIT' or 'UNFIT'", result.Message);
            Assert.False(result.Data);
            await _timeLogsRepository.DidNotReceive().GetByAttendanceId(Arg.Any<int>());
        }

        [Fact]
        public async Task UpdateHealthStatus_invalid_waiver_consent_rejected_without_lookup()
        {
            var result = await CreateService().UpdateHealthStatus(1, HealthConstants.StatusFit, "maybe");

            Assert.False(result.Success);
            Assert.Equal("Invalid waiver consent. Must be 'UNDERSTOOD' or 'NOT_UNDERSTOOD'", result.Message);
            Assert.False(result.Data);
        }

        [Fact]
        public async Task UpdateHealthStatus_missing_record_is_rejected()
        {
            _timeLogsRepository.GetByAttendanceId(42).Returns((time_log?)null);

            var result = await CreateService().UpdateHealthStatus(42, HealthConstants.StatusFit, HealthConstants.WaiverUnderstood);

            Assert.False(result.Success);
            Assert.Equal("Attendance record not found", result.Message);
            Assert.False(result.Data);
        }

        [Fact]
        public async Task UpdateHealthStatus_expired_window_is_rejected_without_update()
        {
            _timeLogsRepository.GetByAttendanceId(1).Returns(new time_log
            {
                attendance_id = 1,
                time_in = DateTime.Now.AddSeconds(-130) // window = 120s
            });

            var result = await CreateService().UpdateHealthStatus(1, HealthConstants.StatusUnfit, HealthConstants.WaiverUnderstood);

            Assert.False(result.Success);
            Assert.Contains("window has expired", result.Message);
            Assert.False(result.Data);
            await _timeLogsRepository.DidNotReceive().Update(Arg.Any<time_log>());
        }

        [Fact]
        public async Task UpdateHealthStatus_unfit_within_window_sets_auto_time_out()
        {
            var before = DateTime.Now;
            _timeLogsRepository.GetByAttendanceId(1).Returns(new time_log
            {
                attendance_id = 1,
                employee_id = "TST-000001",
                time_in = before.AddSeconds(-5),
                time_out = null,
                health_status = HealthConstants.StatusFit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });
            var captured = CaptureUpdates();
            var after = DateTime.Now;

            var result = await CreateService().UpdateHealthStatus(1, HealthConstants.StatusUnfit, HealthConstants.WaiverUnderstood);

            Assert.True(result.Success);
            Assert.True(result.Data);
            await _timeLogsRepository.Received(1).Update(Arg.Any<time_log>());
            var updated = captured.Single();
            Assert.Equal(HealthConstants.StatusUnfit, updated.health_status);
            Assert.NotNull(updated.time_out);
            Assert.InRange(updated.time_out!.Value, before.AddSeconds(-1), after.AddSeconds(1));
        }

        [Fact]
        public async Task UpdateHealthStatus_fit_not_understood_also_sets_auto_time_out()
        {
            _timeLogsRepository.GetByAttendanceId(1).Returns(new time_log
            {
                attendance_id = 1,
                time_in = DateTime.Now.AddSeconds(-5),
                time_out = null,
                health_status = HealthConstants.StatusFit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });
            var captured = CaptureUpdates();

            var result = await CreateService().UpdateHealthStatus(1, HealthConstants.StatusFit, HealthConstants.WaiverNotUnderstood);

            Assert.True(result.Success);
            await _timeLogsRepository.Received(1).Update(Arg.Any<time_log>());
            Assert.NotNull(captured.Single().time_out);
        }

        [Fact]
        public async Task UpdateHealthStatus_fit_understood_clears_recent_auto_time_out()
        {
            _timeLogsRepository.GetByAttendanceId(1).Returns(new time_log
            {
                attendance_id = 1,
                time_in = DateTime.Now.AddSeconds(-15),
                time_out = DateTime.Now.AddSeconds(-10),
                health_status = HealthConstants.StatusUnfit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });
            var captured = CaptureUpdates();

            var result = await CreateService().UpdateHealthStatus(1, HealthConstants.StatusFit, HealthConstants.WaiverUnderstood);

            Assert.True(result.Success);
            await _timeLogsRepository.Received(1).Update(Arg.Any<time_log>());
            Assert.Null(captured.Single().time_out); // auto TIME OUT reverted
        }

        [Fact]
        public async Task UpdateHealthStatus_fit_understood_keeps_time_out_set_before_the_window()
        {
            var manualTimeOut = DateTime.Now.AddHours(-2);
            _timeLogsRepository.GetByAttendanceId(1).Returns(new time_log
            {
                attendance_id = 1,
                time_in = DateTime.Now.AddSeconds(-10),
                time_out = manualTimeOut,
                health_status = HealthConstants.StatusUnfit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });
            var captured = CaptureUpdates();

            var result = await CreateService().UpdateHealthStatus(1, HealthConstants.StatusFit, HealthConstants.WaiverUnderstood);

            Assert.True(result.Success);
            await _timeLogsRepository.Received(1).Update(Arg.Any<time_log>());
            Assert.Equal(manualTimeOut, captured.Single().time_out); // not reverted
        }

        [Fact]
        public async Task UpdateHealthStatus_fit_understood_with_no_time_out_leaves_it_null()
        {
            _timeLogsRepository.GetByAttendanceId(1).Returns(new time_log
            {
                attendance_id = 1,
                time_in = DateTime.Now.AddSeconds(-10),
                time_out = null,
                health_status = HealthConstants.StatusFit,
                waiver_consent = HealthConstants.WaiverUnderstood
            });
            var captured = CaptureUpdates();

            var result = await CreateService().UpdateHealthStatus(1, HealthConstants.StatusFit, HealthConstants.WaiverUnderstood);

            Assert.True(result.Success);
            await _timeLogsRepository.Received(1).Update(Arg.Any<time_log>());
            Assert.Null(captured.Single().time_out);
        }

        [Fact]
        public async Task UpdateHealthStatus_repository_null_returns_failure()
        {
            _timeLogsRepository.GetByAttendanceId(1).Returns(new time_log
            {
                attendance_id = 1,
                time_in = DateTime.Now.AddSeconds(-5)
            });
            _timeLogsRepository.Update(Arg.Any<time_log>()).Returns((time_log)null!);

            var result = await CreateService().UpdateHealthStatus(1, HealthConstants.StatusFit, HealthConstants.WaiverUnderstood);

            Assert.False(result.Success);
            Assert.Equal("Failed to update health status and waiver consent", result.Message);
        }

        [Fact]
        public async Task UpdateHealthStatus_repository_failure_returns_error_envelope()
        {
            _timeLogsRepository.GetByAttendanceId(1).ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().UpdateHealthStatus(1, HealthConstants.StatusFit, HealthConstants.WaiverUnderstood);

            Assert.False(result.Success);
            Assert.Equal("Error updating health status and waiver consent", result.Message);
            Assert.False(result.Data);
        }

        #endregion

        #region CanChangeHealthStatus

        [Fact]
        public async Task CanChangeHealthStatus_missing_record_returns_false()
        {
            _timeLogsRepository.GetByAttendanceId(1).Returns((time_log?)null);

            var result = await CreateService().CanChangeHealthStatus(1);

            Assert.False(result.Success);
            Assert.Equal("Attendance record not found", result.Message);
            Assert.False(result.Data);
        }

        [Fact]
        public async Task CanChangeHealthStatus_within_window_returns_true()
        {
            _timeLogsRepository.GetByAttendanceId(1).Returns(new time_log
            {
                attendance_id = 1,
                time_in = DateTime.Now.AddSeconds(-10)
            });

            var result = await CreateService().CanChangeHealthStatus(1);

            Assert.True(result.Success);
            Assert.Equal("Health declaration can be changed", result.Message);
            Assert.True(result.Data);
        }

        [Fact]
        public async Task CanChangeHealthStatus_expired_window_returns_false()
        {
            _timeLogsRepository.GetByAttendanceId(1).Returns(new time_log
            {
                attendance_id = 1,
                time_in = DateTime.Now.AddMinutes(-30)
            });

            var result = await CreateService().CanChangeHealthStatus(1);

            Assert.True(result.Success);
            Assert.Equal("Health declaration window expired", result.Message);
            Assert.False(result.Data);
        }

        [Fact]
        public async Task CanChangeHealthStatus_repository_failure_returns_error_envelope()
        {
            _timeLogsRepository.GetByAttendanceId(1).ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().CanChangeHealthStatus(1);

            Assert.False(result.Success);
            Assert.Equal("Error checking eligibility", result.Message);
            Assert.False(result.Data);
        }

        #endregion

        #region Pass-through reads

        [Fact]
        public async Task GetTodayAttendance_passes_repository_data_through()
        {
            var data = new[] { new time_log { attendance_id = 1 }, new time_log { attendance_id = 2 } };
            _timeLogsRepository.GetTodayAttendance().Returns(data);

            var result = await CreateService().GetTodayAttendance();

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        [Fact]
        public async Task GetTodayAttendance_repository_failure_returns_error_envelope()
        {
            _timeLogsRepository.GetTodayAttendance().ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetTodayAttendance();

            Assert.False(result.Success);
            Assert.Equal("Error retrieving attendance", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetByAttendanceId_found_returns_record()
        {
            var record = new time_log { attendance_id = 5 };
            _timeLogsRepository.GetByAttendanceId(5).Returns(record);

            var result = await CreateService().GetByAttendanceId(5);

            Assert.True(result.Success);
            Assert.Same(record, result.Data);
        }

        [Fact]
        public async Task GetByAttendanceId_missing_returns_not_found()
        {
            _timeLogsRepository.GetByAttendanceId(5).Returns((time_log?)null);

            var result = await CreateService().GetByAttendanceId(5);

            Assert.False(result.Success);
            Assert.Equal("Attendance record not found", result.Message);
        }

        [Fact]
        public async Task GetLastScan_passes_repository_data_through()
        {
            var scan = new time_log { attendance_id = 1 };
            _timeLogsRepository.GetLastScan("TST-000001").Returns(scan);

            var result = await CreateService().GetLastScan("TST-000001");

            Assert.True(result.Success);
            Assert.Same(scan, result.Data);
        }

        [Fact]
        public async Task GetLastScan_repository_failure_returns_error_envelope()
        {
            _timeLogsRepository.GetLastScan("TST-000001").ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetLastScan("TST-000001");

            Assert.False(result.Success);
            Assert.Equal("Error retrieving last scan", result.Message);
            Assert.Null(result.Data);
        }

        #endregion
    }
}
