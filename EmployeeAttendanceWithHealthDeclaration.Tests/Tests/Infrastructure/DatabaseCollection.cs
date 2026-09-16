using EmployeeAttendanceWithHealthDeclaration.Tests.Infrastructure;
using Xunit;

namespace EmployeeAttendanceWithHealthDeclaration.Tests.Infrastructure
{
    /// <summary>
    /// All repository integration tests join this collection so they hit the real
    /// employee_attendance database strictly sequentially (xUnit runs one test at
    /// a time inside a collection). The fixture wipes TST-prefixed test rows before
    /// and after the whole run — schema objects are never touched.
    /// </summary>
    [CollectionDefinition("database")]
    public class DatabaseCollection : ICollectionFixture<DatabaseCollectionFixture>
    {
    }
}
