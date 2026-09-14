using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Xunit;

namespace ContractorAttendanceWithHealthDeclaration.Tests.Infrastructure
{
    /// <summary>
    /// Shared fixture for the "database" collection. Connects to the real
    /// contractor_attendance database from the output-copy of appsettings.json
    /// (override with the CONTRACTORATT_TEST_CONNECTION_STRING environment variable).
    ///
    /// SAFETY RULES (per the approved plan):
    ///   - schema (tables / stored procedures / functions / events) is NEVER modified;
    ///   - row writes are confined to TST-prefixed rows;
    ///   - all TST rows are deleted on fixture start (crash leftovers) and again on
    ///     dispose, so the database is left exactly as found.
    /// </summary>
    public class DatabaseCollectionFixture : IAsyncLifetime
    {
        private const string ConnectionStringEnvVar = "CONTRACTORATT_TEST_CONNECTION_STRING";

        /// <summary>Wall-clock mark taken before any test runs; audit_log cleanup uses it.</summary>
        public DateTime StartedAtUtc { get; } = DateTime.UtcNow;

        public string ConnectionString { get; } = ResolveConnectionString();

        public async Task InitializeAsync()
        {
            // Clear leftovers from a previously crashed run before any test executes.
            await IntegrationCleanup.DeleteTestRowsAsync(ConnectionString, StartedAtUtc);
        }

        public async Task DisposeAsync()
        {
            // Leave the database exactly as it was found.
            await IntegrationCleanup.DeleteTestRowsAsync(ConnectionString, StartedAtUtc);
        }

        /// <summary>Fresh, caller-owned connection per test (repos take an IDbConnection).</summary>
        public MySqlConnection CreateConnection()
        {
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        private static string ResolveConnectionString()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return fromEnvironment;
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            return configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "No connection string found. Set ConnectionStrings:DefaultConnection in appsettings.json "
                    + $"or the {ConnectionStringEnvVar} environment variable.");
        }
    }
}
