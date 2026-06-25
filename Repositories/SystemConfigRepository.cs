using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using Dapper;
using System.Data;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly IDbConnection _db;

        public SystemConfigRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<system_config>> GetAll()
        {
            const string storedProc = "sp_system_config_GetAll";
            return await _db.QueryAsync<system_config>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<system_config?> GetByKey(string key)
        {
            const string storedProc = "sp_system_config_GetByKey";
            return await _db.QuerySingleOrDefaultAsync<system_config>(
                storedProc,
                new { p_key = key },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<system_config> Create(system_config config)
        {
            const string storedProc = "sp_system_config_Create";
            return await _db.QuerySingleOrDefaultAsync<system_config>(
                storedProc,
                new
                {
                    p_key = config.key,
                    p_value = config.value,
                    p_description = config.description
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<system_config> Update(system_config config)
        {
            const string storedProc = "sp_system_config_Update";
            return await _db.QuerySingleOrDefaultAsync<system_config>(
                storedProc,
                new
                {
                    p_key = config.key,
                    p_value = config.value,
                    p_description = config.description
                },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}