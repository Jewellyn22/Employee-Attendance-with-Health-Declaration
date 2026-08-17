using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public interface ISystemConfigService
    {
        Task<Response<double>> GetDebounceThresholdSeconds();
        Task<Response<int>> GetHealthDeclarationValidityHours();
        Task<Response<double>> GetHealthDeclarationWindowSeconds();
        Task<Response<string>> GetAdminADGroup();
        Task<Response<bool>> GetScanInputReadOnly();
        Task<Response<system_config>> GetByKey(string key);
        Task<Response<bool>> Update(system_config config, string admin_employee_id);
        Task<Response<IEnumerable<system_config>>> GetAll();
        Task<Response<List<string>>> GetHealthDeclarationSicknessItems();
    }
}