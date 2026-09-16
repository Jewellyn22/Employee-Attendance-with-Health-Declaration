using EmployeeAttendanceWithHealthDeclaration.Models.Domain;

namespace EmployeeAttendanceWithHealthDeclaration.Repositories
{
    public interface ISystemConfigRepository
    {
        Task<IEnumerable<system_config>> GetAll();
        Task<system_config?> GetByKey(string key);
        Task<system_config> Create(system_config config);
        Task<system_config> Update(system_config config);
    }
}