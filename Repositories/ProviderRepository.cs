using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using Dapper;
using System.Data;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public class ProviderRepository : IProviderRepository
    {
        private readonly IDbConnection _db;

        public ProviderRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<provider>> GetAll()
        {
            const string storedProc = "sp_provider_GetAll";
            return await _db.QueryAsync<provider>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<provider?> GetByProviderCode(string provider_code)
        {
            const string storedProc = "sp_provider_GetByProviderCode";
            return await _db.QuerySingleOrDefaultAsync<provider>(
                storedProc,
                new { p_provider_code = provider_code },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<provider?> Create(provider provider)
        {
            const string storedProc = "sp_provider_Create";
            return await _db.QuerySingleOrDefaultAsync<provider?>(
                storedProc,
                new
                {
                    p_provider_code = provider.provider_code,
                    p_provider_name = provider.provider_name,
                    p_provider_address = provider.provider_address,
                    p_active = provider.active
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<provider?> Update(provider provider)
        {
            const string storedProc = "sp_provider_Update";
            return await _db.QuerySingleOrDefaultAsync<provider?>(
                storedProc,
                new
                {
                    p_provider_code = provider.provider_code,
                    p_provider_name = provider.provider_name,
                    p_provider_address = provider.provider_address,
                    p_active = provider.active
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> SetInactive(string provider_code)
        {
            const string storedProc = "sp_provider_SetInactive";
            var result = await _db.ExecuteAsync(
                storedProc,
                new { p_provider_code = provider_code },
                commandType: CommandType.StoredProcedure
            );
            return result > 0;
        }

        public async Task<int> GetActiveCount()
        {
            const string storedProc = "sp_provider_GetActiveCount";
            return await _db.QuerySingleAsync<int>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}