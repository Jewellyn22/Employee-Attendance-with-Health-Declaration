using ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Models;
using ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Repositories;
using ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Services;
using ContractorAttendanceWithHealthDeclaration.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ContractorAttendanceWithHealthDeclaration.Tests.Services.Ldap
{
    public class LdapServiceTests
    {
        private readonly ILdapRepository _ldapRepository = Substitute.For<ILdapRepository>();

        private LdapService CreateService() => new(_ldapRepository, NullLogger<LdapService>.Instance);

        [Fact]
        public async Task Login_returns_the_repository_response_unchanged()
        {
            var user = new ldap_user();
            var repositoryResponse = new Response<ldap_user> { Success = true, Data = user };
            _ldapRepository.Login("jsmith", "s3cret").Returns(repositoryResponse);

            var result = await CreateService().Login("jsmith", "s3cret");

            Assert.Same(repositoryResponse, result);
            Assert.Same(user, result.Data);
        }

        [Fact]
        public async Task Login_forwards_credentials_verbatim()
        {
            await CreateService().Login("  jsmith  ", "p@ss word");

            // No trimming/normalization between service and repository.
            await _ldapRepository.Received(1).Login("  jsmith  ", "p@ss word");
        }

        [Fact]
        public async Task Login_repository_failure_returns_error_envelope()
        {
            _ldapRepository.Login(Arg.Any<string>(), Arg.Any<string>())
                .ThrowsAsync(new InvalidOperationException("LDAP unreachable"));

            var result = await CreateService().Login("jsmith", "s3cret");

            Assert.False(result.Success);
            Assert.Equal("LDAP service error", result.Message);
            Assert.Null(result.Data);
        }
    }
}
