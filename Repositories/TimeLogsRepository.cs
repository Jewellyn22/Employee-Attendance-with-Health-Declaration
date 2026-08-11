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

        public async Task<time_log?> GetTodayTimeIn(string employee_id)
        {
            const string storedProc = "sp_time_logs_GetTodayTimeIn";
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
                    p_health_status = time_log.health_status,
                    p_waiver_consent = time_log.waiver_consent
                },
                commandType: CommandType.StoredProcedure
            );
        }

        // Unified update for both kiosk and admin paths. The merged SP preserves the
        // existing updated_by/updated_at when the caller passes null (kiosk self-service
        // health-status change) and writes them when set (kiosk TIME OUT, admin edit).
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
                    p_health_status = time_log.health_status,
                    p_waiver_consent = time_log.waiver_consent,
                    p_updated_by = time_log.updated_by,
                    p_updated_at = time_log.updated_at
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

        // Admin management
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

        public async Task<IEnumerable<attendance_log_with_employee>> GetAllFiltered(string? employee_id = null, DateTime? from_date = null, DateTime? to_date = null, string? health_status = null)
        {
            const string storedProc = "sp_time_logs_GetAllFiltered";
            return await _db.QueryAsync<attendance_log_with_employee>(
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

        public async Task<int> GetOpenSessionsCount()
        {
            const string storedProc = "sp_time_logs_GetOpenSessionsCount";
            return await _db.QuerySingleAsync<int>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<attendance_log_with_employee>> GetRecentForDashboard()
        {
            const string storedProc = "sp_time_logs_GetRecentForDashboard";
            return await _db.QueryAsync<attendance_log_with_employee>(
                storedProc,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}