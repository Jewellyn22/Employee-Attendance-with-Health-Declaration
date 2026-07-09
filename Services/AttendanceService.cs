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

                // Step 2: Check duplicate scan debounce (applies to both TIME IN and TIME OUT)
                var debounceThresholdMinutes = await _systemConfigService.GetDebounceThresholdMinutes();
                var lastScan = await _timeLogsRepository.GetLastScan(employee_id);

                // Check against the MOST RECENT scan event:
                // - For completed sessions: time_out is most recent
                // - For open sessions: time_out is null, so check time_in
                var lastScanTime = lastScan?.time_out ?? lastScan?.time_in;

                if (lastScan != null && IsWithin2Minutes(lastScanTime, debounceThresholdMinutes.Data))
                {
                    var debounceThresholdSeconds = debounceThresholdMinutes.Data * 60;
                    return new Response<time_log>
                    {
                        Success = false,
                        Message = @"Duplicate scan - wait " + debounceThresholdSeconds + "secs." ,
                        Data = null
                    };
                }

                // Step 3: Check if contractor has already TIME-IN today with blocked health status
                var todayTimeIn = await _timeLogsRepository.GetTodayTimeIn(employee_id);
                if (todayTimeIn != null)
                {
                    // Block if health_status is "UNFIT" OR (health_status is "FIT" AND waiver_consent is "NOT_UNDERSTOOD")
                    bool shouldBlock = todayTimeIn.health_status == "UNFIT" ||
                                       (todayTimeIn.health_status == "FIT" && todayTimeIn.waiver_consent == "NOT_UNDERSTOOD");

                    if (shouldBlock)
                    {
                        string healthStatusDesc = todayTimeIn.health_status == "FIT" ? "You did not understand waiver" : "You are UNFIT";
                        return new Response<time_log>
                        {
                            Success = false,
                            Message = $"Not Allowed to Enter. {healthStatusDesc}.",
                            Data = null
                        };
                    }
                }

                // Step 4: Check for open session (TIME IN without TIME OUT)
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

                // Step 5: Create TIME IN record with FIT status and UNDERSTOOD waiver consent (only if no open session)
                var newTimeLog = new time_log
                {
                    employee_id = employee_id,
                    time_in = DateTime.Now,
                    time_out = null,
                    health_status = "FIT", // Default FIT status
                    waiver_consent = "UNDERSTOOD" // Default waiver consent
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

        public async Task<Response<bool>> UpdateHealthStatus(int attendance_id, string health_status, string waiver_consent)
        {
            try
            {
                _logger.LogInformation("Updating health status and waiver consent for attendance: {AttendanceId} to health={HealthStatus}, waiver={WaiverConsent}",
                    attendance_id, health_status, waiver_consent);

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

                // Validate waiver consent enum values
                if (waiver_consent != "UNDERSTOOD" && waiver_consent != "NOT_UNDERSTOOD")
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = "Invalid waiver consent. Must be 'UNDERSTOOD' or 'NOT_UNDERSTOOD'",
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

                // Check if within health declaration window from TIME IN (same window for both health status and waiver consent)
                var healthWindowMinutes = await _systemConfigService.GetHealthDeclarationWindowMinutes();
                if (!IsWithin2Minutes(attendance.time_in, healthWindowMinutes.Data))
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = $"Health declaration and waiver consent window has expired. Changes allowed only within {healthWindowMinutes.Data} minutes of TIME IN.",
                        Data = false
                    };
                }

                // Update health status and waiver consent in memory
                attendance.health_status = health_status;
                attendance.waiver_consent = waiver_consent;

                // Set time_out based on health status and waiver consent
                if (health_status == "UNFIT")
                {
                    // UNFIT always triggers auto time-out
                    attendance.time_out = DateTime.Now;
                    _logger.LogInformation("Health status set to UNFIT for {AttendanceId}, TIME OUT set", attendance_id);
                }
                else if (health_status == "FIT")
                {
                    if (waiver_consent == "NOT_UNDERSTOOD")
                    {
                        // NOT_UNDERSTOOD triggers TIME_OUT even if FIT
                        attendance.time_out = DateTime.Now;
                        _logger.LogInformation("Waiver consent set to NOT_UNDERSTOOD for {AttendanceId}, TIME OUT set even though health is FIT", attendance_id);
                    }
                    else if (attendance.time_out != null && IsWithin2Minutes(attendance.time_out, healthWindowMinutes.Data))
                    {
                        // If changing back to UNDERSTOOD + FIT within window, remove auto TIME_OUT
                        attendance.time_out = null;
                        _logger.LogInformation("Health status set to FIT and waiver consent to UNDERSTOOD for {AttendanceId}, auto TIME OUT removed", attendance_id);
                    }
                }

                // Single database call to update all changed fields
                var updated = await _timeLogsRepository.Update(attendance);
                if (updated == null)
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = "Failed to update health status and waiver consent",
                        Data = false
                    };
                }

                return new Response<bool>
                {
                    Success = true,
                    Message = $"Health status updated to {health_status}, waiver consent updated to {waiver_consent}",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating health status and waiver consent for attendance: {AttendanceId}", attendance_id);
                return new Response<bool>
                {
                    Success = false,
                    Message = "Error updating health status and waiver consent",
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