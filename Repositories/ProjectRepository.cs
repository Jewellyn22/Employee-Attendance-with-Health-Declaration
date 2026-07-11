using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using Dapper;
using System.Data;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly IDbConnection _db;

        public ProjectRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<project>> GetAll()
        {
            const string storedProc = "sp_project_GetAll";
            return await _db.QueryAsync<project>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<project>> GetByProviderCode(string provider_code)
        {
            const string storedProc = "sp_project_GetByProviderCode";
            return await _db.QueryAsync<project>(
                storedProc,
                new { p_provider_code = provider_code },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<project?> GetByProjectCode(string project_code)
        {
            const string storedProc = "sp_project_GetByProjectCode";
            return await _db.QuerySingleOrDefaultAsync<project>(
                storedProc,
                new { p_project_code = project_code },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<project?> Create(project project)
        {
            const string storedProc = "sp_project_Create";
            return await _db.QuerySingleOrDefaultAsync<project?>(
                storedProc,
                new
                {
                    p_project_name = project.project_name,
                    p_provider_code = project.provider_code,
                    p_provider_pic = project.provider_pic,
                    p_provider_pic_number = project.provider_pic_number,
                    p_contract_startdate = project.contract_startdate,
                    p_contract_enddate = project.contract_enddate,
                    p_active = project.active
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<project?> Update(project project)
        {
            const string storedProc = "sp_project_Update";
            return await _db.QuerySingleOrDefaultAsync<project?>(
                storedProc,
                new
                {
                    p_project_code = project.project_code,
                    p_project_name = project.project_name,
                    p_provider_code = project.provider_code,
                    p_provider_pic = project.provider_pic,
                    p_provider_pic_number = project.provider_pic_number,
                    p_contract_startdate = project.contract_startdate,
                    p_contract_enddate = project.contract_enddate,
                    p_active = project.active
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> SetInactive(string project_code)
        {
            const string storedProc = "sp_project_SetInactive";
            var result = await _db.ExecuteAsync(
                storedProc,
                new { p_project_code = project_code },
                commandType: CommandType.StoredProcedure
            );
            return result > 0;
        }
    }
}