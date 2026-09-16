using Dapper;
using MySqlConnector;

namespace EmployeeAttendanceWithHealthDeclaration.Tests.Infrastructure
{
    /// <summary>
    /// Deletes every TST-prefixed test row in FK-safe order (children before parents).
    /// This is the ONLY hand-written DELETE in the suite and the only cleanup mechanism —
    /// it never touches schema objects, the dev helper procedure, or non-TST data.
    /// audit_log rows are matched by TST reference/actor ids, plus bulk-delete summary
    /// rows ("N employees") created during this fixture's lifetime.
    /// </summary>
    public static class IntegrationCleanup
    {
        public static async Task DeleteTestRowsAsync(string connectionString, DateTime startedAtUtc)
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // Children first so foreign keys never block a delete.
            await connection.ExecuteAsync(
                "DELETE FROM time_logs WHERE employee_id LIKE 'TST%'");

            await connection.ExecuteAsync(
                "DELETE FROM employee_project WHERE employee_id LIKE 'TST%' OR project_code LIKE 'TST%'");

            await connection.ExecuteAsync(
                "DELETE FROM employee WHERE employee_id LIKE 'TST%' OR provider_code LIKE 'TST%'");

            await connection.ExecuteAsync(
                "DELETE FROM project WHERE project_code LIKE 'TST%' OR provider_code LIKE 'TST%'");

            await connection.ExecuteAsync(
                "DELETE FROM providers WHERE provider_code LIKE 'TST%'");

            // `key` is a reserved word — backticks required.
            await connection.ExecuteAsync(
                "DELETE FROM `system_config` WHERE `key` LIKE 'TST%'");

            await connection.ExecuteAsync(
                @"DELETE FROM `audit_log`
                  WHERE reference_id LIKE 'TST%'
                     OR updated_by LIKE 'TST%'
                     OR (reference_id LIKE '% employees' AND created_at >= @since)",
                new { since = startedAtUtc });
        }
    }
}
