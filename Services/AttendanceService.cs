using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IContractorEmployeeRepository _contractorRepository;
        private readonly ITimeLogsRepository _timeLogsRepository;
        private readonly ISystemConfigService _systemConfigService;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(
            IContractorEmployeeRepository contractorRepository,
            ITimeLogsRepository timeLogsRepository,
            ISystemConfigService systemConfigService,
            ILogger<AttendanceService> logger)
        {
            _contractorRepository = contractorRepository;
            _timeLogsRepository = timeLogsRepository;
            _systemConfigService = systemConfigService;
            _logger = logger;
        }

        public async Task<Response<time_log>> ProcessScan(string employee_id)
        {
            try
            {
                _logger.LogInformation("Processing scan for employee: {EmployeeId}", employee_id);

                // Step 1: Validate contractor exists and is active
                var employee = await _contractorRepository.GetByEmployeeId(employee_id);
                if (employee == null)
                {
                    return new Response<time_log>
                    {
                        Success = false,
                        Message = "Contractor not found",
                        Data = null
                    };
                }

                if (employee.active == 0)
                {
                    return new Response<time_log>
                    {
                        Success = false,
                        Message = "Contractor is inactive",
                        Data = null
                    };
                }

                // Step 2: Check 2-minute duplicate scan debounce
                var debounceThresholdMinutes = await _systemConfigService.GetDebounceThresholdMinutes();
                var lastScan = await _timeLogsRepository.GetLastScan(employee_id);

                if (lastScan != null && IsWithin2Minutes(lastScan.time_in ?? lastScan.time_out, debounceThresholdMinutes.Data))
                {
                    var debounceThresholdSeconds = debounceThresholdMinutes.Data * 60;
                    return new Response<time_log>
                    {
                        Success = false,
                        Message = @"Duplicate scan - wait " + debounceThresholdSeconds + "secs." ,
                        Data = null
                    };
                }

                // Step 3: Check for open session (TIME IN without TIME OUT)
                var openSession = await _timeLogsRepository.GetOpenSession(employee_id);

                if (openSession != null)
                {
                    // Open session exists → Update with TIME OUT
                    openSession.time_out = DateTime.Now;
                    openSession.updated_at = DateTime.Now;
                    openSession.updated_by = employee_id; // Self-service scan

                    var updatedLog = await _timeLogsRepository.Update(openSession);

                    _logger.LogInformation("TIME OUT recorded for {EmployeeId}, attendance_id: {AttendanceId}",
                        employee_id, openSession.attendance_id);

                    return new Response<time_log>
                    {
                        Success = true,
                        Message = "SUCCESS TIME OUT",
                        Data = updatedLog
                    };
                }

                // Step 4: Create TIME IN record with FIT status (only if no open session)
                var newTimeLog = new time_log
                {
                    employee_id = employee_id,
                    time_in = DateTime.Now,
                    time_out = null,
                    health_status = "FIT" // Default FIT status
                };

                var createdLog = await _timeLogsRepository.Create(newTimeLog);

                _logger.LogInformation("TIME IN created for {EmployeeId} with FIT status", employee_id);

                return new Response<time_log>
                {
                    Success = true,
                    Message = "SUCCESS TIME IN",
                    Data = createdLog
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scan for {EmployeeId}", employee_id);
                return new Response<time_log>
                {
                    Success = false,
                    Message = "Error processing scan",
                    Data = null
                };
            }
        }

        public async Task<Response<IEnumerable<time_log>>> GetTodayAttendance()
        {
            try
            {
                var attendance = await _timeLogsRepository.GetTodayAttendance();
                return new Response<IEnumerable<time_log>>
                {
                    Success = true,
                    Message = "Attendance retrieved successfully",
                    Data = attendance
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's attendance");
                return new Response<IEnumerable<time_log>>
                {
                    Success = false,
                    Message = "Error retrieving attendance",
                    Data = null
                };
            }
        }

        public async Task<Response<time_log>> GetByAttendanceId(int attendance_id)
        {
            try
            {
                var attendance = await _timeLogsRepository.GetByAttendanceId(attendance_id);
                if (attendance == null)
                {
                    return new Response<time_log>
                    {
                        Success = false,
                        Message = "Attendance record not found",
                        Data = null
                    };
                }

                return new Response<time_log>
                {
                    Success = true,
                    Message = "Attendance retrieved successfully",
                    Data = attendance
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance by ID: {AttendanceId}", attendance_id);
                return new Response<time_log>
                {
                    Success = false,
                    Message = "Error retrieving attendance",
                    Data = null
                };
            }
        }

        public async Task<Response<bool>> UpdateHealthStatus(int attendance_id, string health_status)
        {
            try
            {
                _logger.LogInformation("Updating health status for attendance: {AttendanceId} to {HealthStatus}",
                    attendance_id, health_status);

                // Validate health status enum values
                if (health_status != "FIT" && health_status != "UNFIT")
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = "Invalid health status. Must be 'FIT' or 'UNFIT'",
                        Data = false
                    };
                }

                // Get the attendance record
                var attendance = await _timeLogsRepository.GetByAttendanceId(attendance_id);
                if (attendance == null)
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = "Attendance record not found",
                        Data = false
                    };
                }

                // Check if within 2-minute window from TIME IN
                var healthWindowMinutes = await _systemConfigService.GetHealthDeclarationWindowMinutes();
                if (!IsWithin2Minutes(attendance.time_in, healthWindowMinutes.Data))
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = "Health declaration window has expired. Please contact admin for corrections.",
                        Data = false
                    };
                }

                // Update health status in memory
                attendance.health_status = health_status;

                // Set time_out based on health status
                if (health_status == "UNFIT")
                {
                    // UNFIT -> Auto time-out
                    attendance.time_out = DateTime.Now;
                    _logger.LogInformation("Health status set to UNFIT for {AttendanceId}, TIME OUT set", attendance_id);
                }
                else if (health_status == "FIT" && attendance.time_out != null)
                {
                    // Only clear time_out if it was auto-set for UNFIT (within health declaration window)
                    var timeOutElapsed = DateTime.Now - attendance.time_out.Value;
                    if (timeOutElapsed.TotalMinutes <= healthWindowMinutes.Data)
                    {
                        attendance.time_out = null;
                        _logger.LogInformation("Health status set to FIT for {AttendanceId}, auto TIME OUT removed", attendance_id);
                    }
                }

                // Single database call to update all changed fields
                var updated = await _timeLogsRepository.Update(attendance);
                if (!updated)
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = "Failed to update health status",
                        Data = false
                    };
                }

                return new Response<bool>
                {
                    Success = true,
                    Message = $"Health status updated to {health_status}",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating health status for attendance: {AttendanceId}", attendance_id);
                return new Response<bool>
                {
                    Success = false,
                    Message = "Error updating health status",
                    Data = false
                };
            }
        }

        public async Task<Response<bool>> CanChangeHealthStatus(int attendance_id)
        {
            try
            {
                var attendance = await _timeLogsRepository.GetByAttendanceId(attendance_id);
                if (attendance == null)
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = "Attendance record not found",
                        Data = false
                    };
                }

                var healthWindowMinutes = await _systemConfigService.GetHealthDeclarationWindowMinutes();
                var canChange = IsWithin2Minutes(attendance.time_in, healthWindowMinutes.Data);

                return new Response<bool>
                {
                    Success = true,
                    Message = canChange ? "Health declaration can be changed" : "Health declaration window expired",
                    Data = canChange
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health status change eligibility: {AttendanceId}", attendance_id);
                return new Response<bool>
                {
                    Success = false,
                    Message = "Error checking eligibility",
                    Data = false
                };
            }
        }

        public async Task<Response<time_log?>> GetLastScan(string employee_id)
        {
            try
            {
                var lastScan = await _timeLogsRepository.GetLastScan(employee_id);
                return new Response<time_log?>
                {
                    Success = true,
                    Message = "Last scan retrieved successfully",
                    Data = lastScan
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last scan for {EmployeeId}", employee_id);
                return new Response<time_log?>
                {
                    Success = false,
                    Message = "Error retrieving last scan",
                    Data = null
                };
            }
        }

        private bool IsWithin2Minutes(DateTime? timestamp, double thresholdMinutes)
        {
            if (!timestamp.HasValue) return false;
            var timeDiff = DateTime.Now - timestamp.Value;
            return timeDiff.TotalMinutes < thresholdMinutes;
        }
    }
}