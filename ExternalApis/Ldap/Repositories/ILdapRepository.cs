using EmployeeAttendanceWithHealthDeclaration.ExternalApis.Ldap.Models;
using EmployeeAttendanceWithHealthDeclaration.Models;

namespace EmployeeAttendanceWithHealthDeclaration.ExternalApis.Ldap.Repositories
{
    public interface ILdapRepository
    {
        Task<Response<ldap_user>> Login(string username, string password);
    }
}