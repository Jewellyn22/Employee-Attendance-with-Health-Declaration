using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public interface IProviderRepository
    {
        Task<IEnumerable<provider>> GetAll();
        Task<provider?> GetByProviderCode(string provider_code);
        Task<provider?> Create(provider provider);
        Task<provider?> Update(provider provider);
        Task<bool> SetInactive(string provider_code);
        Task<int> GetActiveCount();
    }
}