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
                    return new Response<time_log>
                    {
                        Success = false,
                        Message = "Duplicate scan - wait 2 minutes",
                        Data = null
                    };
                }

                // Step 3: Create TIME IN record with FIT status (single scan workflow)
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
                    Message = "TIME IN created with FIT status",
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

                // Update health status
                var result = await _timeLogsRepository.UpdateHealthStatus(attendance_id, health_status);

                // If UNFIT, set TIME OUT to current time
                if (health_status == "UNFIT")
                {
                    attendance.time_out = DateTime.Now;
                    await _timeLogsRepository.Update(attendance);
                    _logger.LogInformation("Health status set to UNFIT for {AttendanceId}, TIME OUT set", attendance_id);
                }
                else if (health_status == "FIT" && attendance.time_out != null)
                {
                    // If toggling back to FIT, remove TIME OUT
                    attendance.time_out = null;
                    await _timeLogsRepository.Update(attendance);
                    _logger.LogInformation("Health status set to FIT for {AttendanceId}, TIME OUT removed", attendance_id);
                }

                return new Response<bool>
                {
                    Success = true,
                    Message = $"Health status updated to {health_status}",
                    Data = result
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

        private bool IsWithin2Minutes(DateTime? timestamp, int thresholdMinutes)
        {
            if (!timestamp.HasValue) return false;
            var timeDiff = DateTime.Now - timestamp.Value;
            return timeDiff.TotalMinutes < thresholdMinutes;
        }
    }
}