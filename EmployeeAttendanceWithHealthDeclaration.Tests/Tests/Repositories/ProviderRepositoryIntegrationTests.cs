using EmployeeAttendanceWithHealthDeclaration.Models.Domain;
using EmployeeAttendanceWithHealthDeclaration.Repositories;
using EmployeeAttendanceWithHealthDeclaration.Tests.Infrastructure;
using MySqlConnector;
using Xunit;

namespace EmployeeAttendanceWithHealthDeclaration.Tests.Repositories
{
    // Integration tests against the real employee_attendance database.
    // Schema is read-only; rows are confined to TST-prefixed test data and are
    // removed automatically by the collection fixture (before AND after the run).
    [Collection("database")]
    [Trait("Category", "Integration")]
    public class ProviderRepositoryIntegrationTests
    {
        private readonly DatabaseCollectionFixture _fixture;

        public ProviderRepositoryIntegrationTests(DatabaseCollectionFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Create_then_GetByProviderCode_round_trips()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new ProviderRepository(connection);
            var submitted = new provider
            {
                provider_code = "TSTP-CRUD",
                provider_name = "TST CRUD Provider",
                provider_address = "TST Address 1",
                active = 1
            };

            var created = await repo.Create(submitted);
            var fetched = await repo.GetByProviderCode("TSTP-CRUD");

            Assert.NotNull(created);
            Assert.NotNull(fetched);
            Assert.Equal("TSTP-CRUD", fetched!.provider_code);
            Assert.Equal("TST CRUD Provider", fetched.provider_name);
            Assert.Equal("TST Address 1", fetched.provider_address);
            Assert.Equal(1, fetched.active);
        }

        [Fact]
        public async Task Create_duplicate_provider_code_throws()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new ProviderRepository(connection);
            var submitted = new provider { provider_code = "TSTP-DUP", provider_name = "First", active = 1 };
            await repo.Create(submitted);

            await Assert.ThrowsAsync<MySqlException>(() =>
                repo.Create(new provider { provider_code = "TSTP-DUP", provider_name = "Second", active = 1 }));
        }

        [Fact]
        public async Task GetByProviderCode_missing_returns_null()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new ProviderRepository(connection);

            Assert.Null(await repo.GetByProviderCode("TSTP-NOPE"));
        }

        [Fact]
        public async Task GetAll_includes_the_created_provider()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new ProviderRepository(connection);
            await repo.Create(new provider { provider_code = "TSTP-ALL", provider_name = "TST All Provider", active = 1 });

            var all = await repo.GetAll();

            Assert.Contains(all, p => p.provider_code == "TSTP-ALL");
        }

        [Fact]
        public async Task Update_persists_name_and_active_changes()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new ProviderRepository(connection);
            await repo.Create(new provider { provider_code = "TSTP-UPD", provider_name = "Before", active = 1 });

            var updated = await repo.Update(new provider
            {
                provider_code = "TSTP-UPD",
                provider_name = "After",
                provider_address = "New Address",
                active = 0
            });

            var fetched = await repo.GetByProviderCode("TSTP-UPD");
            Assert.NotNull(fetched);
            Assert.Equal("After", fetched!.provider_name);
            Assert.Equal(0, fetched.active);
            Assert.NotNull(updated);
        }

        [Fact]
        public async Task SetInactive_deactivates_the_provider()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new ProviderRepository(connection);
            await repo.Create(new provider { provider_code = "TSTP-OFF", provider_name = "TST Off", active = 1 });

            var result = await repo.SetInactive("TSTP-OFF");

            var fetched = await repo.GetByProviderCode("TSTP-OFF");
            Assert.Equal(0, fetched!.active);
            Assert.True(result);
        }
    }
}
