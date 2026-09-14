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
            string? employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string? health_status = null)
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
            string? employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string? health_status = null)
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

        public async Task<Response<int>> GetOpenSessionsCount()
        {
            try
            {
                var count = await _timeLogsRepository.GetOpenSessionsCount();
                _logger.LogInformation("Open sessions count retrieved: {Count}", count);

                return new Response<int>
                {
                    Success = true,
                    Message = "Open sessions count retrieved successfully",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting open sessions count");
                return new Response<int>
                {
                    Success = false,
                    Message = "Error retrieving open sessions count",
                    Data = 0
                };
            }
        }

        public async Task<Response<IEnumerable<attendance_log_with_employee>>> GetRecentForDashboard()
        {
            try
            {
                _logger.LogInformation("Getting recent time logs for dashboard");

                var recentLogs = await _timeLogsRepository.GetRecentForDashboard();

                return new Response<IEnumerable<attendance_log_with_employee>>
                {
                    Success = true,
                    Message = "Recent time logs retrieved successfully",
                    Data = recentLogs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent time logs for dashboard");
                return new Response<IEnumerable<attendance_log_with_employee>>
                {
                    Success = false,
                    Message = "Error retrieving recent time logs",
                    Data = null
                };
            }
        }

        public async Task<Response<dashboard_stats>> GetDashboardStats(DateTime? from_date = null, DateTime? to_date = null)
        {
            try
            {
                var from = (from_date ?? DateTime.Today.AddDays(-6)).Date;
                var to = (to_date ?? DateTime.Today).Date;

                if (from > to)
                {
                    return new Response<dashboard_stats>
                    {
                        Success = false,
                        Message = "From date must be on or before To date",
                        Data = null
                    };
                }

                if ((to - from).TotalDays > 366)
                {
                    return new Response<dashboard_stats>
                    {
                        Success = false,
                        Message = "Date range cannot exceed one year",
                        Data = null
                    };
                }

                _logger.LogInformation("Getting dashboard stats - FromDate: {FromDate}, ToDate: {ToDate}", from, to);

                var byProvider = (await _timeLogsRepository.GetDailyStatsByProvider(from, to)).ToList();
                var byProject = (await _timeLogsRepository.GetDailyStatsByProject(from, to)).ToList();

                // Overall = per-day sums across the PROVIDER dimension. Safe from double
                // counting: each time_logs row joins exactly one contractor_employee row,
                // so every attendance record contributes to exactly one provider_code.
                // (Summing the project rows would double-count multi-project contractors.)
                var overall = byProvider
                    .GroupBy(r => r.stat_date.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new dashboard_daily_stat
                    {
                        stat_date = g.Key,
                        total_count = g.Sum(r => r.total_count),
                        fit_count = g.Sum(r => r.fit_count),
                        unfit_count = g.Sum(r => r.unfit_count),
                        understood_count = g.Sum(r => r.understood_count),
                        not_understood_count = g.Sum(r => r.not_understood_count),
                        fit_understood_count = g.Sum(r => r.fit_understood_count),
                        fit_not_understood_count = g.Sum(r => r.fit_not_understood_count),
                        unfit_understood_count = g.Sum(r => r.unfit_understood_count),
                        unfit_not_understood_count = g.Sum(r => r.unfit_not_understood_count)
                    })
                    .ToList();

                return new Response<dashboard_stats>
                {
                    Success = true,
                    Message = "Dashboard stats retrieved successfully",
                    Data = new dashboard_stats
                    {
                        overall = overall,
                        by_provider = byProvider,
                        by_project = byProject
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return new Response<dashboard_stats>
                {
                    Success = false,
                    Message = "Error retrieving dashboard stats",
                    Data = null
                };
            }
        }
    }
}