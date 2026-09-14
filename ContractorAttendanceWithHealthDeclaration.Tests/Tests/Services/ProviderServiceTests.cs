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
    public class ProviderServiceTests
    {
        private const string Admin = "ADMIN-1";

        private readonly IProviderRepository _providerRepository = Substitute.For<IProviderRepository>();
        private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();

        private ProviderService CreateService() => new(
            _providerRepository, _auditLogService, NullLogger<ProviderService>.Instance);

        private static provider Provider(string code = "TST") =>
            new() { provider_code = code, provider_name = "Test Provider Co", active = 1 };

        [Fact]
        public async Task GetByProviderCode_found_returns_provider()
        {
            var provider = Provider();
            _providerRepository.GetByProviderCode("TST").Returns(provider);

            var result = await CreateService().GetByProviderCode("TST");

            Assert.True(result.Success);
            Assert.Same(provider, result.Data);
        }

        [Fact]
        public async Task GetByProviderCode_missing_returns_not_found()
        {
            _providerRepository.GetByProviderCode("TST").Returns((provider?)null);

            var result = await CreateService().GetByProviderCode("TST");

            Assert.False(result.Success);
            Assert.Equal("Provider not found", result.Message);
        }

        [Fact]
        public async Task GetByProviderCode_repository_failure_returns_error_envelope()
        {
            _providerRepository.GetByProviderCode("TST").ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetByProviderCode("TST");

            Assert.False(result.Success);
            Assert.Equal("Error retrieving provider", result.Message);
        }

        [Fact]
        public async Task GetAll_passes_repository_data_through()
        {
            var data = new[] { Provider() };
            _providerRepository.GetAll().Returns(data);

            var result = await CreateService().GetAll();

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        [Fact]
        public async Task GetAll_repository_failure_returns_error_envelope()
        {
            _providerRepository.GetAll().ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetAll();

            Assert.False(result.Success);
            Assert.Equal("Error retrieving providers", result.Message);
        }

        [Fact]
        public async Task Create_duplicate_code_is_rejected()
        {
            _providerRepository.GetByProviderCode("TST").Returns(Provider());

            var result = await CreateService().Create(Provider(), Admin);

            Assert.False(result.Success);
            Assert.Equal("Provider with this Provider Code already exists", result.Message);
            await _providerRepository.DidNotReceive().Create(Arg.Any<provider>());
        }

        [Fact]
        public async Task Create_happy_path_audits_create()
        {
            var submitted = Provider();
            _providerRepository.Create(Arg.Any<provider>()).Returns(c => c.Arg<provider>());

            var result = await CreateService().Create(submitted, Admin);

            Assert.True(result.Success, result.Message);
            await _auditLogService.Received(1).Log(
                "provider", "create", "TST",
                Arg.Is<object?>(x => x == null),
                Arg.Is<object?>(x => ReferenceEquals(x, submitted)),
                Admin);
        }

        // A null repository result (SP matched nothing) must be reported as a failure.
        [Fact]
        public async Task Create_repository_null_reports_failure_without_audit()
        {
            _providerRepository.Create(Arg.Any<provider>()).Returns((provider)null!);

            var result = await CreateService().Create(Provider(), Admin);

            Assert.False(result.Success);
            Assert.Equal("Error creating provider", result.Message);
            Assert.Null(result.Data);
            await _auditLogService.DidNotReceive().Log(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<string>());
        }

        [Fact]
        public async Task Create_repository_failure_returns_error_envelope()
        {
            _providerRepository.Create(Arg.Any<provider>()).ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().Create(Provider(), Admin);

            Assert.False(result.Success);
            Assert.Equal("Error creating provider", result.Message);
        }

        [Fact]
        public async Task Update_unknown_provider_is_rejected()
        {
            _providerRepository.GetByProviderCode("TST").Returns((provider?)null);

            var result = await CreateService().Update(Provider(), Admin);

            Assert.False(result.Success);
            Assert.Equal("Provider not found", result.Message);
        }

        [Fact]
        public async Task Update_happy_path_saves_and_audits_the_change()
        {
            var existing = Provider();
            var submitted = Provider();
            _providerRepository.GetByProviderCode("TST").Returns(existing);
            _providerRepository.Update(Arg.Any<provider>()).Returns(c => c.Arg<provider>());

            var result = await CreateService().Update(submitted, Admin);

            Assert.True(result.Success, result.Message);
            Assert.Same(submitted, result.Data);
            await _auditLogService.Received(1).Log(
                "provider", "update", "TST",
                Arg.Is<object?>(x => ReferenceEquals(x, existing)),
                Arg.Is<object?>(x => ReferenceEquals(x, submitted)),
                Admin);
        }

        // A null repository result (SP matched nothing) must be reported as a failure.
        [Fact]
        public async Task Update_repository_null_reports_failure_without_audit()
        {
            _providerRepository.GetByProviderCode("TST").Returns(Provider());
            _providerRepository.Update(Arg.Any<provider>()).Returns((provider)null!);

            var result = await CreateService().Update(Provider(), Admin);

            Assert.False(result.Success);
            Assert.Equal("Error updating provider", result.Message);
            Assert.Null(result.Data);
            await _auditLogService.DidNotReceive().Log(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<string>());
        }

        [Fact]
        public async Task Update_repository_failure_returns_error_envelope()
        {
            _providerRepository.GetByProviderCode("TST").Returns(Provider());
            _providerRepository.Update(Arg.Any<provider>()).ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().Update(Provider(), Admin);

            Assert.False(result.Success);
            Assert.Equal("Error updating provider", result.Message);
        }

        [Fact]
        public async Task SetInactive_passes_repository_result_through()
        {
            _providerRepository.SetInactive("TST").Returns(false);

            var result = await CreateService().SetInactive("TST");

            Assert.True(result.Success);
            Assert.False(result.Data);
        }

        [Fact]
        public async Task SetInactive_repository_failure_returns_error_envelope()
        {
            _providerRepository.SetInactive("TST").ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().SetInactive("TST");

            Assert.False(result.Success);
            Assert.Equal("Error setting provider inactive", result.Message);
        }
    }
}
