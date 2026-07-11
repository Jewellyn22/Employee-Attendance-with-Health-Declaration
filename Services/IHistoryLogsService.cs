using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public interface IHistoryLogsService
    {
        /// <summary>
        /// Get filtered attendance logs for public history logs page
        /// </summary>
        Task<Response<IEnumerable<attendance_log_with_employee>>> GetFilteredAttendance(
            string? employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string? health_status = null
        );

        /// <summary>
        /// Get attendance records for export (all filtered data)
        /// </summary>
        Task<Response<IEnumerable<attendance_log_with_employee>>> GetAttendanceForExport(
            string? employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string? health_status = null
        );
    }
}