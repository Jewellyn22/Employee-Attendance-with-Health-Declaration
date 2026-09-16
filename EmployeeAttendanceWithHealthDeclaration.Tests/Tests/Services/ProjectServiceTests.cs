using EmployeeAttendanceWithHealthDeclaration.Models;
using EmployeeAttendanceWithHealthDeclaration.Models.Domain;
using EmployeeAttendanceWithHealthDeclaration.Repositories;
using EmployeeAttendanceWithHealthDeclaration.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace EmployeeAttendanceWithHealthDeclaration.Tests.Services
{
    public class ProjectServiceTests
    {
        private const string Admin = "ADMIN-1";

        private sealed record AuditCall(
            string Entity, string Action, string ReferenceId, object? DataFrom, object? DataTo, string UpdatedBy);

        private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
        private readonly IProviderRepository _providerRepository = Substitute.For<IProviderRepository>();
        private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
        private readonly IEmployeeService _employeeService = Substitute.For<IEmployeeService>();
        private readonly List<AuditCall> _auditCalls = new();

        private ProjectService CreateService() => new(
            _projectRepository, _providerRepository, _auditLogService, _employeeService,
            NullLogger<ProjectService>.Instance);

        public ProjectServiceTests()
        {
            _auditLogService.Log(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<string>())
                .Returns(c =>
                {
                    _auditCalls.Add(new AuditCall(
                        c.ArgAt<string>(0), c.ArgAt<string>(1), c.ArgAt<string>(2),
                        c.ArgAt<object?>(3), c.ArgAt<object?>(4), c.ArgAt<string>(5)));
                    return new Response<audit_log> { Success = true, Data = new audit_log { log_id = 42 } };
                });
        }

        private static project ValidProject(int active = 1)
        {
            return new project
            {
                project_code = "TST-26-001",
                project_name = "Test Project",
                provider_code = "TST",
                provider_name = "Test Provider Co",
                provider_pic = "Maria PIC",
                provider_pic_number = "09171112222",
                area_of_destination = "Plant Area A",
                contract_startdate = DateTime.Today,
                contract_enddate = DateTime.Today.AddYears(1),
                active = active
            };
        }

        #region Reads

        [Fact]
        public async Task GetByProjectCode_found_returns_project()
        {
            var project = ValidProject();
            _projectRepository.GetByProjectCode("TST-26-001").Returns(project);

            var result = await CreateService().GetByProjectCode("TST-26-001");

            Assert.True(result.Success);
            Assert.Same(project, result.Data);
        }

        [Fact]
        public async Task GetByProjectCode_missing_returns_not_found()
        {
            _projectRepository.GetByProjectCode("TST-26-001").Returns((project?)null);

            var result = await CreateService().GetByProjectCode("TST-26-001");

            Assert.False(result.Success);
            Assert.Equal("Project not found", result.Message);
        }

        [Fact]
        public async Task GetByProjectCode_repository_failure_returns_error_envelope()
        {
            _projectRepository.GetByProjectCode("TST-26-001").ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetByProjectCode("TST-26-001");

            Assert.False(result.Success);
            Assert.Equal("Error retrieving project", result.Message);
        }

        [Fact]
        public async Task GetAll_passes_repository_data_through()
        {
            var data = new[] { ValidProject() };
            _projectRepository.GetAll().Returns(data);

            var result = await CreateService().GetAll();

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        [Fact]
        public async Task GetAll_repository_failure_returns_error_envelope()
        {
            _projectRepository.GetAll().ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetAll();

            Assert.False(result.Success);
            Assert.Equal("Error retrieving projects", result.Message);
        }

        [Fact]
        public async Task GetByProviderCode_unknown_provider_is_rejected()
        {
            _providerRepository.GetByProviderCode("TST").Returns((provider?)null);

            var result = await CreateService().GetByProviderCode("TST");

            Assert.False(result.Success);
            Assert.Equal("Provider not found", result.Message);
        }

        [Fact]
        public async Task GetByProviderCode_passes_repository_data_through()
        {
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            var data = new[] { ValidProject() };
            _projectRepository.GetByProviderCode("TST").Returns(data);

            var result = await CreateService().GetByProviderCode("TST");

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        #endregion

        #region Create

        [Fact]
        public async Task Create_blank_provider_name_is_rejected_first()
        {
            var submitted = ValidProject();
            submitted.provider_name = " ";

            var result = await CreateService().Create(submitted, Admin);

            Assert.Equal("Provider name is required", result.Message);
        }

        [Fact]
        public async Task Create_validation_chain_is_ordered()
        {
            // Only provider_name filled: the FIRST missing field in chain order wins.
            var cases = new (string? provider_pic, string? pic_number, string? project_name, string? area, DateTime? start, DateTime? end, string expected)[]
            {
                (null, "0917", "Name", "Area", DateTime.Today, DateTime.Today, "Provider Project PIC is required"),
                ("PIC", null, "Name", "Area", DateTime.Today, DateTime.Today, "Contact No is required"),
                ("PIC", "0917", null, "Area", DateTime.Today, DateTime.Today, "Project name is required"),
                ("PIC", "0917", "Name", null, DateTime.Today, DateTime.Today, "Area of Destination is required"),
                ("PIC", "0917", "Name", "Area", null, DateTime.Today, "Contract start date is required"),
                ("PIC", "0917", "Name", "Area", DateTime.Today, null, "Contract end date is required"),
                ("PIC", "0917", "Name", "Area", DateTime.Today.AddDays(1), DateTime.Today, "Contract Start Date cannot be later than Contract End Date"),
            };

            foreach (var (pic, number, name, area, start, end, expected) in cases)
            {
                var submitted = ValidProject();
                submitted.provider_pic = pic!;
                submitted.provider_pic_number = number!;
                submitted.project_name = name!;
                submitted.area_of_destination = area!;
                submitted.contract_startdate = start;
                submitted.contract_enddate = end;

                var result = await CreateService().Create(submitted, Admin);

                Assert.Equal(expected, result.Message);
            }
        }

        [Fact]
        public async Task Create_unknown_provider_is_rejected()
        {
            var submitted = ValidProject();
            _providerRepository.GetByProviderCode("TST").Returns((provider?)null);

            var result = await CreateService().Create(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Provider not found", result.Message);
        }

        [Fact]
        public async Task Create_already_expired_contract_enddate_is_rejected()
        {
            var submitted = ValidProject();
            submitted.contract_startdate = DateTime.Today.AddYears(-1);
            submitted.contract_enddate = DateTime.Today.AddDays(-1);
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });

            var result = await CreateService().Create(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Contract End Date cannot be earlier than the current date.", result.Message);
            await _projectRepository.DidNotReceive().Create(Arg.Any<project>());
        }

        [Fact]
        public async Task Create_contract_ending_today_is_allowed()
        {
            var submitted = ValidProject();
            submitted.contract_enddate = DateTime.Today;
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            _projectRepository.Create(Arg.Any<project>()).Returns(c => c.Arg<project>());

            var result = await CreateService().Create(submitted, Admin);

            Assert.True(result.Success, result.Message);
        }

        [Fact]
        public async Task Create_repository_null_reports_creation_failed_without_audit()
        {
            var submitted = ValidProject();
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            _projectRepository.Create(Arg.Any<project>()).Returns((project)null!);

            var result = await CreateService().Create(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Project creation failed", result.Message);
            Assert.Null(result.Data);
            Assert.Empty(_auditCalls);
        }

        [Fact]
        public async Task Create_happy_path_audits_create()
        {
            var submitted = ValidProject();
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            _projectRepository.Create(Arg.Any<project>()).Returns(c => c.Arg<project>());

            var result = await CreateService().Create(submitted, Admin);

            Assert.True(result.Success, result.Message);
            Assert.Equal("Project created successfully", result.Message);
            var audit = _auditCalls.Single();
            Assert.Equal("project", audit.Entity);
            Assert.Equal("create", audit.Action);
            Assert.Equal("TST-26-001", audit.ReferenceId);
            Assert.Null(audit.DataFrom);
        }

        [Fact]
        public async Task Create_repository_failure_returns_error_envelope()
        {
            var submitted = ValidProject();
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            _projectRepository.Create(Arg.Any<project>()).ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().Create(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Error creating project", result.Message);
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_validation_error_is_returned_before_lookup()
        {
            var submitted = ValidProject();
            submitted.project_name = "";

            var result = await CreateService().Update(submitted, Admin);

            Assert.Equal("Project name is required", result.Message);
            await _projectRepository.DidNotReceive().GetByProjectCode(Arg.Any<string>());
        }

        [Fact]
        public async Task Update_unknown_provider_is_rejected()
        {
            var submitted = ValidProject();
            _providerRepository.GetByProviderCode("TST").Returns((provider?)null);

            var result = await CreateService().Update(submitted, Admin);

            Assert.Equal("Provider not found", result.Message);
        }

        [Fact]
        public async Task Update_unknown_project_is_rejected()
        {
            var submitted = ValidProject();
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            _projectRepository.GetByProjectCode("TST-26-001").Returns((project?)null);

            var result = await CreateService().Update(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Project not found", result.Message);
        }

        [Fact]
        public async Task Update_cannot_reactivate_a_project_with_an_expired_contract()
        {
            var submitted = ValidProject(active: 1);
            submitted.contract_startdate = DateTime.Today.AddYears(-1);
            submitted.contract_enddate = DateTime.Today.AddDays(-5);
            var existing = ValidProject(active: 0);
            existing.contract_startdate = DateTime.Today.AddYears(-1);
            existing.contract_enddate = DateTime.Today.AddDays(-5);
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            _projectRepository.GetByProjectCode("TST-26-001").Returns(existing);

            var result = await CreateService().Update(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Cannot set the project to Active: the contract end date is already expired. Extend the contract end date first.",
                result.Message);
        }

        // PIN: deactivating a project whose contract is already expired is allowed —
        // only the Active direction is blocked.
        [Fact]
        public async Task Update_can_deactivate_a_project_with_an_expired_contract()
        {
            var submitted = ValidProject(active: 0);
            submitted.contract_startdate = DateTime.Today.AddYears(-1);
            submitted.contract_enddate = DateTime.Today.AddDays(-5);
            var existing = ValidProject(active: 1);
            existing.contract_startdate = DateTime.Today.AddYears(-1);
            existing.contract_enddate = DateTime.Today.AddDays(-5);
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            _projectRepository.GetByProjectCode("TST-26-001").Returns(existing);
            _projectRepository.Update(Arg.Any<project>()).Returns(c => c.Arg<project>());
            _employeeService.CascadeDeactivateByProject("TST-26-001", Admin)
                .Returns(new Response<int> { Success = true, Data = 0 });

            var result = await CreateService().Update(submitted, Admin);

            Assert.True(result.Success, result.Message);
        }

        [Fact]
        public async Task Update_without_status_change_skips_the_cascades()
        {
            var submitted = ValidProject(active: 1);
            var existing = ValidProject(active: 1);
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            _projectRepository.GetByProjectCode("TST-26-001").Returns(existing);
            _projectRepository.Update(Arg.Any<project>()).Returns(c => c.Arg<project>());

            var result = await CreateService().Update(submitted, Admin);

            Assert.True(result.Success);
            Assert.Equal("Project updated successfully", result.Message);
            await _employeeService.DidNotReceive().CascadeDeactivateByProject(Arg.Any<string>(), Arg.Any<string>());
            await _employeeService.DidNotReceive().CascadeReactivateByProject(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task Update_deactivation_cascades_and_appends_the_count()
        {
            var submitted = ValidProject(active: 0);
            var existing = ValidProject(active: 1);
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            _projectRepository.GetByProjectCode("TST-26-001").Returns(existing);
            _projectRepository.Update(Arg.Any<project>()).Returns(c => c.Arg<project>());
            _employeeService.CascadeDeactivateByProject("TST-26-001", Admin)
                .Returns(new Response<int> { Success = true, Data = 2 });

            var result = await CreateService().Update(submitted, Admin);

            Assert.True(result.Success);
            Assert.Equal("Project updated successfully - 2 employee(s) deactivated", result.Message);
            var audit = _auditCalls.Single();
            Assert.Equal("update", audit.Action);
            Assert.Same(existing, audit.DataFrom);
        }

        [Fact]
        public async Task Update_reactivation_restores_cascade_deactivated_employees()
        {
            var submitted = ValidProject(active: 1);
            var existing = ValidProject(active: 0);
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            _projectRepository.GetByProjectCode("TST-26-001").Returns(existing);
            _projectRepository.Update(Arg.Any<project>()).Returns(c => c.Arg<project>());
            _employeeService.CascadeReactivateByProject("TST-26-001", Admin)
                .Returns(new Response<int> { Success = true, Data = 3 });

            var result = await CreateService().Update(submitted, Admin);

            Assert.True(result.Success);
            Assert.Equal("Project updated successfully - 3 employee(s) re-activated", result.Message);
        }

        [Fact]
        public async Task Update_repository_null_reports_failure_without_audit_or_cascade()
        {
            var submitted = ValidProject(active: 0);
            var existing = ValidProject(active: 1);
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            _projectRepository.GetByProjectCode("TST-26-001").Returns(existing);
            _projectRepository.Update(Arg.Any<project>()).Returns((project)null!);

            var result = await CreateService().Update(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Project update failed", result.Message);
            Assert.Empty(_auditCalls);
            await _employeeService.DidNotReceive().CascadeDeactivateByProject(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task Update_repository_failure_returns_error_envelope()
        {
            var submitted = ValidProject();
            _providerRepository.GetByProviderCode("TST").Returns(new provider { provider_code = "TST", active = 1 });
            _projectRepository.GetByProjectCode("TST-26-001").Returns(ValidProject());
            _projectRepository.Update(Arg.Any<project>()).ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().Update(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Error updating project", result.Message);
        }

        #endregion

        #region SetInactive / Delete / GetActiveCount

        [Fact]
        public async Task SetInactive_passes_repository_result_through()
        {
            _projectRepository.SetInactive("TST-26-001").Returns(false);

            var result = await CreateService().SetInactive("TST-26-001");

            Assert.True(result.Success);
            Assert.False(result.Data);
        }

        [Fact]
        public async Task SetInactive_repository_failure_returns_error_envelope()
        {
            _projectRepository.SetInactive("TST-26-001").ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().SetInactive("TST-26-001");

            Assert.False(result.Success);
            Assert.Equal("Error setting project inactive", result.Message);
        }

        [Fact]
        public async Task Delete_unknown_project_is_rejected()
        {
            _projectRepository.GetByProjectCode("TST-26-001").Returns((project?)null);

            var result = await CreateService().Delete("TST-26-001", Admin);

            Assert.False(result.Success);
            Assert.Equal("Project not found", result.Message);
        }

        [Fact]
        public async Task Delete_reports_failure_when_the_repository_returns_null()
        {
            _projectRepository.GetByProjectCode("TST-26-001").Returns(ValidProject());
            _projectRepository.Delete("TST-26-001").Returns((project_delete_result?)null);

            var result = await CreateService().Delete("TST-26-001", Admin);

            Assert.False(result.Success);
            Assert.Equal("Project delete failed", result.Message);
        }

        [Fact]
        public async Task Delete_reports_failure_when_the_sp_reports_failure()
        {
            _projectRepository.GetByProjectCode("TST-26-001").Returns(ValidProject());
            _projectRepository.Delete("TST-26-001")
                .Returns(new project_delete_result { success = false, deleted_employees = 0 });

            var result = await CreateService().Delete("TST-26-001", Admin);

            Assert.False(result.Success);
            Assert.Equal("Project delete failed", result.Message);
        }

        [Fact]
        public async Task Delete_happy_path_audits_with_deleted_employee_count()
        {
            var existing = ValidProject();
            _projectRepository.GetByProjectCode("TST-26-001").Returns(existing);
            _projectRepository.Delete("TST-26-001")
                .Returns(new project_delete_result { success = true, deleted_employees = 3 });

            var result = await CreateService().Delete("TST-26-001", Admin);

            Assert.True(result.Success);
            Assert.Equal("Project deleted successfully (3 enrolled employee(s) also removed)", result.Message);
            var audit = _auditCalls.Single();
            Assert.Equal("delete", audit.Action);
            Assert.Same(existing, audit.DataFrom);
            // data_to = { deleted_employees: 3 }
            var dataTo = Assert.IsType<int>(audit.DataTo?.GetType().GetProperty("deleted_employees")!.GetValue(audit.DataTo));
            Assert.Equal(3, dataTo);
        }

        [Fact]
        public async Task Delete_repository_failure_returns_error_envelope()
        {
            _projectRepository.GetByProjectCode("TST-26-001").Returns(ValidProject());
            _projectRepository.Delete("TST-26-001").ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().Delete("TST-26-001", Admin);

            Assert.False(result.Success);
            Assert.Equal("Error deleting project", result.Message);
        }

        [Fact]
        public async Task GetActiveCount_passes_through_and_tolerates_failures()
        {
            _projectRepository.GetActiveCount().Returns(7);
            var ok = await CreateService().GetActiveCount();
            Assert.True(ok.Success);
            Assert.Equal(7, ok.Data);

            _projectRepository.GetActiveCount().ThrowsAsync(new InvalidOperationException("db down"));
            var failed = await CreateService().GetActiveCount();
            Assert.False(failed.Success);
            Assert.Equal(0, failed.Data);
        }

        #endregion
    }
}
