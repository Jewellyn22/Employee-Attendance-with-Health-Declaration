using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public interface ISystemConfigService
    {
        Task<Response<int>> GetDebounceThresholdMinutes();
        Task<Response<int>> GetHealthDeclarationValidityHours();
        Task<Response<int>> GetHealthDeclarationWindowMinutes();
        Task<Response<string>> GetAdminADGroup();
        Task<Response<system_config>> GetByKey(string key);
        Task<Response<bool>> Update(system_config config);
        Task<Response<IEnumerable<system_config>>> GetAll();
    }
}