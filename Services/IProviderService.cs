using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public interface IProviderService
    {
        Task<Response<provider>> GetByProviderCode(string provider_code);
        Task<Response<IEnumerable<provider>>> GetAll();
        Task<Response<provider>> Create(provider provider, string admin_employee_id);
        Task<Response<provider>> Update(provider provider);
        Task<Response<bool>> SetInactive(string provider_code);
    }
}