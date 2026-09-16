using EmployeeAttendanceWithHealthDeclaration.Models;
using EmployeeAttendanceWithHealthDeclaration.Models.Domain;

namespace EmployeeAttendanceWithHealthDeclaration.Repositories
{
    public interface IProjectRepository
    {
        Task<IEnumerable<project>> GetAll();
        Task<project?> GetByProjectCode(string project_code);
        Task<IEnumerable<project>> GetByProviderCode(string provider_code);
        Task<project?> Create(project project);
        Task<project?> Update(project project);
        Task<bool> SetInactive(string project_code);
        Task<project_delete_result?> Delete(string project_code);
        Task<int> GetActiveCount();
    }
}