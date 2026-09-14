using ContractorAttendanceWithHealthDeclaration.Helpers;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Tests.TestDoubles.Builders;
using Xunit;

namespace ContractorAttendanceWithHealthDeclaration.Tests.Helpers
{
    public class ValidationHelperTests
    {
        #region ValidateHealthStatus

        [Theory]
        [InlineData(HealthConstants.StatusFit)]
        [InlineData(HealthConstants.StatusUnfit)]
        public void ValidateHealthStatus_valid_values_return_null(string health_status)
        {
            Assert.Null(ValidationHelper.ValidateHealthStatus(health_status));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("fit")]
        [InlineData("FIT ")]
        [InlineData("SICK")]
        public void ValidateHealthStatus_invalid_values_return_exact_message(string? health_status)
        {
            Assert.Equal("Invalid health status. Must be 'FIT' or 'UNFIT'",
                ValidationHelper.ValidateHealthStatus(health_status));
        }

        #endregion

        #region ValidateWaiverConsent

        [Theory]
        [InlineData(HealthConstants.WaiverUnderstood)]
        [InlineData(HealthConstants.WaiverNotUnderstood)]
        public void ValidateWaiverConsent_valid_values_return_null(string waiver_consent)
        {
            Assert.Null(ValidationHelper.ValidateWaiverConsent(waiver_consent));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("understood")]
        [InlineData("MAYBE")]
        public void ValidateWaiverConsent_invalid_values_return_exact_message(string? waiver_consent)
        {
            Assert.Equal("Invalid waiver consent. Must be 'UNDERSTOOD' or 'NOT_UNDERSTOOD'",
                ValidationHelper.ValidateWaiverConsent(waiver_consent));
        }

        #endregion

        #region ValidateContractorEmployee — ordered first-error-wins chain

        [Fact]
        public void Null_employee_returns_contractor_data_required()
        {
            Assert.Equal("Contractor data is required", ValidationHelper.ValidateContractorEmployee(null));
        }

        [Fact]
        public void Blank_name_returns_name_required_before_assignment_checks()
        {
            // Name wins over the missing assignments error (order pin).
            var employee = new ContractorBuilder()
                .WithName("   ")
                .WithoutAssignments()
                .Build();

            Assert.Equal("Name is required", ValidationHelper.ValidateContractorEmployee(employee));
        }

        [Fact]
        public void Null_assignments_returns_at_least_one_project()
        {
            var employee = new ContractorBuilder().WithoutAssignments().Build();

            Assert.Equal("At least one project must be assigned",
                ValidationHelper.ValidateContractorEmployee(employee));
        }

        [Fact]
        public void Empty_assignments_list_returns_at_least_one_project()
        {
            var employee = new ContractorBuilder().WithAssignments(new List<contractor_project_assignment>()).Build();

            Assert.Equal("At least one project must be assigned",
                ValidationHelper.ValidateContractorEmployee(employee));
        }

        [Fact]
        public void Fifty_one_assignments_return_max_fifty_projects()
        {
            var employee = new ContractorBuilder()
                .WithAssignments(Enumerable.Range(1, 51)
                    .Select(i => new contractor_project_assignment { project_code = $"P-{i:D3}", position = "Worker" })
                    .ToList())
                .Build();

            Assert.Equal("A contractor can be assigned to at most 50 projects",
                ValidationHelper.ValidateContractorEmployee(employee));
        }

        [Fact]
        public void Exactly_fifty_valid_assignments_are_accepted()
        {
            var employee = new ContractorBuilder()
                .WithAssignments(Enumerable.Range(1, 50)
                    .Select(i => new contractor_project_assignment { project_code = $"P-{i:D3}", position = "Worker" })
                    .ToList())
                .Build();

            Assert.Null(ValidationHelper.ValidateContractorEmployee(employee));
        }

        [Fact]
        public void Blank_project_code_wins_over_blank_position()
        {
            var employee = new ContractorBuilder()
                .WithAssignments(new List<contractor_project_assignment>
                {
                    new() { project_code = "  ", position = "  " }
                })
                .Build();

            Assert.Equal("Project is required for every assignment row",
                ValidationHelper.ValidateContractorEmployee(employee));
        }

        [Fact]
        public void Blank_position_returns_position_required()
        {
            var employee = new ContractorBuilder()
                .WithAssignments(new List<contractor_project_assignment>
                {
                    new() { project_code = "TST-26-001", position = " " }
                })
                .Build();

            Assert.Equal("Position is required", ValidationHelper.ValidateContractorEmployee(employee));
        }

        [Fact]
        public void Duplicate_project_codes_ignoring_case_and_whitespace_are_rejected()
        {
            var employee = new ContractorBuilder()
                .WithAssignments(new List<contractor_project_assignment>
                {
                    new() { project_code = "TST-26-001", position = "Worker" },
                    new() { project_code = " tst-26-001 ", position = "Supervisor" }
                })
                .Build();

            Assert.Equal("Each project can only be assigned once",
                ValidationHelper.ValidateContractorEmployee(employee));
        }

        [Fact]
        public void Blank_provider_code_returns_provider_code_required()
        {
            var employee = new ContractorBuilder().WithProviderCode("").Build();

            Assert.Equal("Provider code is required", ValidationHelper.ValidateContractorEmployee(employee));
        }

        [Fact]
        public void Birthdate_is_validated_last_in_the_chain()
        {
            // Everything else valid; only the birthdate is missing -> birthdate error proves ordering.
            var employee = new ContractorBuilder().WithBirthdate(null).Build();

            Assert.Equal("Birthdate is required", ValidationHelper.ValidateContractorEmployee(employee));
        }

        [Fact]
        public void Fully_valid_employee_returns_null()
        {
            Assert.Null(ValidationHelper.ValidateContractorEmployee(new ContractorBuilder().Build()));
        }

        #endregion

        #region ValidateBirthdate

        [Fact]
        public void Null_birthdate_returns_required_message()
        {
            Assert.Equal("Birthdate is required", ValidationHelper.ValidateBirthdate(null));
        }

        [Fact]
        public void Future_birthdate_returns_future_message_even_if_under_18()
        {
            Assert.Equal("Birthdate cannot be a future date",
                ValidationHelper.ValidateBirthdate(DateTime.Today.AddDays(1)));
        }

        [Fact]
        public void Eighteenth_birthday_today_is_eligible()
        {
            Assert.Null(ValidationHelper.ValidateBirthdate(DateTime.Today.AddYears(-18)));
        }

        [Fact]
        public void Day_after_eighteenth_birthday_is_underage()
        {
            Assert.Equal("The employee is under 18 years old and is not eligible for employment under DOLE regulations.",
                ValidationHelper.ValidateBirthdate(DateTime.Today.AddYears(-18).AddDays(1)));
        }

        [Fact]
        public void Seventeen_year_old_is_underage()
        {
            Assert.Equal("The employee is under 18 years old and is not eligible for employment under DOLE regulations.",
                ValidationHelper.ValidateBirthdate(DateTime.Today.AddYears(-17)));
        }

        [Fact]
        public void Adult_birthdate_is_eligible()
        {
            Assert.Null(ValidationHelper.ValidateBirthdate(DateTime.Today.AddYears(-30)));
        }

        #endregion
    }
}
