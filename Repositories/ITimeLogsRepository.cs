using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Repositories
{
    public interface ITimeLogsRepository
    {
        Task<IEnumerable<time_log>> GetAll();
        Task<IEnumerable<time_log>> GetTodayAttendance();
        Task<time_log?> GetByAttendanceId(int attendance_id);
        Task<IEnumerable<time_log>> GetByEmployeeId(string employee_id);
        Task<time_log?> GetLastScan(string employee_id);
        Task<time_log?> GetOpenSession(string employee_id);
        Task<time_log> Create(time_log time_log);
        Task<time_log> Update(time_log time_log);
        Task<bool> UpdateHealthStatus(int attendance_id, string health_status);

        // New methods for admin management
        Task<time_log> UpdateForAdminEdit(time_log timelog);
        Task<bool> Delete(int attendance_id);
        Task<IEnumerable<attendance_log_with_employee>> GetAllFiltered(string employee_id = null, DateTime? from_date = null, DateTime? to_date = null, string health_status = null);
    }
}