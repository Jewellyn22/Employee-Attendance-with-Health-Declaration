namespace ContractorAttendanceWithHealthDeclaration.Helpers
{
    /// <summary>
    /// Canonical string values for the time_logs health_status and waiver_consent
    /// columns (MySQL ENUMs). Kept as const strings rather than C# enums because the
    /// API contract, the kiosk JS, and the stored-procedure parameters all use these
    /// literals. Centralising them removes the magic-string duplication that had
    /// drifted across AttendanceService and TimeLogsManagementService.
    /// </summary>
    public static class HealthConstants
    {
        // health_status ENUM('FIT', 'UNFIT')
        public const string StatusFit = "FIT";
        public const string StatusUnfit = "UNFIT";

        // waiver_consent ENUM('UNDERSTOOD', 'NOT_UNDERSTOOD')
        public const string WaiverUnderstood = "UNDERSTOOD";
        public const string WaiverNotUnderstood = "NOT_UNDERSTOOD";
    }
}
