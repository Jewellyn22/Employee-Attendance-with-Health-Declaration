using ContractorAttendanceWithHealthDeclaration.Helpers;
using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IContractorEmployeeRepository _contractorRepository;
        private readonly ITimeLogsRepository _timeLogsRepository;
        private readonly IProviderRepository _providerRepository;
        private readonly ISystemConfigService _systemConfigService;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(
            IContractorEmployeeRepository contractorRepository,
            ITimeLogsRepository timeLogsRepository,
            IProviderRepository providerRepository,
            ISystemConfigService systemConfigService,
            ILogger<AttendanceService> logger)
        {
            _contractorRepository = contractorRepository;
            _timeLogsRepository = timeLogsRepository;
            _providerRepository = providerRepository;
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
                        Message = $"{employee.name} - Contractor is inactive",
                        Data = null
                    };
                }

                // Step 2: Check duplicate scan debounce (applies to both TIME IN and TIME OUT)
                var debounceThresholdSeconds = await _systemConfigService.GetDebounceThresholdSeconds();
                var lastScan = await _timeLogsRepository.GetLastScan(employee_id);

                // Check against the MOST RECENT scan event:
                // - For completed sessions: time_out is most recent
                // - For open sessions: time_out is null, so check time_in
                var lastScanTime = lastScan?.time_out ?? lastScan?.time_in;

                if (lastScan != null && IsWithinThreshold(lastScanTime, debounceThresholdSeconds.Data))
                {
                    return new Response<time_log>
                    {
                        Success = false,
                        Message = $"{employee.name} - Duplicate scan - wait {debounceThresholdSeconds.Data} seconds.",
                        Data = null
                    };
                }

                // Step 3: Check if contractor has already TIME-IN today with blocked health status
                var todayTimeIn = await _timeLogsRepository.GetTodayTimeIn(employee_id);
                if (todayTimeIn != null)
                {
                    // Block entry if the contractor's latest TIME IN today is a "not allowed to enter"
                    // combination (UNFIT, or FIT + NOT_UNDERSTOOD).
                    bool shouldBlock = BusinessRulesHelper.IsNotAllowedToEnter(todayTimeIn.health_status, todayTimeIn.waiver_consent);

                    if (shouldBlock)
                    {
                        string healthStatusDesc = todayTimeIn.health_status == HealthConstants.StatusFit ? "You did not understand waiver" : "You are UNFIT";
                        return new Response<time_log>
                        {
                            Success = false,
                            Message = $"{employee.name} - Not Allowed to Enter. {healthStatusDesc}.",
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
                        Message = $"{employee.name} - SUCCESS TIME OUT",
                        Data = updatedLog
                    };
                }

                // Step 4b: Validate the contractor still has an ACTIVE project before
                // allowing TIME IN (placed after the open-session check so deactivating
                // a project/provider mid-day never blocks a contractor from timing out).
                // Multi-project: one active assignment is enough; active_project_count
                // comes from the GetByEmployeeId join over contractor_project.
                if (employee.active_project_count == 0)
                {
                    _logger.LogInformation("TIME IN blocked for {EmployeeId}: no active project assignment",
                        employee_id);

                    return new Response<time_log>
                    {
                        Success = false,
                        Message = $"{employee.name} - Not Allowed to Enter. Project is In-Active.",
                        Data = null
                    };
                }

                var provider = await _providerRepository.GetByProviderCode(employee.provider_code);
                if (provider == null || provider.active == 0)
                {
                    _logger.LogInformation("TIME IN blocked for {EmployeeId}: provider {ProviderCode} is inactive",
                        employee_id, employee.provider_code);

                    return new Response<time_log>
                    {
                        Success = false,
                        Message = $"{employee.name} - Not Allowed to Enter. Provider is In-Active.",
                        Data = null
                    };
                }

                // Step 5: Create TIME IN record with FIT status and UNDERSTOOD waiver consent (only if no open session)
                var newTimeLog = new time_log
                {
                    employee_id = employee_id,
                    time_in = DateTime.Now,
                    time_out = null,
                    health_status = HealthConstants.StatusFit, // Default FIT status
                    waiver_consent = HealthConstants.WaiverUnderstood // Default waiver consent
                };

                var createdLog = await _timeLogsRepository.Create(newTimeLog);

                _logger.LogInformation("TIME IN created for {EmployeeId} with FIT status", employee_id);

                return new Response<time_log>
                {
                    Success = true,
                    Message = $"{employee.name} - SUCCESS TIME IN",
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
                var healthError = ValidationHelper.ValidateHealthStatus(health_status);
                if (healthError != null)
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = healthError,
                        Data = false
                    };
                }

                // Validate waiver consent enum values
                var waiverError = ValidationHelper.ValidateWaiverConsent(waiver_consent);
                if (waiverError != null)
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = waiverError,
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
                var healthWindowSeconds = await _systemConfigService.GetHealthDeclarationWindowSeconds();
                if (!IsWithinThreshold(attendance.time_in, healthWindowSeconds.Data))
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = $"Health declaration and waiver consent window has expired. Changes allowed only within {healthWindowSeconds.Data} seconds of TIME IN.",
                        Data = false
                    };
                }

                // Update health status and waiver consent in memory
                attendance.health_status = health_status;
                attendance.waiver_consent = waiver_consent;

                // Set time_out based on health status and waiver consent
                if (health_status == HealthConstants.StatusUnfit)
                {
                    // UNFIT always triggers auto time-out
                    attendance.time_out = DateTime.Now;
                    _logger.LogInformation("Health status set to UNFIT for {AttendanceId}, TIME OUT set", attendance_id);
                }
                else if (health_status == HealthConstants.StatusFit)
                {
                    if (waiver_consent == HealthConstants.WaiverNotUnderstood)
                    {
                        // NOT_UNDERSTOOD triggers TIME_OUT even if FIT
                        attendance.time_out = DateTime.Now;
                        _logger.LogInformation("Waiver consent set to NOT_UNDERSTOOD for {AttendanceId}, TIME OUT set even though health is FIT", attendance_id);
                    }
                    else if (attendance.time_out != null && IsWithinThreshold(attendance.time_out, healthWindowSeconds.Data))
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

                var healthWindowSeconds = await _systemConfigService.GetHealthDeclarationWindowSeconds();
                var canChange = IsWithinThreshold(attendance.time_in, healthWindowSeconds.Data);

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

        // Thin wrapper over BusinessRulesHelper.IsWithinThreshold kept so existing
        // call sites read naturally; the rule itself now lives in one place.
        private bool IsWithinThreshold(DateTime? timestamp, double thresholdSeconds)
            => BusinessRulesHelper.IsWithinThreshold(timestamp, thresholdSeconds);
    }
}