using ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Models;
using ContractorAttendanceWithHealthDeclaration.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Repositories
{
    public class LdapRepository : ILdapRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LdapRepository> _logger;

        public LdapRepository(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<LdapRepository> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var ldapUrl = _configuration["ApiHosts:Ldap"];
            _httpClient.BaseAddress = new Uri(ldapUrl);
        }

        public async Task<Response<ldap_user>> Login(string username, string password)
        {
            try
            {
                _logger.LogInformation("LDAP login attempt for user: {Username}", username);

                var loginRequest = new
                {
                    username,
                    password
                };

                var response = await _httpClient.PostAsJsonAsync("/Auth/Login", loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var ldapUser = await response.Content.ReadFromJsonAsync<ldap_user>();
                    return new Response<ldap_user>
                    {
                        Success = true,
                        Message = "LDAP authentication successful",
                        Data = ldapUser
                    };
                }
                else
                {
                    _logger.LogWarning("LDAP authentication failed for user: {Username}", username);
                    return new Response<ldap_user>
                    {
                        Success = false,
                        Message = "Invalid credentials",
                        Data = null
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LDAP authentication error for user: {Username}", username);
                return new Response<ldap_user>
                {
                    Success = false,
                    Message = "LDAP authentication error",
                    Data = null
                };
            }
        }
    }
}