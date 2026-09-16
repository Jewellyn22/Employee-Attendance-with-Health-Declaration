using EmployeeAttendanceWithHealthDeclaration.Models.Domain;
using EmployeeAttendanceWithHealthDeclaration.Repositories;
using EmployeeAttendanceWithHealthDeclaration.Tests.Infrastructure;
using Dapper;
using Xunit;

namespace EmployeeAttendanceWithHealthDeclaration.Tests.Repositories
{
    [Collection("database")]
    [Trait("Category", "Integration")]
    public class ProjectRepositoryIntegrationTests
    {
        private readonly DatabaseCollectionFixture _fixture;

        public ProjectRepositoryIntegrationTests(DatabaseCollectionFixture fixture)
        {
            _fixture = fixture;
        }

        private static project NewProject(string name) => new()
        {
            project_name = name,
            provider_code = IntegrationSeed.ProviderCode,
            provider_pic = "TST PIC",
            provider_pic_number = "09170000000",
            area_of_destination = "TST Area",
            contract_startdate = DateTime.Today,
            contract_enddate = DateTime.Today.AddYears(1),
            active = 1
        };

        [Fact]
        public async Task Create_auto_generates_provider_scoped_year_sequence()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            var repo = new ProjectRepository(connection);

            var first = await repo.Create(NewProject("TST Seq A"));
            var second = await repo.Create(NewProject("TST Seq B"));

            Assert.NotNull(first?.project_code);
            Assert.NotNull(second?.project_code);
            // Format {ProviderCode}-YY-### scoped per provider per year.
            Assert.Matches($"^{IntegrationSeed.ProviderCode}-\\d{{2}}-\\d{{3}}$", first!.project_code);
            Assert.Matches($"^{IntegrationSeed.ProviderCode}-\\d{{2}}-\\d{{3}}$", second!.project_code);

            var firstNumber = int.Parse(first!.project_code[^3..]);
            var secondNumber = int.Parse(second!.project_code[^3..]);
            Assert.Equal(firstNumber + 1, secondNumber);   // per-provider-per-year sequence increments
            Assert.Equal(first.project_code[..^3], second.project_code[..^3]);   // same provider-year prefix
        }

        [Fact]
        public async Task Create_then_GetByProjectCode_round_trips()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            var repo = new ProjectRepository(connection);

            var created = await repo.Create(NewProject("TST Round Trip"));
            var fetched = await repo.GetByProjectCode(created!.project_code);

