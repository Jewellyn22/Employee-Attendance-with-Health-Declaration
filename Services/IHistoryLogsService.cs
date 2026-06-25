using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public interface IHistoryLogsService
    {
        /// <summary>
        /// Get filtered attendance logs for public history logs page
        /// </summary>
        Task<Response<IEnumerable<time_log>>> GetFilteredAttendance(
            string employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string health_status = null
        );

        /// <summary>
        /// Get attendance records for export (all filtered data)
        /// </summary>
        Task<Response<IEnumerable<time_log>>> GetAttendanceForExport(
            string employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string health_status = null
        );
    }
}