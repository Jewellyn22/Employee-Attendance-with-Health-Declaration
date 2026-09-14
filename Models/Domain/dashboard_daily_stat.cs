namespace ContractorAttendanceWithHealthDeclaration.Models.Domain
{
    /// <summary>
    /// One aggregated row from sp_time_logs_GetDailyStatsByProvider / _ByProject.
    /// Provider fields are populated by the provider SP, project fields by the
    /// project SP; the service-computed "overall" rows leave all four null.
    /// </summary>
    public class dashboard_daily_stat
    {
        public DateTime stat_date { get; set; }
        public string? provider_code { get; set; }
        public string? provider_name { get; set; }
        public string? project_code { get; set; }
        public string? project_name { get; set; }
        public int total_count { get; set; }
        public int fit_count { get; set; }
        public int unfit_count { get; set; }
        public int understood_count { get; set; }
        public int not_understood_count { get; set; }
        public int fit_understood_count { get; set; }
        public int fit_not_understood_count { get; set; }
        public int unfit_understood_count { get; set; }
        public int unfit_not_understood_count { get; set; }
    }
}
