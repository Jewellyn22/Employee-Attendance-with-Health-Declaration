namespace EmployeeAttendanceWithHealthDeclaration.Models.Domain
{
    /// <summary>
    /// Everything the dashboard charts need in one payload:
    /// overall = per-day sums across the provider dimension (computed in
    /// HistoryLogsService; no double counting), by_provider / by_project
    /// are the raw SP result sets.
    /// </summary>
    public class dashboard_stats
    {
        public List<dashboard_daily_stat> overall { get; set; } = new();
        public List<dashboard_daily_stat> by_provider { get; set; } = new();
        public List<dashboard_daily_stat> by_project { get; set; } = new();
    }
}
