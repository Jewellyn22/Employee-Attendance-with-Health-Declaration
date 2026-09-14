using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;
using ContractorAttendanceWithHealthDeclaration.Tests.Infrastructure;
using MySqlConnector;
using Xunit;

namespace ContractorAttendanceWithHealthDeclaration.Tests.Repositories
{
    [Collection("database")]
    [Trait("Category", "Integration")]
    public class SystemConfigRepositoryIntegrationTests
    {
        private readonly DatabaseCollectionFixture _fixture;

        public SystemConfigRepositoryIntegrationTests(DatabaseCollectionFixture fixture)
        {
            _fixture = fixture;
        }

        // READ-ONLY: touches a real production key but never writes to it.
        [Fact]
        public async Task GetByKey_reads_a_real_config_row()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new SystemConfigRepository(connection);

            var config = await repo.GetByKey("DebounceThresholdSeconds");

            Assert.NotNull(config);
            Assert.Equal("DebounceThresholdSeconds", config!.key);
            Assert.False(string.IsNullOrWhiteSpace(config.value));
        }

        [Fact]
        public async Task GetByKey_missing_returns_null()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new SystemConfigRepository(connection);

            Assert.Null(await repo.GetByKey("TST_ConfigKey_Missing"));
        }

        [Fact]
        public async Task Create_then_GetByKey_round_trips_a_TST_key()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new SystemConfigRepository(connection);

            var created = await repo.Create(new system_config
            {
                key = "TST_ConfigKey_RoundTrip",
                value = "42",
                description = "TST integration test row"
            });

            Assert.NotNull(created);
            Assert.True(created.id > 0);
            var fetched = await repo.GetByKey("TST_ConfigKey_RoundTrip");
            Assert.NotNull(fetched);
            Assert.Equal(created.id, fetched!.id);
            Assert.Equal("42", fetched.value);
            Assert.Equal("TST integration test row", fetched.description);
        }

        [Fact]
        public async Task Update_changes_value_and_description_of_a_TST_key()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new SystemConfigRepository(connection);
            await repo.Create(new system_config
            {
                key = "TST_ConfigKey_Update",
                value = "before",
                description = "before desc"
            });

            var updated = await repo.Update(new system_config
            {
                key = "TST_ConfigKey_Update",
                value = "after",
                description = "after desc"
            });

            Assert.NotNull(updated);
            Assert.Equal("after", updated!.value);
            var fetched = await repo.GetByKey("TST_ConfigKey_Update");
            Assert.Equal("after", fetched!.value);
            Assert.Equal("after desc", fetched.description);
        }

        [Fact]
        public async Task GetAll_includes_the_TST_key()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new SystemConfigRepository(connection);
            await repo.Create(new system_config { key = "TST_ConfigKey_GetAll", value = "1", description = "TST" });

            var all = await repo.GetAll();

            Assert.Contains(all, c => c.key == "TST_ConfigKey_GetAll");
        }

        [Fact]
        public async Task Create_duplicate_key_throws()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new SystemConfigRepository(connection);
            await repo.Create(new system_config { key = "TST_ConfigKey_Dup", value = "1", description = "first" });

            await Assert.ThrowsAsync<MySqlException>(() =>
                repo.Create(new system_config { key = "TST_ConfigKey_Dup", value = "2", description = "second" }));
        }
    }
}
