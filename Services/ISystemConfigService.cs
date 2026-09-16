using EmployeeAttendanceWithHealthDeclaration.Models;
using EmployeeAttendanceWithHealthDeclaration.Models.Domain;

namespace EmployeeAttendanceWithHealthDeclaration.Services
{
    public interface ISystemConfigService
    {
        Task<Response<double>> GetDebounceThresholdSeconds();
        Task<Response<int>> GetHealthDeclarationValidityHours();
        Task<Response<double>> GetHealthDeclarationWindowSeconds();
        Task<Response<string>> GetAdminADGroup();
        Task<Response<bool>> GetScanInputReadOnly();
        Task<Response<string>> GetWaiverCertificationText();
        Task<Response<string>> GetWaiverAcknowledgmentText();
        Task<Response<string>> GetWaiverLiabilityReleaseText();
        Task<Response<system_config>> GetByKey(string key);
        Task<Response<bool>> Update(system_config config, string admin_employee_id);
        Task<Response<IEnumerable<system_config>>> GetAll();
        Task<Response<List<string>>> GetHealthDeclarationSicknessItems();
    }
}