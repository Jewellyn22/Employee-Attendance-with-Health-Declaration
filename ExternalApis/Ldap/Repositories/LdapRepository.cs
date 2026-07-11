using ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Models;
using ContractorAttendanceWithHealthDeclaration.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        // Wrapper class to handle LDAP API Response<T> structure
        private class LdapApiResponse
        {
            [JsonPropertyName("data")]
            public ldap_user Data { get; set; }

            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }
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
                    // Temporary debug logging to verify JSON mapping
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("LDAP API Response: {Response}", responseContent);

                    // Deserialize to wrapper first to handle nested Response<T> structure
                    var apiResponse = await response.Content.ReadFromJsonAsync<LdapApiResponse>();

                    if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                    {
                        // Log the mapped values to verify member_of is populated
                        _logger.LogInformation("Mapped ldap_user - username: {Username}, office: {Office}, displayName: {DisplayName}, member_of: {MemberOf}, email: {Email}",
                            apiResponse.Data.username, apiResponse.Data.office, apiResponse.Data.displayName,
                            apiResponse.Data.member_of, apiResponse.Data.email);

                        return new Response<ldap_user>
                        {
                            Success = true,
                            Message = "LDAP authentication successful",
                            Data = apiResponse.Data  // Extract the nested ldap_user
                        };
                    }
                    else
                    {
                        _logger.LogWarning("LDAP authentication failed for user: {Username} - API returned success: {Success}", username, apiResponse?.Success);
                        return new Response<ldap_user>
                        {
                            Success = false,
                            Message = apiResponse?.Message ?? "LDAP authentication failed",
                            Data = null
                        };
                    }
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