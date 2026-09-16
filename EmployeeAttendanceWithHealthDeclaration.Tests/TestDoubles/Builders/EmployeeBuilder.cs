using EmployeeAttendanceWithHealthDeclaration.Models.Domain;

namespace EmployeeAttendanceWithHealthDeclaration.Tests.TestDoubles.Builders
{
    /// <summary>
    /// Fluent builder for a fully valid employee (passes
    /// ValidationHelper.ValidateEmployee). Tests mutate one aspect at a time.
    /// Provider/project codes use the "TST" test prefix so integration rows are
    /// distinguishable and cleanable; unit tests only rely on them being opaque strings.
    /// </summary>
    public class EmployeeBuilder
    {
        private readonly employee _employee = new()
        {
            employee_id = "TST-000001",
            name = "Juan Dela Cruz",
            gender = "Male",
            birthdate = DateTime.Today.AddYears(-30),
            contact_number = "09171234567",
            address = "123 Test Street",
            provider_code = "TST",
            active = 1,
            is_deleted = 0,
            // Mirrors DB hydration: one active assignment -> active_project_count = 1
            // (the kiosk TIME IN gate reads this field, not the assignments list).
            active_project_count = 1,
            assignments = new List<employee_project_assignment>
            {
                new() { project_code = "TST-26-001", position = "Worker" }
            }
        };

        public EmployeeBuilder WithEmployeeId(string employee_id) { _employee.employee_id = employee_id; return this; }
        public EmployeeBuilder WithName(string name) { _employee.name = name; return this; }
        public EmployeeBuilder WithGender(string gender) { _employee.gender = gender; return this; }
        public EmployeeBuilder WithBirthdate(DateTime? birthdate) { _employee.birthdate = birthdate; return this; }
        public EmployeeBuilder WithContactNumber(string contact_number) { _employee.contact_number = contact_number; return this; }
        public EmployeeBuilder WithAddress(string address) { _employee.address = address; return this; }
        public EmployeeBuilder WithProviderCode(string provider_code) { _employee.provider_code = provider_code; return this; }
        public EmployeeBuilder AsInactive() { _employee.active = 0; return this; }
        public EmployeeBuilder AsDeleted() { _employee.is_deleted = 1; return this; }

        public EmployeeBuilder WithAssignments(List<employee_project_assignment> assignments)
        {
            _employee.assignments = assignments;
            _employee.active_project_count = assignments.Count;
            return this;
        }

        public EmployeeBuilder WithAssignments(params (string project_code, string position)[] rows)
        {
            _employee.assignments = rows.Select(r => new employee_project_assignment
            {
                project_code = r.project_code,
                position = r.position
            }).ToList();
            _employee.active_project_count = _employee.assignments.Count;
            return this;
        }

        public EmployeeBuilder WithoutAssignments()
        {
            _employee.assignments = null;
            _employee.active_project_count = 0;
            return this;
        }

        // Read-derived fields (hydrated by the repository on DB reads; the merge logic
        // parses project_positions when re-saving an existing account).
        public EmployeeBuilder WithActiveProjectCount(int count) { _employee.active_project_count = count; return this; }
        public EmployeeBuilder WithProjectPositions(string project_positions) { _employee.project_positions = project_positions; return this; }
        public EmployeeBuilder WithProjectCodesCsv(string project_codes) { _employee.project_codes = project_codes; return this; }
        public EmployeeBuilder WithOtherActiveProjectCount(int count) { _employee.other_active_project_count = count; return this; }

        public employee Build() => _employee;

        public static implicit operator employee(EmployeeBuilder builder) => builder.Build();
    }
}
