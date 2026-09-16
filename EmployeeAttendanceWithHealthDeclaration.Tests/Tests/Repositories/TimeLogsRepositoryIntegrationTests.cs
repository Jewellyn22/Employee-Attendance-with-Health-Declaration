using EmployeeAttendanceWithHealthDeclaration.Models.Domain;
using EmployeeAttendanceWithHealthDeclaration.Repositories;
using EmployeeAttendanceWithHealthDeclaration.Tests.Infrastructure;
using Dapper;
using Xunit;

namespace EmployeeAttendanceWithHealthDeclaration.Tests.Repositories
{
    [Collection("database")]
    [Trait("Category", "Integration")]
    public class TimeLogsRepositoryIntegrationTests
    {
        private readonly DatabaseCollectionFixture _fixture;

        public TimeLogsRepositoryIntegrationTests(DatabaseCollectionFixture fixture)
        {
            _fixture = fixture;
        }

        private static time_log NewLog(string employee_id, DateTime? time_in = null, DateTime? time_out = null,
            string health_status = "FIT", string waiver_consent = "UNDERSTOOD") => new()
        {
            employee_id = employee_id,
            time_in = time_in ?? DateTime.Now.AddMinutes(-5),
            time_out = time_out,
            health_status = health_status,
            waiver_consent = waiver_consent
        };

        [Fact]
        public async Task Create_returns_the_inserted_row_with_id_and_created_at()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000041");
            var repo = new TimeLogsRepository(connection);

            var created = await repo.Create(NewLog("TST-000041"));

            Assert.True(created.attendance_id > 0);
            Assert.Equal("TST-000041", created.employee_id);
            Assert.Equal("FIT", created.health_status);
            Assert.Equal("UNDERSTOOD", created.waiver_consent);
            Assert.Null(created.time_out);
            // sp_time_logs_Create stamps created_at with NOW().
            Assert.InRange(created.created_at, DateTime.Now.AddSeconds(-5), DateTime.Now.AddSeconds(5));

            var fetched = await repo.GetByAttendanceId(created.attendance_id);
            Assert.Equal(created.attendance_id, fetched!.attendance_id);
        }

        // PIN: GetLastScan orders by COALESCE(time_out, time_in) — a recent TIME OUT
        // must win over an older TIME IN, even though its time_in is older.
        [Fact]
        public async Task GetLastScan_prefers_the_most_recent_time_out_over_an_older_time_in()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000042");
            var repo = new TimeLogsRepository(connection);
            // Open session TIME IN 10 minutes ago -> COALESCE = -10 min.
            var open = await repo.Create(NewLog("TST-000042", time_in: DateTime.Now.AddMinutes(-10)));
            // Closed session TIME IN 60 min ago, TIME OUT 5 min ago -> COALESCE = -5 min.
            var closed = await repo.Create(NewLog("TST-000042",
                time_in: DateTime.Now.AddMinutes(-60), time_out: DateTime.Now.AddMinutes(-5)));

            var last = await repo.GetLastScan("TST-000042");

