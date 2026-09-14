using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;
using ContractorAttendanceWithHealthDeclaration.Tests.Infrastructure;
using Xunit;

namespace ContractorAttendanceWithHealthDeclaration.Tests.Repositories
{
    [Collection("database")]
    [Trait("Category", "Integration")]
    public class AuditLogRepositoryIntegrationTests
    {
        private readonly DatabaseCollectionFixture _fixture;

        public AuditLogRepositoryIntegrationTests(DatabaseCollectionFixture fixture)
        {
            _fixture = fixture;
        }

        private static audit_log NewEntry(string reference_id, string? data_from, string? data_to,
            string entity_type = "TST_integration", string action = "update") => new()
        {
            entity_type = entity_type,
            action = action,
            reference_id = reference_id,
            data_from = data_from,
            data_to = data_to,
            updated_by = "TST-ADMIN"
        };

        // Multiline indented JSON (AuditLogService.WriteIndented format) must
        // round-trip exactly through the JSON column.
        [Fact]
        public async Task Create_round_trips_multiline_json_and_null_data_from()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new AuditLogRepository(connection);
            const string dataTo = "{\n  \"employee_id\": \"TST-000001\",\n  \"name\": \"TST Contractor One\"\n}";

            var created = await repo.Create(NewEntry("TST-000001", data_from: null, data_to: dataTo));

            Assert.True(created.log_id > 0);
            Assert.Equal(dataTo, created.data_to);       // exact string, newlines included
            Assert.Null(created.data_from);              // creates carry a null previous state
            Assert.Equal("TST-ADMIN", created.updated_by);
            Assert.InRange(created.created_at, DateTime.Now.AddSeconds(-5), DateTime.Now.AddSeconds(5));

            var fetched = (await repo.GetByEntity("TST_integration", "update"))
                .Single(e => e.log_id == created.log_id);
            Assert.Equal(dataTo, fetched.data_to);
            Assert.Null(fetched.data_from);
        }

        [Fact]
        public async Task GetByEntity_filters_by_type_and_action_in_ascending_log_id_order()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new AuditLogRepository(connection);
            var first = await repo.Create(NewEntry("TST-order", null, "{}"));
            var second = await repo.Create(NewEntry("TST-order", "{}", "{}"));
            await repo.Create(NewEntry("TST-order", null, "{}", action: "create"));   // different action

            var entries = (await repo.GetByEntity("TST_integration", "update")).ToList();
            var tstEntries = entries.Where(e => e.reference_id == "TST-order").ToList();

            Assert.Equal(new[] { first.log_id, second.log_id }, tstEntries.Select(e => e.log_id));
        }

        [Fact]
        public async Task GetAll_contains_the_TST_entry()
        {
            await using var connection = _fixture.CreateConnection();
            var repo = new AuditLogRepository(connection);
            var created = await repo.Create(NewEntry("TST-getall", null, "{}"));

            var all = await repo.GetAll();

            Assert.Contains(all, e => e.log_id == created.log_id);
        }
    }
}
