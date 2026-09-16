using EmployeeAttendanceWithHealthDeclaration.ExternalApis.Ldap.Models;
using EmployeeAttendanceWithHealthDeclaration.ExternalApis.Ldap.Repositories;
using EmployeeAttendanceWithHealthDeclaration.Models;

namespace EmployeeAttendanceWithHealthDeclaration.ExternalApis.Ldap.Services
{
    public class LdapService : ILdapService
    {
        private readonly ILdapRepository _ldapRepository;
        private readonly ILogger<LdapService> _logger;

        public LdapService(
            ILdapRepository ldapRepository,
            ILogger<LdapService> logger)
        {
            _ldapRepository = ldapRepository;
            _logger = logger;
        }

        public async Task<Response<ldap_user>> Login(string username, string password)
        {
            try
            {
                _logger.LogInformation("LDAP service login attempt for user: {Username}", username);

                var result = await _ldapRepository.Login(username, password);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LDAP service error for user: {Username}", username);
                return new Response<ldap_user>
                {
                    Success = false,
                    Message = "LDAP service error",
                    Data = null
                };
            }
        }
    }
}