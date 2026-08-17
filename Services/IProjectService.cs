using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public interface IProjectService
    {
        Task<Response<project>> GetByProjectCode(string project_code);
        Task<Response<IEnumerable<project>>> GetAll();
        Task<Response<IEnumerable<project>>> GetByProviderCode(string provider_code);
        Task<Response<project>> Create(project project, string admin_employee_id);
        Task<Response<project>> Update(project project, string admin_employee_id);
        Task<Response<bool>> SetInactive(string project_code);
        Task<Response<int>> GetActiveCount();
    }
}