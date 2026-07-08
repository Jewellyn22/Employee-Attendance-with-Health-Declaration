using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public interface ITimeLogsManagementService
    {
        /// <summary>
        /// Get a specific timelog record by attendance ID for editing
        /// </summary>
        Task<Response<time_log>> GetTimeLogById(int attendance_id);

        /// <summary>
        /// Update a timelog record with audit trail (admin correction)
        /// </summary>
        Task<Response<time_log>> UpdateTimeLog(time_log timelog, string admin_employee_id);

        /// <summary>
        /// Delete a timelog record with confirmation (hard delete for admin corrections)
        /// </summary>
        Task<Response<bool>> DeleteTimeLog(int attendance_id);

        /// <summary>
        /// Get all timelogs with optional filtering for admin management interface
        /// </summary>
        Task<Response<IEnumerable<attendance_log_with_employee>>> GetAllTimeLogs(
            string employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string health_status = null
        );
    }
}