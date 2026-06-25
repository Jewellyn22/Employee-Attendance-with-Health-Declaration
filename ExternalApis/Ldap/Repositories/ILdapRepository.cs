using ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Models;
using ContractorAttendanceWithHealthDeclaration.Models;

namespace ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Repositories
{
    public interface ILdapRepository
    {
        Task<Response<ldap_user>> Login(string username, string password);
    }
}