            Assert.NotNull(last);
            Assert.Equal(closed.attendance_id, last!.attendance_id);
        }

        [Fact]
        public async Task GetOpenSession_returns_only_the_row_with_no_time_out()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000043");
            var repo = new TimeLogsRepository(connection);
            await repo.Create(NewLog("TST-000043",
                time_in: DateTime.Now.AddHours(-2), time_out: DateTime.Now.AddHours(-1)));
            var open = await repo.Create(NewLog("TST-000043", time_in: DateTime.Now.AddMinutes(-1)));

            var session = await repo.GetOpenSession("TST-000043");

            Assert.NotNull(session);
            Assert.Equal(open.attendance_id, session!.attendance_id);
        }

        [Fact]
        public async Task GetTodayTimeIn_ignores_yesterdays_open_session()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000044");
            var repo = new TimeLogsRepository(connection);
            // Left open since yesterday — must not count as today's TIME IN.
            await IntegrationSeed.CreateTimeLogAsync(connection, "TST-000044",
                time_in: DateTime.Now.AddDays(-1), time_out: null);

            Assert.Null(await repo.GetTodayTimeIn("TST-000044"));

            var today = await repo.Create(NewLog("TST-000044", time_in: DateTime.Now.AddMinutes(-2)));
            var fetched = await repo.GetTodayTimeIn("TST-000044");
            Assert.Equal(today.attendance_id, fetched!.attendance_id);
        }

        [Fact]
        public async Task Update_persists_every_written_column()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000045");
            var repo = new TimeLogsRepository(connection);
            var created = await repo.Create(NewLog("TST-000045"));

            created.time_out = DateTime.Now;
            created.health_status = "UNFIT";
            created.waiver_consent = "NOT_UNDERSTOOD";
            created.updated_by = "TST-ADMIN";
            created.updated_at = DateTime.Now;
            await repo.Update(created);

            var fetched = await repo.GetByAttendanceId(created.attendance_id);
            Assert.NotNull(fetched!.time_out);
            Assert.Equal("UNFIT", fetched.health_status);
            Assert.Equal("NOT_UNDERSTOOD", fetched.waiver_consent);
            Assert.Equal("TST-ADMIN", fetched.updated_by);
            Assert.NotNull(fetched.updated_at);
        }

        // PIN: sp_time_logs_UpdateHealthStatus touches ONLY health_status —
        // the kiosk self-service path must not stamp updated_by/updated_at or
        // alter the auto time_out written by the admin edit flow.
        [Fact]
        public async Task UpdateHealthStatus_changes_only_the_health_column()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000046");
            var repo = new TimeLogsRepository(connection);
            var created = await repo.Create(NewLog("TST-000046", time_out: DateTime.Now.AddMinutes(-1)));
            await connection.ExecuteAsync(
                "UPDATE time_logs SET updated_by = 'TST-ADMIN', updated_at = @ts WHERE attendance_id = @id",
                new { ts = DateTime.Now.AddMinutes(-1), id = created.attendance_id });
            var before = await repo.GetByAttendanceId(created.attendance_id);

            var result = await repo.UpdateHealthStatus(created.attendance_id, "UNFIT");

            Assert.True(result);
            var after = await repo.GetByAttendanceId(created.attendance_id);
            Assert.Equal("UNFIT", after!.health_status);
            Assert.Equal(before!.time_out, after.time_out);                 // untouched
            Assert.Equal("TST-ADMIN", after.updated_by);                    // untouched
            Assert.Equal(before.updated_at, after.updated_at);              // untouched
            Assert.Equal(before.waiver_consent, after.waiver_consent);      // untouched
        }

        [Fact]
        public async Task Delete_removes_the_row_and_reports_what_it_deleted()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000047");
            var repo = new TimeLogsRepository(connection);
            var created = await repo.Create(NewLog("TST-000047"));

            Assert.True(await repo.Delete(created.attendance_id));
            Assert.Null(await repo.GetByAttendanceId(created.attendance_id));
            Assert.False(await repo.Delete(created.attendance_id));   // second delete: nothing to remove
        }

        [Fact]
        public async Task GetAllFiltered_scopes_by_employee_health_and_date()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000048");
            var repo = new TimeLogsRepository(connection);
            var yesterday = await IntegrationSeed.CreateTimeLogAsync(connection, "TST-000048",
                time_in: DateTime.Now.AddDays(-1), health_status: "UNFIT", waiver_consent: "NOT_UNDERSTOOD");
            var today = (await repo.Create(NewLog("TST-000048"))).attendance_id;

            var all = (await repo.GetAllFiltered(employee_id: "TST-000048")).ToList();
            Assert.Equal(2, all.Count);
            Assert.Contains(all, r => r.attendance_id == yesterday);
            Assert.Contains(all, r => r.attendance_id == today);

            var unfitOnly = (await repo.GetAllFiltered(employee_id: "TST-000048", health_status: "UNFIT")).ToList();
            Assert.Single(unfitOnly, r => r.attendance_id == yesterday);

            var todayOnly = (await repo.GetAllFiltered(employee_id: "TST-000048", from_date: DateTime.Today)).ToList();
            Assert.Single(todayOnly, r => r.attendance_id == today);

            // Joined employee fields are hydrated.
            Assert.All(all, r => Assert.Equal("TST Employee One", r.name));
        }

        // PIN: the SP normalizes '' to NULL (NULLIF), so an empty-string filter
        // behaves like "no filter" instead of matching nothing.
        [Fact]
        public async Task GetAllFiltered_empty_string_filter_behaves_like_no_filter()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000049");
            var repo = new TimeLogsRepository(connection);
            var created = await repo.Create(NewLog("TST-000049"));

            var withEmptyFilter = (await repo.GetAllFiltered(employee_id: "")).ToList();

            Assert.Contains(withEmptyFilter, r => r.attendance_id == created.attendance_id);
        }

        [Fact]
        public async Task GetOpenSessionsCount_increases_by_one_for_a_new_open_session()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000050");
            var repo = new TimeLogsRepository(connection);
            var before = await repo.GetOpenSessionsCount();

            var created = await repo.Create(NewLog("TST-000050"));   // no time_out -> open

            Assert.Equal(before + 1, await repo.GetOpenSessionsCount());
            await repo.Delete(created.attendance_id);
            Assert.Equal(before, await repo.GetOpenSessionsCount());   // closed set shrinks back
        }

        [Fact]
        public async Task GetTodayAttendance_and_GetRecentForDashboard_include_todays_scan()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000051");
            var repo = new TimeLogsRepository(connection);
            var created = await repo.Create(NewLog("TST-000051", time_in: DateTime.Now));

            var today = (await repo.GetTodayAttendance()).ToList();
            Assert.Contains(today, t => t.attendance_id == created.attendance_id);

            var recent = (await repo.GetRecentForDashboard()).ToList();
            var row = Assert.Single(recent, r => r.attendance_id == created.attendance_id);
            Assert.Equal("TST-000051", row.employee_id);
            Assert.Equal("TST Employee One", row.name);
        }

        [Fact]
        public async Task GetByEmployeeId_returns_that_employees_logs()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000052");
            var repo = new TimeLogsRepository(connection);
            var first = await repo.Create(NewLog("TST-000052"));
            var second = await repo.Create(NewLog("TST-000052", time_in: DateTime.Now.AddMinutes(-30)));

            var logs = (await repo.GetByEmployeeId("TST-000052")).ToList();

            Assert.Equal(2, logs.Count);
            Assert.Contains(logs, l => l.attendance_id == first.attendance_id);
            Assert.Contains(logs, l => l.attendance_id == second.attendance_id);
        }

        // Dashboard daily stats. These use dedicated TST-DAx providers / TST-00006x
        // employees so other tests' rows under the shared TST provider can't
        // inflate the grouped counts.

        [Fact]
        public async Task GetDailyStatsByProvider_groups_counts_per_day_and_provider()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection, provider_code: "TST-DA", provider_name: "TST Dash A");
            await IntegrationSeed.CreateProviderAsync(connection, provider_code: "TST-DB", provider_name: "TST Dash B");
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000060", provider_code: "TST-DA");
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000061", provider_code: "TST-DB");
            var repo = new TimeLogsRepository(connection);

            // Today: TST-DA has 1 FIT/UNDERSTOOD + 1 UNFIT/NOT_UNDERSTOOD; TST-DB has 1 FIT/NOT_UNDERSTOOD.
            await repo.Create(NewLog("TST-000060"));
            await repo.Create(NewLog("TST-000060", health_status: "UNFIT", waiver_consent: "NOT_UNDERSTOOD"));
            await repo.Create(NewLog("TST-000061", waiver_consent: "NOT_UNDERSTOOD"));
            // Yesterday: TST-DA has 1 more scan.
            await repo.Create(NewLog("TST-000060", time_in: DateTime.Now.AddDays(-1)));
            // 8 days back: outside every default window.
            await IntegrationSeed.CreateTimeLogAsync(connection, "TST-000060", time_in: DateTime.Now.AddDays(-8));

            var stats = (await repo.GetDailyStatsByProvider(DateTime.Today.AddDays(-6), DateTime.Today)).ToList();
            var mine = stats.Where(r => r.provider_code is "TST-DA" or "TST-DB").ToList();

            Assert.Equal(3, mine.Count);

            var daToday = Assert.Single(mine, r => r.provider_code == "TST-DA" && r.stat_date.Date == DateTime.Today);
            Assert.Equal(2, daToday.total_count);
            Assert.Equal(1, daToday.fit_count);
            Assert.Equal(1, daToday.unfit_count);
            Assert.Equal(1, daToday.understood_count);
            Assert.Equal(1, daToday.not_understood_count);
            // Cross-breakdown: 1 FIT/UNDERSTOOD + 1 UNFIT/NOT_UNDERSTOOD.
            Assert.Equal(1, daToday.fit_understood_count);
            Assert.Equal(0, daToday.fit_not_understood_count);
            Assert.Equal(0, daToday.unfit_understood_count);
            Assert.Equal(1, daToday.unfit_not_understood_count);
            Assert.Equal("TST Dash A", daToday.provider_name);

            var dbToday = Assert.Single(mine, r => r.provider_code == "TST-DB" && r.stat_date.Date == DateTime.Today);
            Assert.Equal(1, dbToday.total_count);
            Assert.Equal(0, dbToday.unfit_count);
            Assert.Equal(0, dbToday.understood_count);
            Assert.Equal(1, dbToday.not_understood_count);
            Assert.Equal(0, dbToday.fit_understood_count);
            Assert.Equal(1, dbToday.fit_not_understood_count);

            var daYesterday = Assert.Single(mine, r => r.provider_code == "TST-DA" && r.stat_date.Date == DateTime.Today.AddDays(-1));
            Assert.Equal(1, daYesterday.total_count);

            // The 8-day-old scan is outside the window: no third TST-DA day.
            Assert.DoesNotContain(mine, r => r.stat_date.Date == DateTime.Today.AddDays(-8));
        }

        [Fact]
        public async Task GetDailyStatsByProvider_cross_counts_cover_all_four_combos()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection, provider_code: "TST-DD", provider_name: "TST Dash D");
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000065", provider_code: "TST-DD");
            var repo = new TimeLogsRepository(connection);

            // One log per health_status x waiver_consent combo.
            await repo.Create(NewLog("TST-000065"));
            await repo.Create(NewLog("TST-000065", waiver_consent: "NOT_UNDERSTOOD"));
            await repo.Create(NewLog("TST-000065", health_status: "UNFIT"));
            await repo.Create(NewLog("TST-000065", health_status: "UNFIT", waiver_consent: "NOT_UNDERSTOOD"));

            var stats = (await repo.GetDailyStatsByProvider(DateTime.Today.AddDays(-6), DateTime.Today)).ToList();

            var row = Assert.Single(stats, r => r.provider_code == "TST-DD");
            Assert.Equal(4, row.total_count);
            Assert.Equal(1, row.fit_understood_count);
            Assert.Equal(1, row.fit_not_understood_count);
            Assert.Equal(1, row.unfit_understood_count);
            Assert.Equal(1, row.unfit_not_understood_count);
        }

        [Fact]
        public async Task GetDailyStatsByProvider_excludes_rows_outside_the_window()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection, provider_code: "TST-DC", provider_name: "TST Dash C");
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000062", provider_code: "TST-DC");
            var repo = new TimeLogsRepository(connection);
            var old = await repo.Create(NewLog("TST-000062", time_in: DateTime.Now.AddDays(-8)));

            var stats = (await repo.GetDailyStatsByProvider(DateTime.Today.AddDays(-6), DateTime.Today)).ToList();

            Assert.DoesNotContain(stats, r => r.provider_code == "TST-DC");
        }

        // PIN: employee_project is many-to-many — one TIME IN from a employee
        // assigned to N projects is counted under ALL N projects.
        [Fact]
        public async Task GetDailyStatsByProject_counts_a_employee_under_each_assigned_project()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateProjectAsync(connection);
            await IntegrationSeed.CreateProjectAsync(connection, project_code: IntegrationSeed.SecondProjectCode);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000063");
            await IntegrationSeed.AssignProjectAsync(connection, employee_id: "TST-000063", project_code: IntegrationSeed.ProjectCode);
            await IntegrationSeed.AssignProjectAsync(connection, employee_id: "TST-000063", project_code: IntegrationSeed.SecondProjectCode);
            var repo = new TimeLogsRepository(connection);
            await repo.Create(NewLog("TST-000063"));

            var stats = (await repo.GetDailyStatsByProject(DateTime.Today.AddDays(-6), DateTime.Today)).ToList();

            // One scan, but a row for each of the employee's two projects.
            foreach (var code in new[] { IntegrationSeed.ProjectCode, IntegrationSeed.SecondProjectCode })
            {
                var row = Assert.Single(stats, r => r.project_code == code && r.stat_date.Date == DateTime.Today);
                Assert.Equal(1, row.total_count);
                Assert.Equal(1, row.fit_count);
                // The project SP exposes the same cross-count columns as the provider SP.
                Assert.Equal(1, row.fit_understood_count);
            }
        }

        [Fact]
        public async Task GetDailyStatsByProject_excludes_soft_deleted_projects()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateProjectAsync(connection, project_code: "TST-26-009", is_deleted: 1);
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000064");
            await IntegrationSeed.AssignProjectAsync(connection, employee_id: "TST-000064", project_code: "TST-26-009");
            var repo = new TimeLogsRepository(connection);
            await repo.Create(NewLog("TST-000064"));

            var stats = (await repo.GetDailyStatsByProject(DateTime.Today.AddDays(-6), DateTime.Today)).ToList();

            Assert.DoesNotContain(stats, r => r.project_code == "TST-26-009");
        }
    }
}
