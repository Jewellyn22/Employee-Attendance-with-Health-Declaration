using ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Models;
using ContractorAttendanceWithHealthDeclaration.Models;

namespace ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Services
{
    public interface ILdapService
    {
        Task<Response<ldap_user>> Login(string username, string password);
    }
}