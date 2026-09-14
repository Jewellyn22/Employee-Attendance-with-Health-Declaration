using ContractorAttendanceWithHealthDeclaration.Models.Domain;

namespace ContractorAttendanceWithHealthDeclaration.Tests.TestDoubles.Builders
{
    /// <summary>
    /// Fluent builder for a fully valid contractor_employee (passes
    /// ValidationHelper.ValidateContractorEmployee). Tests mutate one aspect at a time.
    /// Provider/project codes use the "TST" test prefix so integration rows are
    /// distinguishable and cleanable; unit tests only rely on them being opaque strings.
    /// </summary>
    public class ContractorBuilder
    {
        private readonly contractor_employee _employee = new()
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
            assignments = new List<contractor_project_assignment>
            {
                new() { project_code = "TST-26-001", position = "Worker" }
            }
        };

        public ContractorBuilder WithEmployeeId(string employee_id) { _employee.employee_id = employee_id; return this; }
        public ContractorBuilder WithName(string name) { _employee.name = name; return this; }
        public ContractorBuilder WithGender(string gender) { _employee.gender = gender; return this; }
        public ContractorBuilder WithBirthdate(DateTime? birthdate) { _employee.birthdate = birthdate; return this; }
        public ContractorBuilder WithContactNumber(string contact_number) { _employee.contact_number = contact_number; return this; }
        public ContractorBuilder WithAddress(string address) { _employee.address = address; return this; }
        public ContractorBuilder WithProviderCode(string provider_code) { _employee.provider_code = provider_code; return this; }
        public ContractorBuilder AsInactive() { _employee.active = 0; return this; }
        public ContractorBuilder AsDeleted() { _employee.is_deleted = 1; return this; }

        public ContractorBuilder WithAssignments(List<contractor_project_assignment> assignments)
        {
            _employee.assignments = assignments;
            _employee.active_project_count = assignments.Count;
            return this;
        }

        public ContractorBuilder WithAssignments(params (string project_code, string position)[] rows)
        {
            _employee.assignments = rows.Select(r => new contractor_project_assignment
            {
                project_code = r.project_code,
                position = r.position
            }).ToList();
            _employee.active_project_count = _employee.assignments.Count;
            return this;
        }

        public ContractorBuilder WithoutAssignments()
        {
            _employee.assignments = null;
            _employee.active_project_count = 0;
            return this;
        }

        // Read-derived fields (hydrated by the repository on DB reads; the merge logic
        // parses project_positions when re-saving an existing account).
        public ContractorBuilder WithActiveProjectCount(int count) { _employee.active_project_count = count; return this; }
        public ContractorBuilder WithProjectPositions(string project_positions) { _employee.project_positions = project_positions; return this; }
        public ContractorBuilder WithProjectCodesCsv(string project_codes) { _employee.project_codes = project_codes; return this; }
        public ContractorBuilder WithOtherActiveProjectCount(int count) { _employee.other_active_project_count = count; return this; }

        public contractor_employee Build() => _employee;

        public static implicit operator contractor_employee(ContractorBuilder builder) => builder.Build();
    }
}
