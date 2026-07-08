using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public interface IContractorService
    {
        Task<Response<contractor_employee>> GetByEmployeeId(string employee_id);
        Task<Response<IEnumerable<contractor_employee>>> GetAll();
        Task<Response<contractor_employee>> Create(contractor_employee employee);
        Task<Response<contractor_employee>> Update(contractor_employee employee);
        Task<Response<bool>> SetInactive(string employee_id);
    }
}