using EmployeeAttendanceWithHealthDeclaration.ExternalApis.Ldap.Models;
using EmployeeAttendanceWithHealthDeclaration.Models;

namespace EmployeeAttendanceWithHealthDeclaration.ExternalApis.Ldap.Services
{
    public interface ILdapService
    {
        Task<Response<ldap_user>> Login(string username, string password);
    }
}