            Assert.NotNull(fetched);
            Assert.Equal("TST Round Trip", fetched!.project_name);
            Assert.Equal(IntegrationSeed.ProviderCode, fetched.provider_code);
            Assert.Equal("TST PIC", fetched.provider_pic);
            Assert.Equal("09170000000", fetched.provider_pic_number);
            Assert.Equal("TST Area", fetched.area_of_destination);
            Assert.Equal(DateTime.Today, fetched.contract_startdate);
            Assert.Equal(1, fetched.active);
            Assert.Equal(0, fetched.is_deleted);
        }

        [Fact]
        public async Task GetByProjectCode_missing_returns_null()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new ProjectRepository(connection);

            Assert.Null(await repo.GetByProjectCode("TST-26-999"));
        }

        [Fact]
        public async Task Update_persists_changes()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            var repo = new ProjectRepository(connection);
            var created = await repo.Create(NewProject("TST Before"));

            created!.project_name = "TST After";
            created.area_of_destination = "TST Area 2";
            created.contract_enddate = DateTime.Today.AddYears(2);
            await repo.Update(created);

            var fetched = await repo.GetByProjectCode(created.project_code);
            Assert.Equal("TST After", fetched!.project_name);
            Assert.Equal("TST Area 2", fetched.area_of_destination);
            Assert.Equal(DateTime.Today.AddYears(2), fetched.contract_enddate);
        }

        [Fact]
        public async Task SetInactive_deactivates_the_project()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            var repo = new ProjectRepository(connection);
            var created = await repo.Create(NewProject("TST Inactive"));

            var result = await repo.SetInactive(created!.project_code);

            var fetched = await repo.GetByProjectCode(created.project_code);
            Assert.Equal(0, fetched!.active);
            Assert.True(result);
        }

        [Fact]
        public async Task GetByProviderCode_returns_only_that_providers_projects()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateProviderAsync(connection, provider_code: "TSTP-OTHER");
            var repo = new ProjectRepository(connection);
            var mine = await repo.Create(NewProject("TST Mine"));

            // Provider must be set BEFORE Create: sp_project_Create scopes the
            // auto-generated code to this provider, so the code lands in the
            // TSTP-OTHER-YY-### namespace. Flipping provider AFTER Create (the old
            // approach) orphaned the TST-prefixed code and broke the TST sequence.
            var others = NewProject("TST Theirs");
            others.provider_code = "TSTP-OTHER";
            var othersCreated = await repo.Create(others);

            var list = (await repo.GetByProviderCode(IntegrationSeed.ProviderCode)).ToList();

            Assert.Contains(list, p => p.project_code == mine!.project_code);
            Assert.DoesNotContain(list, p => p.project_code == othersCreated!.project_code);
        }

        [Fact]
        public async Task Delete_soft_deletes_and_cascades_only_to_employees_without_another_active_project()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            // Project A (to delete) and B (the "other active project").
            await IntegrationSeed.CreateProjectAsync(connection, project_code: "TST-26-901");
            await IntegrationSeed.CreateProjectAsync(connection, project_code: "TST-26-902");
            // C1 only on A (cascade target), C2 on A + B (must survive).
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000011");
            await IntegrationSeed.AssignProjectAsync(connection, "TST-000011", "TST-26-901");
            await IntegrationSeed.CreateEmployeeAsync(connection, employee_id: "TST-000012");
            await IntegrationSeed.AssignProjectAsync(connection, "TST-000012", "TST-26-901");
            await IntegrationSeed.AssignProjectAsync(connection, "TST-000012", "TST-26-902");
            var repo = new ProjectRepository(connection);

            var result = await repo.Delete("TST-26-901");

            Assert.NotNull(result);
            Assert.True(result!.success);
            Assert.Equal(1, result.deleted_employees);   // only C1

            // Rows are retained (soft delete), flags set correctly.
            var deletedProject = await connection.QuerySingleAsync<(int is_deleted, int active)>(
                "SELECT is_deleted, active FROM project WHERE project_code = 'TST-26-901'");
            Assert.Equal(1, deletedProject.is_deleted);

            var c1 = await connection.QuerySingleAsync<int>(
                "SELECT is_deleted FROM employee WHERE employee_id = 'TST-000011'");
            var c2 = await connection.QuerySingleAsync<int>(
                "SELECT is_deleted FROM employee WHERE employee_id = 'TST-000012'");
            Assert.Equal(1, c1);   // last active project deleted
            Assert.Equal(0, c2);   // still has another active project
        }

        [Fact]
        public async Task Delete_twice_reports_nothing_the_second_time()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            await IntegrationSeed.CreateProjectAsync(connection, project_code: "TST-26-903");
            var repo = new ProjectRepository(connection);
            await repo.Delete("TST-26-903");

            var second = await repo.Delete("TST-26-903");

            // The SP excludes already-deleted projects, so the second call is a no-op.
            Assert.True(second == null || !second.success || second.deleted_employees == 0,
                "Second delete must not report deleted rows");
        }

        [Fact]
        public async Task GetActiveCount_reflects_created_projects()
        {
            await using var connection = _fixture.CreateConnection();
            await IntegrationSeed.CreateProviderAsync(connection);
            var repo = new ProjectRepository(connection);
            var before = await repo.GetActiveCount();

            var created = await repo.Create(NewProject("TST Count"));

            var after = await repo.GetActiveCount();
            Assert.Equal(before + 1, after);   // delta-based: tolerant of real ACI data
            Assert.Contains(created!.project_code, created.project_code);   // sanity
        }
    }
}
