using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Helpers
{
    /// <summary>
    /// Pure (non-DB) validation helpers shared across services. Each method returns
    /// null when valid, otherwise the EXACT user-facing error message that the
    /// services already returned inline - so wiring these in changes no UI text.
    /// DB-dependent checks (does the project/provider exist?) stay in the services.
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>Null if health_status is a valid ENUM value, else the error message.</summary>
        public static string? ValidateHealthStatus(string? health_status)
            => (health_status == HealthConstants.StatusFit || health_status == HealthConstants.StatusUnfit)
                ? null
                : "Invalid health status. Must be 'FIT' or 'UNFIT'";

        /// <summary>Null if waiver_consent is a valid ENUM value, else the error message.</summary>
        public static string? ValidateWaiverConsent(string? waiver_consent)
            => (waiver_consent == HealthConstants.WaiverUnderstood || waiver_consent == HealthConstants.WaiverNotUnderstood)
                ? null
                : "Invalid waiver consent. Must be 'UNDERSTOOD' or 'NOT_UNDERSTOOD'";

        /// <summary>
        /// Validates the contractor required fields shared by Create() and Update().
        /// Returns null when valid, else the first field error encountered. Does NOT
        /// validate employee_id (auto-generated on create, validated separately on
        /// update) or gender (validated only in BulkCreate) - those are the callers' job.
        /// Position is validated per assignment (it is a per-project attribute since
        /// the employee-level column was dropped), replacing the old project_codes CSV
        /// and single-position checks.
        /// </summary>
        public static string? ValidateContractorEmployee(contractor_employee? employee)
        {
            if (employee == null)                                  return "Contractor data is required";
            if (string.IsNullOrWhiteSpace(employee.name))          return "Name is required";
            if (employee.assignments == null || employee.assignments.Count == 0)
                                                                  return "At least one project must be assigned";
            if (employee.assignments.Count > 50)                   return "A contractor can be assigned to at most 50 projects";
            if (employee.assignments.Any(a => string.IsNullOrWhiteSpace(a.project_code)))
                                                                  return "Project is required for every assignment row";
            if (employee.assignments.Any(a => string.IsNullOrWhiteSpace(a.position)))
                                                                  return "Position is required";
            if (employee.assignments.GroupBy(a => a.project_code.Trim(), StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
                                                                  return "Each project can only be assigned once";
            if (string.IsNullOrWhiteSpace(employee.provider_code)) return "Provider code is required";
            return ValidateBirthdate(employee.birthdate);
        }

        /// <summary>
        /// Validates birthdate: must be present, not a future date, and the employee
        /// must be at least 18 years old (DOLE employment regulations).
        /// Moved verbatim from ContractorService.ValidateBirthdate.
        /// </summary>
        public static string? ValidateBirthdate(DateTime? birthdate)
        {
            if (birthdate == null)
            {
                return "Birthdate is required";
            }

            var dob = birthdate.Value.Date;
            var today = DateTime.Today;

            if (dob > today)
            {
                return "Birthdate cannot be a future date";
            }

            int age = today.Year - dob.Year;
            if (dob > today.AddYears(-age))
            {
                age--;
            }

            if (age < 18)
            {
                return "The employee is under 18 years old and is not eligible for employment under DOLE regulations.";
            }

            return null;
        }
    }
}
