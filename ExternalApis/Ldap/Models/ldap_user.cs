using System.Text.Json.Serialization;

namespace EmployeeAttendanceWithHealthDeclaration.ExternalApis.Ldap.Models
{
    public class ldap_user
    {
        [JsonPropertyName("username")]
        public string username { get; set; }

        [JsonPropertyName("firstName")]
        public string first_name { get; set; }

        [JsonPropertyName("lastName")]
        public string last_name { get; set; }

        [JsonPropertyName("displayName")]
        public string displayName { get; set; }

        [JsonPropertyName("description")]
        public string description { get; set; }

        [JsonPropertyName("email")]
        public string email { get; set; }

        [JsonPropertyName("office")]
        public string office { get; set; }

        [JsonPropertyName("memberOf")]
        public string[] member_of { get; set; }  // This is the critical property!
    }
}