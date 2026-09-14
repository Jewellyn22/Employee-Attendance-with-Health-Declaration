namespace ContractorAttendanceWithHealthDeclaration.Helpers
{
    /// <summary>
    /// Encapsulates the attendance business rules that were duplicated across
    /// AttendanceService and TimeLogsManagementService. Pure functions, no DI.
    /// </summary>
    public static class BusinessRulesHelper
    {
        /// <summary>
        /// "Not allowed to enter" rule: a contractor may not enter if they are UNFIT,
        /// or if they are FIT but did NOT understand the waiver. Used both at scan time
        /// (block re-entry) and during admin edit (auto-fill time_out).
        /// Comparisons are case-insensitive so values that bypass the DB ENUM
        /// (raw row edits, future SP changes) are still judged correctly.
        /// </summary>
        public static bool IsNotAllowedToEnter(string? health_status, string? waiver_consent)
            => string.Equals(health_status, HealthConstants.StatusUnfit, StringComparison.OrdinalIgnoreCase)
               || (string.Equals(health_status, HealthConstants.StatusFit, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(waiver_consent, HealthConstants.WaiverNotUnderstood, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// True when the given timestamp falls within thresholdSeconds of now.
        /// A null timestamp is treated as outside the window (matches the previous
        /// private AttendanceService.IsWithinThreshold semantics). A future timestamp
        /// yields a negative diff and therefore counts as within the window —
        /// deliberate fail-closed behavior: a suspicious stamp still debounces
        /// duplicate scans instead of letting them through.
        /// </summary>
        public static bool IsWithinThreshold(DateTime? timestamp, double thresholdSeconds)
        {
            if (!timestamp.HasValue) return false;
            var timeDiff = DateTime.Now - timestamp.Value;
            return timeDiff.TotalSeconds < thresholdSeconds;
        }
    }
}
