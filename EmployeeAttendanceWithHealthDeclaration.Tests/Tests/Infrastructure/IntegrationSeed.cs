using Dapper;
using MySqlConnector;

namespace EmployeeAttendanceWithHealthDeclaration.Tests.Infrastructure
{
    /// <summary>
    /// Raw-SQL insert helpers for integration test seed data. Every code/id is
    /// TST-prefixed (picked up by IntegrationCleanup); dates are relative to
    /// Today/Now so tests never go stale. Reads always go through the SUT
    /// repositories — this class only inserts base rows.
    /// </summary>
    public static class IntegrationSeed
    {
        public const string ProviderCode = "TST";
        public const string ProjectCode = "TST-26-001";
        public const string SecondProjectCode = "TST-26-002";
        public const string EmployeeId = "TST-000001";
        public const string SecondEmployeeId = "TST-000002";

        public static async Task CreateProviderAsync(
            MySqlConnection connection,
            string provider_code = ProviderCode,
            string provider_name = "TST Provider Co",
            int active = 1)
        {
            // Idempotent: several tests share the same TST provider/project rows and
            // cleanup only runs at fixture start/end, so re-seeding must not collide.
            await connection.ExecuteAsync(
                @"INSERT INTO providers (provider_code, provider_name, provider_address, active)
                  VALUES (@provider_code, @provider_name, 'TST Address', @active)
                  ON DUPLICATE KEY UPDATE provider_code = provider_code",
                new { provider_code, provider_name, active });
        }

        public static async Task CreateProjectAsync(
            MySqlConnection connection,
            string project_code = ProjectCode,
            string provider_code = ProviderCode,
            int active = 1,
            int is_deleted = 0,
            DateTime? contract_startdate = null,
            DateTime? contract_enddate = null)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO project
                    (project_code, project_name, provider_code, provider_pic, provider_pic_number,
                     area_of_destination, contract_startdate, contract_enddate, active, is_deleted)
                  VALUES
                    (@project_code, 'TST Project', @provider_code, 'TST PIC', '09170000000',
                     'TST Area', @contract_startdate, @contract_enddate, @active, @is_deleted)
                  ON DUPLICATE KEY UPDATE project_code = project_code",
                new
                {
                    project_code,
                    provider_code,
                    contract_startdate = contract_startdate ?? DateTime.Today,
                    contract_enddate = contract_enddate ?? DateTime.Today.AddYears(1),
                    active,
                    is_deleted
                });
        }

        public static async Task CreateEmployeeAsync(
            MySqlConnection connection,
            string employee_id = EmployeeId,
            string provider_code = ProviderCode,
            string name = "TST Employee One",
            DateTime? birthdate = null,
            string gender = "Male",
            int active = 1,
            int is_deleted = 0)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO employee
                    (employee_id, name, gender, birthdate, contact_number, address, provider_code, active, is_deleted)
                  VALUES
                    (@employee_id, @name, @gender, @birthdate, '09170000000', 'TST Address', @provider_code, @active, @is_deleted)
                  ON DUPLICATE KEY UPDATE employee_id = employee_id",
                new
                {
                    employee_id,
                    name,
                    gender,
                    birthdate = birthdate ?? DateTime.Today.AddYears(-30),
                    provider_code,
                    active,
                    is_deleted
                });
        }

        public static async Task AssignProjectAsync(
            MySqlConnection connection,
            string employee_id = EmployeeId,
            string project_code = ProjectCode,
            string position = "Worker")
        {
            await connection.ExecuteAsync(
                @"INSERT INTO employee_project (employee_id, project_code, position)
                  VALUES (@employee_id, @project_code, @position)
                  ON DUPLICATE KEY UPDATE position = VALUES(position)",
                new { employee_id, project_code, position });
        }

        public static async Task<int> CreateTimeLogAsync(
            MySqlConnection connection,
            string employee_id = EmployeeId,
            DateTime? time_in = null,
            DateTime? time_out = null,
            string health_status = "FIT",
            string waiver_consent = "UNDERSTOOD")
        {
            return await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO time_logs (employee_id, time_in, time_out, health_status, waiver_consent)
                  VALUES (@employee_id, @time_in, @time_out, @health_status, @waiver_consent);
                  SELECT LAST_INSERT_ID();",
                new
                {
                    employee_id,
                    time_in = time_in ?? DateTime.Now.AddHours(-1),
                    time_out,
                    health_status,
                    waiver_consent
                });
        }

        public static async Task CreateConfigKeyAsync(
            MySqlConnection connection,
            string key,
            string value)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO `system_config` (`key`, `value`, `description`)
                  VALUES (@key, @value, 'TST integration test row')
                  ON DUPLICATE KEY UPDATE `value` = VALUES(`value`)",
                new { key, value });
        }
    }
}
