using EmployeeAttendanceWithHealthDeclaration.Models;
using EmployeeAttendanceWithHealthDeclaration.Models.Domain;

namespace EmployeeAttendanceWithHealthDeclaration.Services
{
    public interface IAttendanceService
    {
        /// <summary>
        /// Process employee ID scan - creates TIME IN with FIT status, checks duplicate scan prevention
        /// </summary>
        Task<Response<time_log>> ProcessScan(string employee_id);

        /// <summary>
        /// Get today's attendance records for admin interface
        /// </summary>
        Task<Response<IEnumerable<time_log>>> GetTodayAttendance();

        /// <summary>
        /// Get specific attendance record by ID
        /// </summary>
        Task<Response<time_log>> GetByAttendanceId(int attendance_id);

        /// <summary>
        /// Update health status (FIT/UNFIT) and waiver consent (UNDERSTOOD/NOT_UNDERSTOOD) for employee self-declaration
        /// </summary>
        Task<Response<bool>> UpdateHealthStatus(int attendance_id, string health_status, string waiver_consent);

        /// <summary>
        /// Check if health declaration can be changed (within 2-minute window)
        /// </summary>
        Task<Response<bool>> CanChangeHealthStatus(int attendance_id);

        /// <summary>
        /// Get last scan timestamp for duplicate detection
        /// </summary>
        Task<Response<time_log?>> GetLastScan(string employee_id);
    }
}