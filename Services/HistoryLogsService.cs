using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public class HistoryLogsService : IHistoryLogsService
    {
        private readonly ITimeLogsRepository _timeLogsRepository;
        private readonly ILogger<HistoryLogsService> _logger;

        public HistoryLogsService(
            ITimeLogsRepository timeLogsRepository,
            ILogger<HistoryLogsService> logger)
        {
            _timeLogsRepository = timeLogsRepository;
            _logger = logger;
        }

        public async Task<Response<IEnumerable<attendance_log_with_employee>>> GetFilteredAttendance(
            string employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string health_status = null)
        {
            try
            {
                _logger.LogInformation("Getting filtered attendance logs - EmployeeId: {EmployeeId}, FromDate: {FromDate}, ToDate: {ToDate}, HealthStatus: {HealthStatus}",
                    employee_id, from_date, to_date, health_status);

                // Get filtered attendance from repository (includes employee name and provider)
                var attendance = await _timeLogsRepository.GetAllFiltered(employee_id, from_date, to_date, health_status);

                return new Response<IEnumerable<attendance_log_with_employee>>
                {
                    Success = true,
                    Message = "Filtered attendance retrieved successfully",
                    Data = attendance
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered attendance logs");
                return new Response<IEnumerable<attendance_log_with_employee>>
                {
                    Success = false,
                    Message = "Error retrieving attendance logs",
                    Data = null
                };
            }
        }

        public async Task<Response<IEnumerable<attendance_log_with_employee>>> GetAttendanceForExport(
            string employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string health_status = null)
        {
            try
            {
                _logger.LogInformation("Getting attendance data for export - EmployeeId: {EmployeeId}, FromDate: {FromDate}, ToDate: {ToDate}, HealthStatus: {HealthStatus}",
                    employee_id, from_date, to_date, health_status);

                // Use same filtered query logic
                var attendance = await _timeLogsRepository.GetAllFiltered(employee_id, from_date, to_date, health_status);

                return new Response<IEnumerable<attendance_log_with_employee>>
                {
                    Success = true,
                    Message = "Attendance data for export retrieved successfully",
                    Data = attendance
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance data for export");
                return new Response<IEnumerable<attendance_log_with_employee>>
                {
                    Success = false,
                    Message = "Error retrieving export data",
                    Data = null
                };
            }
        }
    }
}