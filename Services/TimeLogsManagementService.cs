using ContractorAttendanceWithHealthDeclaration.Helpers;
using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public class TimeLogsManagementService : ITimeLogsManagementService
    {
        private readonly ITimeLogsRepository _timeLogsRepository;
        private readonly ILogger<TimeLogsManagementService> _logger;

        public TimeLogsManagementService(
            ITimeLogsRepository timeLogsRepository,
            ILogger<TimeLogsManagementService> logger)
        {
            _timeLogsRepository = timeLogsRepository;
            _logger = logger;
        }

        public async Task<Response<time_log>> GetTimeLogById(int attendance_id)
        {
            try
            {
                _logger.LogInformation("Getting timelog by ID: {AttendanceId}", attendance_id);

                var timelog = await _timeLogsRepository.GetByAttendanceId(attendance_id);
                if (timelog == null)
                {
                    return new Response<time_log>
                    {
                        Success = false,
                        Message = "Timelog record not found",
                        Data = null
                    };
                }

                return new Response<time_log>
                {
                    Success = true,
                    Message = "Timelog retrieved successfully",
                    Data = timelog
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timelog by ID: {AttendanceId}", attendance_id);
                return new Response<time_log>
                {
                    Success = false,
                    Message = "Error retrieving timelog",
                    Data = null
                };
            }
        }

        public async Task<Response<time_log>> UpdateTimeLog(time_log timelog, string admin_employee_id)
        {
            try
            {
                _logger.LogInformation("Admin {AdminId} updating timelog: {AttendanceId}", admin_employee_id, timelog.attendance_id);

                // Set audit trail fields
                timelog.updated_by = admin_employee_id;
                timelog.updated_at = DateTime.Now;

                // Validate health status enum values
                var healthError = ValidationHelper.ValidateHealthStatus(timelog.health_status);
                if (healthError != null)
                {
                    return new Response<time_log>
                    {
                        Success = false,
                        Message = healthError,
                        Data = null
                    };
                }

                // Validate waiver consent enum values (same rule as kiosk time-in)
                var waiverError = ValidationHelper.ValidateWaiverConsent(timelog.waiver_consent);
                if (waiverError != null)
                {
                    return new Response<time_log>
                    {
                        Success = false,
                        Message = waiverError,
                        Data = null
                    };
                }

                // Keep records consistent: "not allowed to enter" combos (UNFIT, or FIT + NOT_UNDERSTOOD)
                // require a Time Out. If Time Out is blank, auto-fill it with Time In (zero-duration session).
                // Existing Time Out values are never overwritten; reverting to FIT + UNDERSTOOD does not clear it.
                if (BusinessRulesHelper.IsNotAllowedToEnter(timelog.health_status, timelog.waiver_consent)
                    && timelog.time_out == null && timelog.time_in != null)
                {
                    timelog.time_out = timelog.time_in;
                    _logger.LogInformation("Auto-set time_out = time_in for attendance {AttendanceId} (not-allowed combo, time_out was blank)", timelog.attendance_id);
                }

                var updatedLog = await _timeLogsRepository.Update(timelog);

                _logger.LogInformation("Timelog {AttendanceId} updated successfully by admin {AdminId}",
                    timelog.attendance_id, admin_employee_id);

                return new Response<time_log>
                {
                    Success = true,
                    Message = "Timelog updated successfully",
                    Data = updatedLog
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating timelog: {AttendanceId}", timelog.attendance_id);
                return new Response<time_log>
                {
                    Success = false,
                    Message = "Error updating timelog",
                    Data = null
                };
            }
        }

        public async Task<Response<bool>> DeleteTimeLog(int attendance_id)
        {
            try
            {
                _logger.LogInformation("Deleting timelog: {AttendanceId}", attendance_id);

                // Verify timelog exists before deletion
                var existingLog = await _timeLogsRepository.GetByAttendanceId(attendance_id);
                if (existingLog == null)
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = "Timelog record not found",
                        Data = false
                    };
                }

                var result = await _timeLogsRepository.Delete(attendance_id);

                if (result)
                {
                    _logger.LogInformation("Timelog {AttendanceId} deleted successfully", attendance_id);
                    return new Response<bool>
                    {
                        Success = true,
                        Message = "Timelog deleted successfully",
                        Data = true
                    };
                }
                else
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = "Failed to delete timelog",
                        Data = false
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting timelog: {AttendanceId}", attendance_id);
                return new Response<bool>
                {
                    Success = false,
                    Message = "Error deleting timelog",
                    Data = false
                };
            }
        }

        public async Task<Response<IEnumerable<attendance_log_with_employee>>> GetAllTimeLogs(
            string? employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string? health_status = null)
        {
            try
            {
                _logger.LogInformation("Getting all timelogs with filters - EmployeeId: {EmployeeId}, FromDate: {FromDate}, ToDate: {ToDate}, HealthStatus: {HealthStatus}",
                    employee_id, from_date, to_date, health_status);

                var timelogs = await _timeLogsRepository.GetAllFiltered(employee_id, from_date, to_date, health_status);

                return new Response<IEnumerable<attendance_log_with_employee>>
                {
                    Success = true,
                    Message = "Timelogs retrieved successfully",
                    Data = timelogs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered timelogs");
                return new Response<IEnumerable<attendance_log_with_employee>>
                {
                    Success = false,
                    Message = "Error retrieving timelogs",
                    Data = null
                };
            }
        }
    }
}