using EmployeeAttendanceWithHealthDeclaration.Models;
using EmployeeAttendanceWithHealthDeclaration.Models.Domain;
using EmployeeAttendanceWithHealthDeclaration.Repositories;
using EmployeeAttendanceWithHealthDeclaration.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace EmployeeAttendanceWithHealthDeclaration.Tests.Services
{
    public class AuditLogServiceTests
    {
        private readonly IAuditLogRepository _repository = Substitute.For<IAuditLogRepository>();

        private AuditLogService CreateService() => new(_repository, NullLogger<AuditLogService>.Instance);

        [Fact]
        public async Task Log_persists_an_entry_with_all_fields_set()
        {
            audit_log? captured = null;
            _repository.Create(Arg.Do<audit_log>(e => captured = e))
                .Returns(c => c.Arg<audit_log>());

            var result = await CreateService().Log("employee", "create", "TST-000001", null, new { active = 1 }, "ADMIN-1");

            Assert.True(result.Success);
            Assert.NotNull(captured);
            Assert.Equal("employee", captured!.entity_type);
            Assert.Equal("create", captured.action);
            Assert.Equal("TST-000001", captured.reference_id);
            Assert.Equal("ADMIN-1", captured.updated_by);
            Assert.Null(captured.data_from);   // pure create: no previous state
            Assert.NotNull(captured.data_to);
        }

        [Fact]
        public async Task Log_serializes_payloads_as_indented_snake_case_json()
        {
            audit_log? captured = null;
            _repository.Create(Arg.Do<audit_log>(e => captured = e))
                .Returns(c => c.Arg<audit_log>());
            var payload = new { EmployeeId = "TST-000001", Active = 1 };

            await CreateService().Log("employee", "update", "TST-000001", payload, payload, "ADMIN-1");

            // snake_case keys + WriteIndented (newlines) so the DB stays human-readable.
            var dataFrom = captured!.data_from;
            Assert.NotNull(dataFrom);
            Assert.Contains("\"employee_id\": \"TST-000001\"", dataFrom);
            Assert.Contains("\"active\": 1", dataFrom);
            Assert.Contains('\n', dataFrom);
        }

        [Fact]
        public async Task Log_blank_updated_by_is_recorded_as_system()
        {
            audit_log? captured = null;
            _repository.Create(Arg.Do<audit_log>(e => captured = e))
                .Returns(c => c.Arg<audit_log>());

            await CreateService().Log("employee", "create", "TST-000001", null, null, "  ");

            Assert.Equal("System", captured!.updated_by);
        }

        [Fact]
        public async Task Log_non_serializable_payload_fails_gracefully_instead_of_throwing()
        {
            // A self-referencing structure cannot be serialized to JSON.
            var cyclic = new Dictionary<string, object>();
            cyclic["self"] = cyclic;

            var result = await CreateService().Log("employee", "update", "TST-000001", cyclic, null, "ADMIN-1");

            Assert.False(result.Success);
            Assert.Equal("Error writing audit log", result.Message);
        }

        [Fact]
        public async Task Log_repository_failure_returns_failure_envelope_without_throwing()
        {
            // Logging must never break the calling operation.
            _repository.Create(Arg.Any<audit_log>()).ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().Log("employee", "create", "TST-000001", null, null, "ADMIN-1");

            Assert.False(result.Success);
            Assert.Equal("Error writing audit log", result.Message);
        }

        [Fact]
        public async Task GetAll_passes_repository_data_through()
        {
            var data = new[] { new audit_log { log_id = 1 } };
            _repository.GetAll().Returns(data);

            var result = await CreateService().GetAll();

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        [Fact]
        public async Task GetAll_repository_failure_returns_error_envelope()
        {
            _repository.GetAll().ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetAll();

            Assert.False(result.Success);
            Assert.Equal("Error retrieving audit logs", result.Message);
        }

        [Fact]
        public async Task GetByEntity_forwards_entity_and_action()
        {
            var data = new[] { new audit_log { log_id = 1 } };
            _repository.GetByEntity("employee", "deactivate_by_project").Returns(data);

            var result = await CreateService().GetByEntity("employee", "deactivate_by_project");

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        [Fact]
        public async Task GetByEntity_repository_failure_returns_error_envelope()
        {
            _repository.GetByEntity(Arg.Any<string>(), Arg.Any<string>())
                .ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetByEntity("employee", "update");

            Assert.False(result.Success);
            Assert.Equal("Error retrieving audit logs", result.Message);
        }
    }
}
