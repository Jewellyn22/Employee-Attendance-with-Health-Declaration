using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using Dapper;
using System.Data;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public class TimeLogsRepository : ITimeLogsRepository
    {
        private readonly IDbConnection _db;

        public TimeLogsRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<time_log>> GetAll()
        {
            const string storedProc = "sp_time_logs_GetAll";
            return await _db.QueryAsync<time_log>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<time_log>> GetTodayAttendance()
        {
            const string storedProc = "sp_time_logs_GetTodayAttendance";
            return await _db.QueryAsync<time_log>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<time_log?> GetByAttendanceId(int attendance_id)
        {
            const string storedProc = "sp_time_logs_GetByAttendanceId";
            return await _db.QuerySingleOrDefaultAsync<time_log>(
                storedProc,
                new { p_attendance_id = attendance_id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<time_log>> GetByEmployeeId(string employee_id)
        {
            const string storedProc = "sp_time_logs_GetByEmployeeId";
            return await _db.QueryAsync<time_log>(
                storedProc,
                new { p_employee_id = employee_id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<time_log?> GetLastScan(string employee_id)
        {
            const string storedProc = "sp_time_logs_GetLastScan";
            return await _db.QuerySingleOrDefaultAsync<time_log>(
                storedProc,
                new { p_employee_id = employee_id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<time_log?> GetOpenSession(string employee_id)
        {
            const string storedProc = "sp_time_logs_GetOpenSession";
            return await _db.QuerySingleOrDefaultAsync<time_log>(
                storedProc,
                new { p_employee_id = employee_id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<time_log> Create(time_log time_log)
        {
            const string storedProc = "sp_time_logs_Create";
            return await _db.QuerySingleOrDefaultAsync<time_log>(
                storedProc,
                new
                {
                    p_employee_id = time_log.employee_id,
                    p_time_in = time_log.time_in,
                    p_time_out = time_log.time_out,
                    p_health_status = time_log.health_status
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<time_log> Update(time_log time_log)
        {
            const string storedProc = "sp_time_logs_Update";
            return await _db.QuerySingleOrDefaultAsync<time_log>(
                storedProc,
                new
                {
                    p_attendance_id = time_log.attendance_id,
                    p_employee_id = time_log.employee_id,
                    p_time_in = time_log.time_in,
                    p_time_out = time_log.time_out,
                    p_health_status = time_log.health_status
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> UpdateHealthStatus(int attendance_id, string health_status)
        {
            const string storedProc = "sp_time_logs_UpdateHealthStatus";
            var result = await _db.ExecuteAsync(
                storedProc,
                new { p_attendance_id = attendance_id, p_health_status = health_status },
                commandType: CommandType.StoredProcedure
            );
            return result > 0;
        }

        // New methods for admin management
        public async Task<time_log> UpdateForAdminEdit(time_log timelog)
        {
            const string storedProc = "sp_time_logs_UpdateForAdminEdit";
            return await _db.QuerySingleOrDefaultAsync<time_log>(
                storedProc,
                new
                {
                    p_attendance_id = timelog.attendance_id,
                    p_employee_id = timelog.employee_id,
                    p_time_in = timelog.time_in,
                    p_time_out = timelog.time_out,
                    p_health_status = timelog.health_status,
                    p_updated_by = timelog.updated_by,
                    p_updated_at = timelog.updated_at
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> Delete(int attendance_id)
        {
            const string storedProc = "sp_time_logs_Delete";
            var result = await _db.ExecuteAsync(
                storedProc,
                new { p_attendance_id = attendance_id },
                commandType: CommandType.StoredProcedure
            );
            return result > 0;
        }

        public async Task<IEnumerable<time_log>> GetAllFiltered(string employee_id = null, DateTime? from_date = null, DateTime? to_date = null, string health_status = null)
        {
            const string storedProc = "sp_time_logs_GetAllFiltered";
            return await _db.QueryAsync<time_log>(
                storedProc,
                new
                {
                    p_employee_id = employee_id,
                    p_from_date = from_date,
                    p_to_date = to_date,
                    p_health_status = health_status
                },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}