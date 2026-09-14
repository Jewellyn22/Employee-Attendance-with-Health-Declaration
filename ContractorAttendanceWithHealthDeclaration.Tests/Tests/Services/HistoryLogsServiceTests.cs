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
    public class HistoryLogsServiceTests
    {
        private readonly ITimeLogsRepository _timeLogsRepository = Substitute.For<ITimeLogsRepository>();

        private HistoryLogsService CreateService() => new(
            _timeLogsRepository, NullLogger<HistoryLogsService>.Instance);

        [Fact]
        public async Task GetFilteredAttendance_passes_filters_through()
        {
            var from = DateTime.Today.AddDays(-7);
            var to = DateTime.Today;
            var data = new[] { new attendance_log_with_employee() };
            _timeLogsRepository.GetAllFiltered("TST-000001", from, to, "FIT").Returns(data);

            var result = await CreateService().GetFilteredAttendance("TST-000001", from, to, "FIT");

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        [Fact]
        public async Task GetFilteredAccepts_null_filters()
        {
            var data = new[] { new attendance_log_with_employee() };
            _timeLogsRepository.GetAllFiltered(null, null, null, null).Returns(data);

            var result = await CreateService().GetFilteredAttendance();

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        [Fact]
        public async Task GetFilteredAttendance_repository_failure_returns_error_envelope()
        {
            _timeLogsRepository.GetAllFiltered(Arg.Any<string?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<string?>())
                .ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetFilteredAttendance();

            Assert.False(result.Success);
            Assert.Equal("Error retrieving attendance logs", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetAttendanceForExport_uses_the_same_filtered_query()
        {
            var data = new[] { new attendance_log_with_employee() };
            _timeLogsRepository.GetAllFiltered(null, null, null, null).Returns(data);

            var result = await CreateService().GetAttendanceForExport();

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        [Fact]
        public async Task GetAttendanceForExport_repository_failure_returns_error_envelope()
        {
            _timeLogsRepository.GetAllFiltered(Arg.Any<string?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<string?>())
                .ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetAttendanceForExport();

            Assert.False(result.Success);
            Assert.Equal("Error retrieving export data", result.Message);
        }

        [Fact]
        public async Task GetOpenSessionsCount_passes_through_and_defaults_to_zero_on_failure()
        {
            _timeLogsRepository.GetOpenSessionsCount().Returns(5);
            var ok = await CreateService().GetOpenSessionsCount();
            Assert.True(ok.Success);
            Assert.Equal(5, ok.Data);

            _timeLogsRepository.GetOpenSessionsCount().ThrowsAsync(new InvalidOperationException("db down"));
            var failed = await CreateService().GetOpenSessionsCount();
            Assert.False(failed.Success);
            Assert.Equal(0, failed.Data);
        }

        [Fact]
        public async Task GetRecentForDashboard_passes_repository_data_through()
        {
            var data = new[] { new attendance_log_with_employee() };
            _timeLogsRepository.GetRecentForDashboard().Returns(data);

            var result = await CreateService().GetRecentForDashboard();

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        [Fact]
        public async Task GetRecentForDashboard_repository_failure_returns_error_envelope()
        {
            _timeLogsRepository.GetRecentForDashboard().ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetRecentForDashboard();

            Assert.False(result.Success);
            Assert.Equal("Error retrieving recent time logs", result.Message);
        }

        private static dashboard_daily_stat Stat(DateTime stat_date, string provider_code,
            int total, int fit, int unfit, int understood, int not_understood,
            int fit_understood = 0, int fit_not_understood = 0,
            int unfit_understood = 0, int unfit_not_understood = 0) => new()
        {
            stat_date = stat_date,
            provider_code = provider_code,
            provider_name = "P " + provider_code,
            total_count = total,
            fit_count = fit,
            unfit_count = unfit,
            understood_count = understood,
            not_understood_count = not_understood,
            fit_understood_count = fit_understood,
            fit_not_understood_count = fit_not_understood,
            unfit_understood_count = unfit_understood,
            unfit_not_understood_count = unfit_not_understood
        };

        [Fact]
        public async Task GetDashboardStats_defaults_null_dates_to_last_7_days()
        {
            _timeLogsRepository.GetDailyStatsByProvider(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(Array.Empty<dashboard_daily_stat>());
            _timeLogsRepository.GetDailyStatsByProject(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(Array.Empty<dashboard_daily_stat>());

            var result = await CreateService().GetDashboardStats();

            Assert.True(result.Success);
            _ = _timeLogsRepository.Received().GetDailyStatsByProvider(DateTime.Today.AddDays(-6), DateTime.Today);
            _ = _timeLogsRepository.Received().GetDailyStatsByProject(DateTime.Today.AddDays(-6), DateTime.Today);
        }

        [Fact]
        public async Task GetDashboardStats_passes_explicit_dates_through()
        {
            var from = DateTime.Today.AddDays(-13);
            var to = DateTime.Today;
            _timeLogsRepository.GetDailyStatsByProvider(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(Array.Empty<dashboard_daily_stat>());
            _timeLogsRepository.GetDailyStatsByProject(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(Array.Empty<dashboard_daily_stat>());

            var result = await CreateService().GetDashboardStats(from, to);

            Assert.True(result.Success);
            _ = _timeLogsRepository.Received().GetDailyStatsByProvider(from, to);
            _ = _timeLogsRepository.Received().GetDailyStatsByProject(from, to);
        }

        [Fact]
        public async Task GetDashboardStats_overall_is_summed_from_provider_rows()
        {
            var day1 = DateTime.Today.AddDays(-1);
            var providerRows = new[]
            {
                Stat(day1, "P1", total: 3, fit: 2, unfit: 1, understood: 3, not_understood: 0),
                Stat(day1, "P2", total: 2, fit: 1, unfit: 0, understood: 1, not_understood: 1),
                Stat(DateTime.Today, "P1", total: 1, fit: 1, unfit: 0, understood: 1, not_understood: 0)
            };
            _timeLogsRepository.GetDailyStatsByProvider(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(providerRows);
            _timeLogsRepository.GetDailyStatsByProject(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(Array.Empty<dashboard_daily_stat>());

            var result = await CreateService().GetDashboardStats();

            Assert.True(result.Success);
            var overall = result.Data!.overall;
            Assert.Equal(2, overall.Count);
            Assert.Equal(day1, overall[0].stat_date);
            Assert.Equal(5, overall[0].total_count);
            Assert.Equal(3, overall[0].fit_count);
            Assert.Equal(1, overall[0].unfit_count);
            Assert.Equal(4, overall[0].understood_count);
            Assert.Equal(1, overall[0].not_understood_count);
            Assert.Equal(1, overall[1].total_count);
            // Overall rows are service-computed: no provider/project attribution.
            Assert.Null(overall[0].provider_code);
            Assert.Null(overall[0].project_code);
        }

        [Fact]
        public async Task GetDashboardStats_overall_ignores_project_row_double_counting()
        {
            var today = DateTime.Today;
            // One contractor on two projects: each project row carries the same 5 scans,
            // so summing the PROJECT dimension would give 10 where the truth is 5.
            var providerRows = new[]
            {
                Stat(today, "P1", total: 3, fit: 2, unfit: 1, understood: 3, not_understood: 0),
                Stat(today, "P2", total: 2, fit: 1, unfit: 0, understood: 1, not_understood: 1)
            };
            var projectRows = new[]
            {
                new dashboard_daily_stat { stat_date = today, project_code = "J1", project_name = "Proj 1", total_count = 5, fit_count = 3, unfit_count = 1, understood_count = 4, not_understood_count = 1 },
                new dashboard_daily_stat { stat_date = today, project_code = "J2", project_name = "Proj 2", total_count = 5, fit_count = 3, unfit_count = 1, understood_count = 4, not_understood_count = 1 }
            };
            _timeLogsRepository.GetDailyStatsByProvider(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(providerRows);
            _timeLogsRepository.GetDailyStatsByProject(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(projectRows);

            var result = await CreateService().GetDashboardStats();

            Assert.True(result.Success);
            var overall = Assert.Single(result.Data!.overall);
            Assert.Equal(5, overall.total_count);   // provider sums, not the project double count (10)
        }

        [Fact]
        public async Task GetDashboardStats_overall_sums_the_four_way_cross_counts()
        {
            // The Health Declaration chart needs the health_status x waiver_consent
            // cross-breakdown (FIT/UNFIT x UNDERSTOOD/NOT_UNDERSTOOD), not just marginals.
            var day1 = DateTime.Today.AddDays(-1);
            var providerRows = new[]
            {
                Stat(day1, "P1", total: 5, fit: 3, unfit: 2, understood: 3, not_understood: 2,
                    fit_understood: 2, fit_not_understood: 1, unfit_understood: 1, unfit_not_understood: 1),
                Stat(day1, "P2", total: 3, fit: 2, unfit: 1, understood: 1, not_understood: 2,
                    fit_understood: 1, unfit_not_understood: 2)
            };
            _timeLogsRepository.GetDailyStatsByProvider(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(providerRows);
            _timeLogsRepository.GetDailyStatsByProject(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(Array.Empty<dashboard_daily_stat>());

            var result = await CreateService().GetDashboardStats();

            Assert.True(result.Success);
            var overall = Assert.Single(result.Data!.overall);
            Assert.Equal(3, overall.fit_understood_count);
            Assert.Equal(1, overall.fit_not_understood_count);
            Assert.Equal(1, overall.unfit_understood_count);
            Assert.Equal(3, overall.unfit_not_understood_count);
        }

        [Fact]
        public async Task GetDashboardStats_empty_results_yield_empty_lists()
        {
            _timeLogsRepository.GetDailyStatsByProvider(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(Array.Empty<dashboard_daily_stat>());
            _timeLogsRepository.GetDailyStatsByProject(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(Array.Empty<dashboard_daily_stat>());

            var result = await CreateService().GetDashboardStats();

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data.overall);
            Assert.Empty(result.Data.by_provider);
            Assert.Empty(result.Data.by_project);
        }

        [Fact]
        public async Task GetDashboardStats_rejects_from_after_to()
        {
            var result = await CreateService().GetDashboardStats(DateTime.Today, DateTime.Today.AddDays(-6));

            Assert.False(result.Success);
            Assert.Equal("From date must be on or before To date", result.Message);
            _ = _timeLogsRepository.DidNotReceive().GetDailyStatsByProvider(Arg.Any<DateTime>(), Arg.Any<DateTime>());
            _ = _timeLogsRepository.DidNotReceive().GetDailyStatsByProject(Arg.Any<DateTime>(), Arg.Any<DateTime>());
        }

        [Fact]
        public async Task GetDashboardStats_rejects_ranges_over_one_year()
        {
            var result = await CreateService().GetDashboardStats(DateTime.Today.AddDays(-367), DateTime.Today);

            Assert.False(result.Success);
            Assert.Equal("Date range cannot exceed one year", result.Message);
            _ = _timeLogsRepository.DidNotReceive().GetDailyStatsByProvider(Arg.Any<DateTime>(), Arg.Any<DateTime>());
        }

        [Fact]
        public async Task GetDashboardStats_repository_failure_returns_error_envelope()
        {
            _timeLogsRepository.GetDailyStatsByProvider(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetDashboardStats();

            Assert.False(result.Success);
            Assert.Equal("Error retrieving dashboard stats", result.Message);
            Assert.Null(result.Data);
        }
    }
}
