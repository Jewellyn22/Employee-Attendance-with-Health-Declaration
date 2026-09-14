using ContractorAttendanceWithHealthDeclaration.Helpers;
using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;
using ContractorAttendanceWithHealthDeclaration.Services;
using ContractorAttendanceWithHealthDeclaration.Tests.TestDoubles.Builders;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ContractorAttendanceWithHealthDeclaration.Tests.Services
{
    public class ContractorServiceTests
    {
        private const string Admin = "ADMIN-1";

        private sealed record AuditCall(
            string Entity, string Action, string ReferenceId, object? DataFrom, object? DataTo, string UpdatedBy);

        private readonly IContractorEmployeeRepository _contractorRepository = Substitute.For<IContractorEmployeeRepository>();
        private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
        private readonly IProviderRepository _providerRepository = Substitute.For<IProviderRepository>();
        private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
        private readonly List<AuditCall> _auditCalls = new();

        private ContractorService CreateService() => new(
            _contractorRepository, _projectRepository, _providerRepository, _auditLogService,
            NullLogger<ContractorService>.Instance);

        public ContractorServiceTests()
        {
            // Every audit Log call is captured for assertions; returns a fixed row
            // (BulkCreate reads Data.log_id).
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
            _auditLogService.GetByEntity(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new Response<IEnumerable<audit_log>> { Success = true, Data = Array.Empty<audit_log>() });

            _contractorRepository.SetQrCode(Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);
        }

        // ---- shared stubs ---------------------------------------------------------

        private void StubProject(string project_code, string provider_code = "TST") =>
            _projectRepository.GetByProjectCode(project_code)
                .Returns(new project { project_code = project_code, provider_code = provider_code, active = 1 });

        private void StubProvider(string provider_code = "TST") =>
            _providerRepository.GetByProviderCode(provider_code)
                .Returns(new provider { provider_code = provider_code, active = 1 });

        private List<contractor_employee> CaptureCreates()
        {
            var captured = new List<contractor_employee>();
            _contractorRepository.Create(Arg.Do<contractor_employee>(e => captured.Add(e)))
                .Returns(c => c.Arg<contractor_employee>());
            return captured;
        }

        private List<contractor_employee> CaptureUpdates()
        {
            var captured = new List<contractor_employee>();
            _contractorRepository.Update(Arg.Do<contractor_employee>(e => captured.Add(e)))
                .Returns(c => c.Arg<contractor_employee>());
            return captured;
        }

        private static contractor_employee ExistingWithPositions(string project_positions, string employee_id = "TST-000009")
        {
            return new ContractorBuilder()
                .WithEmployeeId(employee_id)
                .WithName("Juan Dela Cruz")
                .WithProjectPositions(project_positions)
                .Build();
        }

        #region Reads (pass-through)

        [Fact]
        public async Task GetByEmployeeId_found_returns_contractor()
        {
            var contractor = new ContractorBuilder().Build();
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(contractor);

            var result = await CreateService().GetByEmployeeId("TST-000001");

            Assert.True(result.Success);
            Assert.Same(contractor, result.Data);
        }

        [Fact]
        public async Task GetByEmployeeId_missing_returns_not_found()
        {
            _contractorRepository.GetByEmployeeId("TST-000001").Returns((contractor_employee?)null);

            var result = await CreateService().GetByEmployeeId("TST-000001");

            Assert.False(result.Success);
            Assert.Equal("Contractor not found", result.Message);
        }

        [Fact]
        public async Task GetByEmployeeId_repository_failure_returns_error_envelope()
        {
            _contractorRepository.GetByEmployeeId("TST-000001").ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetByEmployeeId("TST-000001");

            Assert.False(result.Success);
            Assert.Equal("Error retrieving contractor", result.Message);
        }

        [Fact]
        public async Task GetAll_passes_repository_data_through()
        {
            var data = new[] { new ContractorBuilder().Build() };
            _contractorRepository.GetAll().Returns(data);

            var result = await CreateService().GetAll();

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        [Fact]
        public async Task GetAll_repository_failure_returns_error_envelope()
        {
            _contractorRepository.GetAll().ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetAll();

            Assert.False(result.Success);
            Assert.Equal("Error retrieving contractors", result.Message);
        }

        [Fact]
        public async Task GetQrCodes_passes_repository_data_through()
        {
            var data = new[] { new contractor_qr_code() };
            _contractorRepository.GetQrCodes().Returns(data);

            var result = await CreateService().GetQrCodes();

            Assert.True(result.Success);
            Assert.Same(data, result.Data);
        }

        [Fact]
        public async Task GetQrCodes_repository_failure_returns_error_envelope()
        {
            _contractorRepository.GetQrCodes().ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().GetQrCodes();

            Assert.False(result.Success);
            Assert.Equal("Error retrieving QR codes", result.Message);
        }

        #endregion

        #region Create (create-or-merge routing)

        [Fact]
        public async Task Create_invalid_submission_returns_validation_message()
        {
            var result = await CreateService().Create(null!, Admin);

            Assert.False(result.Success);
            Assert.Equal("Contractor data is required", result.Message);
            await _contractorRepository.DidNotReceive().Create(Arg.Any<contractor_employee>());
        }

        [Fact]
        public async Task Create_unknown_project_is_rejected()
        {
            var submitted = new ContractorBuilder().Build();
            _projectRepository.GetByProjectCode("TST-26-001").Returns((project?)null);

            var result = await CreateService().Create(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Project not found: TST-26-001", result.Message);
        }

        [Fact]
        public async Task Create_project_from_another_provider_is_rejected()
        {
            var submitted = new ContractorBuilder().Build();
            StubProject("TST-26-001", provider_code: "OTHER");

            var result = await CreateService().Create(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Project TST-26-001 does not belong to the selected provider.", result.Message);
        }

        [Fact]
        public async Task Create_unknown_provider_is_rejected()
        {
            var submitted = new ContractorBuilder().Build();
            StubProject("TST-26-001");
            _providerRepository.GetByProviderCode("TST").Returns((provider?)null);

            var result = await CreateService().Create(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Provider not found", result.Message);
        }

        [Fact]
        public async Task Create_happy_path_persists_qr_and_audits_create()
        {
            var submitted = new ContractorBuilder().Build();
            var captured = CaptureCreates();
            StubProject("TST-26-001");
            StubProvider();
            _contractorRepository.FindDuplicate(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
                .Returns((contractor_employee?)null);

            var result = await CreateService().Create(submitted, Admin);

            Assert.True(result.Success, result.Message);
            Assert.Equal("Contractor created successfully", result.Message);
            var created = captured.Single();
            Assert.Equal("TST-000001", created.employee_id);
            await _contractorRepository.Received(1).SetQrCode("TST-000001", Arg.Any<byte[]>());

            var audit = _auditCalls.Single();
            Assert.Equal("contractor", audit.Entity);
            Assert.Equal("create", audit.Action);
            Assert.Equal("TST-000001", audit.ReferenceId);
            Assert.Null(audit.DataFrom);
            Assert.Same(created, audit.DataTo);
            Assert.Equal(Admin, audit.UpdatedBy);
        }

        [Fact]
        public async Task Create_repository_null_returns_failure()
        {
            var submitted = new ContractorBuilder().Build();
            StubProject("TST-26-001");
            StubProvider();
            _contractorRepository.FindDuplicate(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
                .Returns((contractor_employee?)null);
            _contractorRepository.Create(Arg.Any<contractor_employee>()).Returns((contractor_employee)null!);

            var result = await CreateService().Create(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Error creating contractor", result.Message);
            Assert.Empty(_auditCalls);
        }

        [Fact]
        public async Task Create_qr_persistence_failure_does_not_fail_the_registration()
        {
            // Best-effort QR: a SetQrCode blow-up must not fail the enrollment.
            var submitted = new ContractorBuilder().Build();
            CaptureCreates();
            StubProject("TST-26-001");
            StubProvider();
            _contractorRepository.SetQrCode(Arg.Any<string>(), Arg.Any<byte[]>())
                .ThrowsAsync(new InvalidOperationException("blob too large"));

            var result = await CreateService().Create(submitted, Admin);

            Assert.True(result.Success, result.Message);
            Assert.Equal("Contractor created successfully", result.Message);
        }

        [Fact]
        public async Task Create_with_log_audit_false_skips_the_audit_entry()
        {
            var submitted = new ContractorBuilder().Build();
            CaptureCreates();
            StubProject("TST-26-001");
            StubProvider();

            var result = await CreateService().Create(submitted, Admin, log_audit: false);

            Assert.True(result.Success, result.Message);
            Assert.Empty(_auditCalls);
        }

        [Fact]
        public async Task Create_duplicate_routes_to_merge_instead_of_insert()
        {
            var submitted = new ContractorBuilder().Build();
            var existing = ExistingWithPositions(@"{""TST-26-001"":""Worker""}");
            var captured = CaptureUpdates();
            StubProject("TST-26-001");
            StubProvider();
            _contractorRepository.FindDuplicate("TST", "Juan Dela Cruz", Arg.Any<DateTime>())
                .Returns(new contractor_employee { employee_id = "TST-000009" });
            _contractorRepository.GetByEmployeeId("TST-000009").Returns(existing);

            var result = await CreateService().Create(submitted, Admin);

            Assert.True(result.Success, result.Message);
            Assert.Equal("Contractor already enrolled under this provider — existing account updated with the submitted details",
                result.Message);
            await _contractorRepository.DidNotReceive().Create(Arg.Any<contractor_employee>());
            Assert.Single(captured);
            // The merge is audited as a contractor update.
            Assert.Equal("update", _auditCalls.Single().Action);
        }

        [Fact]
        public async Task Create_duplicate_race_falls_through_to_create()
        {
            // Duplicate found, but the account vanished between the two reads.
            var submitted = new ContractorBuilder().Build();
            var captured = CaptureCreates();
            StubProject("TST-26-001");
            StubProvider();
            _contractorRepository.FindDuplicate("TST", "Juan Dela Cruz", Arg.Any<DateTime>())
                .Returns(new contractor_employee { employee_id = "TST-000009" });
            _contractorRepository.GetByEmployeeId("TST-000009").Returns((contractor_employee?)null);

            var result = await CreateService().Create(submitted, Admin);

            Assert.True(result.Success, result.Message);
            Assert.Equal("Contractor created successfully", result.Message);
            Assert.Single(captured);
        }

        #endregion

        #region Merge-on-duplicate field rules

        [Fact]
        public async Task Merge_overwrites_details_but_keeps_identity_fields()
        {
            var submitted = new ContractorBuilder()
                .WithEmployeeId("TST-777777")                     // ignored on merge
                .WithName("Juan D. Cruz")                          // registered casing kept
                .AsInactive()                                      // Active checkbox ignored
                .WithGender("Female")
                .WithBirthdate(DateTime.Today.AddYears(-25))
                .WithContactNumber("09180000000")
                .WithAddress("New Address")
                .Build();
            var existing = ExistingWithPositions(@"{""TST-26-001"":""Worker""}").Let(e =>
            {
                e.name = "Juan Dela Cruz";   // registered casing (kept)
                e.active = 0;                // kept even though submission is active
                return e;
            });
            var captured = CaptureUpdates();
            StubProject("TST-26-001");
            StubProvider();
            _contractorRepository.FindDuplicate("TST", "Juan D. Cruz", Arg.Any<DateTime>())
                .Returns(new contractor_employee { employee_id = "TST-000009" });
            _contractorRepository.GetByEmployeeId("TST-000009").Returns(existing);

            var result = await CreateService().Create(submitted, Admin);

            Assert.True(result.Success, result.Message);
            var target = captured.Single();
            Assert.Equal("TST-000009", target.employee_id);          // kept
            Assert.Equal("Juan Dela Cruz", target.name);             // kept
            Assert.Equal("TST", target.provider_code);               // kept
            Assert.Equal(0, target.active);                          // kept
            Assert.Equal("Female", target.gender);                   // overwritten
            Assert.Equal(DateTime.Today.AddYears(-25), target.birthdate);
            Assert.Equal("09180000000", target.contact_number);
            Assert.Equal("New Address", target.address);
        }

        [Fact]
        public async Task Merge_replaces_matching_position_and_appends_new_projects()
        {
            var submitted = new ContractorBuilder()
                .WithAssignments(
                    ("TST-26-001", "Supervisor"),   // replaces stored "Worker"
                    ("TST-26-003", "Welder"))       // appended
                .Build();
            var existing = ExistingWithPositions(@"{""TST-26-001"":""Worker"",""TST-26-002"":""Helper""}");
            var captured = CaptureUpdates();
            StubProject("TST-26-001");
            StubProject("TST-26-003");
            StubProvider();
            _contractorRepository.FindDuplicate("TST", "Juan Dela Cruz", Arg.Any<DateTime>())
                .Returns(new contractor_employee { employee_id = "TST-000009" });
            _contractorRepository.GetByEmployeeId("TST-000009").Returns(existing);

            var result = await CreateService().Create(submitted, Admin);

            Assert.True(result.Success, result.Message);
            var merged = captured.Single().assignments!;
            Assert.Equal(3, merged.Count);
            Assert.Equal(("TST-26-001", "Supervisor"), (merged[0].project_code, merged[0].position));
            Assert.Equal(("TST-26-002", "Helper"), (merged[1].project_code, merged[1].position));
            Assert.Equal(("TST-26-003", "Welder"), (merged[2].project_code, merged[2].position));
        }

        [Fact]
        public async Task Merge_with_unparseable_project_positions_aborts_without_saving()
        {
            // Update rewrites the junction delete-all + re-insert; an unparseable
            // snapshot must abort the merge, never wipe the mappings.
            var submitted = new ContractorBuilder().Build();
            var existing = ExistingWithPositions("not-json{");
            StubProject("TST-26-001");
            StubProvider();
            _contractorRepository.FindDuplicate("TST", "Juan Dela Cruz", Arg.Any<DateTime>())
                .Returns(new contractor_employee { employee_id = "TST-000009" });
            _contractorRepository.GetByEmployeeId("TST-000009").Returns(existing);

            var result = await CreateService().Create(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Error merging contractor", result.Message);
            await _contractorRepository.DidNotReceive().Update(Arg.Any<contractor_employee>());
        }

        [Fact]
        public async Task Merge_exceeding_fifty_total_projects_is_rejected()
        {
            var entries = string.Join(",", Enumerable.Range(1, 50).Select(i => $@"""TST-26-{i:D3}"":""Role {i}"""));
            var submitted = new ContractorBuilder()
                .WithAssignments(("TST-26-999", "One too many"))
                .Build();
            var existing = ExistingWithPositions("{" + entries + "}");
            StubProject("TST-26-999");
            StubProvider();
            _contractorRepository.FindDuplicate("TST", "Juan Dela Cruz", Arg.Any<DateTime>())
                .Returns(new contractor_employee { employee_id = "TST-000009" });
            _contractorRepository.GetByEmployeeId("TST-000009").Returns(existing);

            var result = await CreateService().Create(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("A contractor can be assigned to at most 50 projects", result.Message);
            await _contractorRepository.DidNotReceive().Update(Arg.Any<contractor_employee>());
        }

        [Fact]
        public async Task Merge_repository_null_returns_failure()
        {
            var submitted = new ContractorBuilder().Build();
            var existing = ExistingWithPositions(@"{""TST-26-001"":""Worker""}");
            StubProject("TST-26-001");
            StubProvider();
            _contractorRepository.FindDuplicate("TST", "Juan Dela Cruz", Arg.Any<DateTime>())
                .Returns(new contractor_employee { employee_id = "TST-000009" });
            _contractorRepository.GetByEmployeeId("TST-000009").Returns(existing);
            _contractorRepository.Update(Arg.Any<contractor_employee>()).Returns((contractor_employee)null!);

            var result = await CreateService().Create(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Error updating contractor", result.Message);
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_null_employee_is_rejected()
        {
            var result = await CreateService().Update(null!, Admin);

            Assert.False(result.Success);
            Assert.Equal("Contractor data is required", result.Message);
        }

        [Fact]
        public async Task Update_blank_employee_id_is_rejected()
        {
            var submitted = new ContractorBuilder().WithEmployeeId("  ").Build();

            var result = await CreateService().Update(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Employee ID is required", result.Message);
        }

        [Fact]
        public async Task Update_invalid_submission_returns_validation_message()
        {
            var submitted = new ContractorBuilder().WithoutAssignments().Build();

            var result = await CreateService().Update(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("At least one project must be assigned", result.Message);
        }

        [Fact]
        public async Task Update_unknown_contractor_is_rejected()
        {
            var submitted = new ContractorBuilder().Build();
            _contractorRepository.GetByEmployeeId("TST-000001").Returns((contractor_employee?)null);

            var result = await CreateService().Update(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Contractor not found", result.Message);
        }

        [Fact]
        public async Task Update_unknown_project_is_rejected()
        {
            var submitted = new ContractorBuilder().Build();
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(new ContractorBuilder().Build());
            _projectRepository.GetByProjectCode("TST-26-001").Returns((project?)null);

            var result = await CreateService().Update(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Project not found: TST-26-001", result.Message);
            await _contractorRepository.DidNotReceive().Update(Arg.Any<contractor_employee>());
        }

        [Fact]
        public async Task Update_unknown_provider_is_rejected()
        {
            var submitted = new ContractorBuilder().Build();
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(new ContractorBuilder().Build());
            StubProject("TST-26-001");
            _providerRepository.GetByProviderCode("TST").Returns((provider?)null);

            var result = await CreateService().Update(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Provider not found", result.Message);
        }

        [Fact]
        public async Task Update_happy_path_audits_before_and_after()
        {
            var submitted = new ContractorBuilder().Build();
            var existing = new ContractorBuilder().WithGender("Male").Build();
            var captured = CaptureUpdates();
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(existing);
            StubProject("TST-26-001");
            StubProvider();

            var result = await CreateService().Update(submitted, Admin);

            Assert.True(result.Success, result.Message);
            Assert.Equal("Contractor updated successfully", result.Message);
            var updated = captured.Single();
            var audit = _auditCalls.Single();
            Assert.Equal("contractor", audit.Entity);
            Assert.Equal("update", audit.Action);
            Assert.Equal("TST-000001", audit.ReferenceId);
            Assert.Same(existing, audit.DataFrom);   // previous state
            Assert.Same(updated, audit.DataTo);      // post-update state
        }

        // PIN: Update never checks the repository result — a failed SP update still
        // returns Success = true with Data = null (and still writes the audit entry).
        [Fact]
        public async Task Update_repository_null_reports_failure_without_audit()
        {
            var submitted = new ContractorBuilder().Build();
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(new ContractorBuilder().Build());
            StubProject("TST-26-001");
            StubProvider();
            _contractorRepository.Update(Arg.Any<contractor_employee>()).Returns((contractor_employee)null!);

            var result = await CreateService().Update(submitted, Admin);

            Assert.False(result.Success);             // the save did not happen
            Assert.Equal("Error updating contractor", result.Message);
            Assert.Null(result.Data);
            Assert.Empty(_auditCalls);                // no audit for a failed save
        }

        [Fact]
        public async Task Update_repository_failure_returns_error_envelope()
        {
            var submitted = new ContractorBuilder().Build();
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(new ContractorBuilder().Build());
            StubProject("TST-26-001");
            StubProvider();
            _contractorRepository.Update(Arg.Any<contractor_employee>())
                .ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().Update(submitted, Admin);

            Assert.False(result.Success);
            Assert.Equal("Error updating contractor", result.Message);
        }

        #endregion

        #region SetInactive

        [Fact]
        public async Task SetInactive_passes_repository_result_through()
        {
            _contractorRepository.SetInactive("TST-000001").Returns(false);

            var result = await CreateService().SetInactive("TST-000001");

            // PIN: Success is true even when the repository reports false.
            Assert.True(result.Success);
            Assert.False(result.Data);
        }

        [Fact]
        public async Task SetInactive_repository_failure_returns_error_envelope()
        {
            _contractorRepository.SetInactive("TST-000001").ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().SetInactive("TST-000001");

            Assert.False(result.Success);
            Assert.Equal("Error setting contractor inactive", result.Message);
            Assert.False(result.Data);
        }

        #endregion

        #region Delete (bulk soft delete)

        [Fact]
        public async Task Delete_with_no_ids_is_rejected()
        {
            var result = await CreateService().Delete(new List<string>(), Admin);

            Assert.False(result.Success);
            Assert.Equal("No contractor IDs were provided.", result.Message);
            await _contractorRepository.DidNotReceive().Delete(Arg.Any<IEnumerable<string>>());
        }

        [Fact]
        public async Task Delete_with_only_blank_ids_is_rejected()
        {
            var result = await CreateService().Delete(new List<string> { "  ", "" }, Admin);

            Assert.False(result.Success);
            Assert.Equal("No valid contractor IDs were provided.", result.Message);
        }

        [Fact]
        public async Task Delete_drops_comma_bearing_ids()
        {
            // The SP receives ids as a CSV matched with FIND_IN_SET — a comma-bearing
            // id would corrupt the list, so it is dropped by the sanitizer.
            var result = await CreateService().Delete(new List<string> { "TST,001" }, Admin);

            Assert.False(result.Success);
            Assert.Equal("No valid contractor IDs were provided.", result.Message);
        }

        [Fact]
        public async Task Delete_with_no_matching_contractors_is_rejected()
        {
            _contractorRepository.GetByEmployeeId("TST-000001").Returns((contractor_employee?)null);

            var result = await CreateService().Delete(new List<string> { "TST-000001" }, Admin);

            Assert.False(result.Success);
            Assert.Equal("No contractors were found to delete (they may have already been deleted).", result.Message);
            await _contractorRepository.DidNotReceive().Delete(Arg.Any<IEnumerable<string>>());
        }

        [Fact]
        public async Task Delete_single_contractor_audits_with_the_employee_id()
        {
            var contractor = new ContractorBuilder().Build();
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(contractor);
            _contractorRepository.Delete(Arg.Any<IEnumerable<string>>())
                .Returns(new contractor_delete_result { success = true, deleted_count = 1 });

            var result = await CreateService().Delete(new List<string> { "TST-000001" }, Admin);

            Assert.True(result.Success);
            Assert.Equal("Deleted 1 contractor(s) successfully", result.Message);
            Assert.Equal(1, result.Data);
            await _contractorRepository.Received(1).Delete(
                Arg.Is<IEnumerable<string>>(ids => ids.Single() == "TST-000001"));
            var audit = _auditCalls.Single();
            Assert.Equal("delete", audit.Action);
            Assert.Equal("TST-000001", audit.ReferenceId);
            // data_from is the pre-delete snapshot list (one entry for a single delete).
            var snapshot = Assert.IsType<List<contractor_employee>>(audit.DataFrom);
            Assert.Same(contractor, snapshot.Single());
            Assert.Null(audit.DataTo);   // soft delete: no new state
        }

        [Fact]
        public async Task Delete_multiple_contractors_uses_a_count_summary_reference()
        {
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(new ContractorBuilder().WithEmployeeId("TST-000001").Build());
            _contractorRepository.GetByEmployeeId("TST-000002").Returns(new ContractorBuilder().WithEmployeeId("TST-000002").Build());
            _contractorRepository.Delete(Arg.Any<IEnumerable<string>>())
                .Returns(new contractor_delete_result { success = true, deleted_count = 2 });

            var result = await CreateService().Delete(new List<string> { "TST-000001", "TST-000002" }, Admin);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data);
            var audit = _auditCalls.Single();
            // audit_log.reference_id is VARCHAR(100) - bulk deletes summarize.
            Assert.Equal("2 contractors", audit.ReferenceId);
        }

        [Fact]
        public async Task Delete_deduplicates_and_reports_skipped_ids()
        {
            // Duplicate id resolved once; unknown id counted as skipped.
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(new ContractorBuilder().Build());
            _contractorRepository.GetByEmployeeId("TST-000009").Returns((contractor_employee?)null);
            _contractorRepository.Delete(Arg.Any<IEnumerable<string>>())
                .Returns(new contractor_delete_result { success = true, deleted_count = 1 });

            var result = await CreateService().Delete(new List<string> { "TST-000001", "TST-000001", "TST-000009" }, Admin);

            Assert.True(result.Success);
            Assert.Contains("1 skipped - not found or already deleted", result.Message);
            await _contractorRepository.Received(1).GetByEmployeeId("TST-000001");   // deduped
            await _contractorRepository.Received(1).Delete(
                Arg.Is<IEnumerable<string>>(ids => ids.Single() == "TST-000001"));
        }

        // A zero-count delete result is a failure with honest wording, and no audit
        // entry is written for the no-op.
        [Fact]
        public async Task Delete_zero_deleted_reports_failure_without_audit()
        {
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(new ContractorBuilder().Build());
            _contractorRepository.Delete(Arg.Any<IEnumerable<string>>())
                .Returns(new contractor_delete_result { success = false, deleted_count = 0 });

            var result = await CreateService().Delete(new List<string> { "TST-000001" }, Admin);

            Assert.False(result.Success);
            Assert.Equal("No contractors were deleted (they may have already been deleted)", result.Message);
            Assert.Empty(_auditCalls);
        }

        [Fact]
        public async Task Delete_repository_failure_returns_error_envelope()
        {
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(new ContractorBuilder().Build());
            _contractorRepository.Delete(Arg.Any<IEnumerable<string>>())
                .ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().Delete(new List<string> { "TST-000001" }, Admin);

            Assert.False(result.Success);
            Assert.Equal("Error deleting contractors", result.Message);
            Assert.Equal(0, result.Data);
        }

        #endregion

        #region CascadeDeactivateByProject

        [Fact]
        public async Task CascadeDeactivate_skips_contractors_with_another_active_project()
        {
            _contractorRepository.GetByProjectCode("TST-26-001")
                .Returns(new[] { new ContractorBuilder().WithOtherActiveProjectCount(1).Build() });

            var result = await CreateService().CascadeDeactivateByProject("TST-26-001", Admin);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data);
            await _contractorRepository.DidNotReceive().GetByEmployeeId(Arg.Any<string>());
            await _contractorRepository.DidNotReceive().Update(Arg.Any<contractor_employee>());
        }

        [Fact]
        public async Task CascadeDeactivate_sets_last_active_contractor_inactive_and_audits()
        {
            var full = new ContractorBuilder().Build();   // active = 1
            _contractorRepository.GetByProjectCode("TST-26-001")
                .Returns(new[] { new ContractorBuilder().WithOtherActiveProjectCount(0).Build() });
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(full);
            var captured = CaptureUpdates();

            var result = await CreateService().CascadeDeactivateByProject("TST-26-001", Admin);

            Assert.True(result.Success);
            Assert.Equal("1 contractor(s) deactivated", result.Message);
            Assert.Equal(1, result.Data);
            Assert.Equal(0, captured.Single().active);
            var audit = _auditCalls.Single();
            Assert.Equal("deactivate_by_project", audit.Action);
            Assert.Equal("TST-000001", audit.ReferenceId);
            var previous = Assert.IsType<contractor_employee>(audit.DataFrom);
            Assert.Equal(1, previous.active);   // data_from snapshot taken before the flip
        }

        [Fact]
        public async Task CascadeDeactivate_skips_vanished_rows()
        {
            _contractorRepository.GetByProjectCode("TST-26-001")
                .Returns(new[] { new ContractorBuilder().WithOtherActiveProjectCount(0).Build() });
            _contractorRepository.GetByEmployeeId("TST-000001").Returns((contractor_employee?)null);

            var result = await CreateService().CascadeDeactivateByProject("TST-26-001", Admin);

            Assert.Equal(0, result.Data);
            await _contractorRepository.DidNotReceive().Update(Arg.Any<contractor_employee>());
        }

        [Fact]
        public async Task CascadeDeactivate_skips_already_inactive_rows()
        {
            _contractorRepository.GetByProjectCode("TST-26-001")
                .Returns(new[] { new ContractorBuilder().WithOtherActiveProjectCount(0).Build() });
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(new ContractorBuilder().AsInactive().Build());

            var result = await CreateService().CascadeDeactivateByProject("TST-26-001", Admin);

            Assert.Equal(0, result.Data);
            await _contractorRepository.DidNotReceive().Update(Arg.Any<contractor_employee>());
        }

        [Fact]
        public async Task CascadeDeactivate_uncounted_when_update_fails()
        {
            _contractorRepository.GetByProjectCode("TST-26-001")
                .Returns(new[] { new ContractorBuilder().WithOtherActiveProjectCount(0).Build() });
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(new ContractorBuilder().Build());
            _contractorRepository.Update(Arg.Any<contractor_employee>()).Returns((contractor_employee)null!);

            var result = await CreateService().CascadeDeactivateByProject("TST-26-001", Admin);

            Assert.Equal(0, result.Data);
            Assert.Empty(_auditCalls);
        }

        [Fact]
        public async Task CascadeDeactivate_repository_failure_returns_error_envelope()
        {
            _contractorRepository.GetByProjectCode("TST-26-001")
                .ThrowsAsync(new InvalidOperationException("db down"));

            var result = await CreateService().CascadeDeactivateByProject("TST-26-001", Admin);

            Assert.False(result.Success);
            Assert.Equal("Error deactivating contractors", result.Message);
            Assert.Equal(0, result.Data);
        }

        #endregion

        #region CascadeReactivateByProject

        private static audit_log CascadeEntry(int log_id, string employee_id, string data_from, string data_to) =>
            new()
            {
                log_id = log_id,
                entity_type = "contractor",
                action = "deactivate_by_project",
                reference_id = employee_id,
                data_from = data_from,
                data_to = data_to,
                updated_by = Admin
            };

        [Fact]
        public async Task CascadeReactivate_skips_contractors_deactivated_manually()
        {
            // Latest 1->0 transition is a manual update (higher log_id) - stays inactive.
            var cascade = CascadeEntry(5, "TST-000001",
                data_from: @"{""active"":1}",
                data_to: @"{""active"":0,""project_code"":""TST-26-001""}");
            var manual = new audit_log
            {
                log_id = 10,
                entity_type = "contractor",
                action = "update",
                reference_id = "TST-000001",
                data_from = @"{""active"":1}",
                data_to = @"{""active"":0}"
            };
            _auditLogService.GetByEntity("contractor", "deactivate_by_project")
                .Returns(new Response<IEnumerable<audit_log>> { Success = true, Data = new[] { cascade } });
            _auditLogService.GetByEntity("contractor", "update")
                .Returns(new Response<IEnumerable<audit_log>> { Success = true, Data = new[] { manual } });
            _contractorRepository.GetByEmployeeId("TST-000001")
                .Returns(new ContractorBuilder().AsInactive().WithProjectCodesCsv("TST-26-001").Build());

            var result = await CreateService().CascadeReactivateByProject("TST-26-001", Admin);

            Assert.True(result.Success);
            Assert.Equal("0 contractor(s) re-activated", result.Message);
            await _contractorRepository.DidNotReceive().Update(Arg.Any<contractor_employee>());
        }

        [Fact]
        public async Task CascadeReactivate_skips_entries_for_a_different_project()
        {
            var cascade = CascadeEntry(5, "TST-000001",
                data_from: @"{""active"":1}",
                data_to: @"{""active"":0,""project_code"":""OTHER-26-001""}");
            _auditLogService.GetByEntity("contractor", "deactivate_by_project")
                .Returns(new Response<IEnumerable<audit_log>> { Success = true, Data = new[] { cascade } });
            _contractorRepository.GetByEmployeeId("TST-000001")
                .Returns(new ContractorBuilder().AsInactive().WithProjectCodesCsv("TST-26-001").Build());

            var result = await CreateService().CascadeReactivateByProject("TST-26-001", Admin);

            Assert.Equal(0, result.Data);
            await _contractorRepository.DidNotReceive().Update(Arg.Any<contractor_employee>());
        }

        [Fact]
        public async Task CascadeReactivate_skips_already_active_and_unassigned_contractors()
        {
            var cascade = CascadeEntry(5, "TST-000001",
                data_from: @"{""active"":1}",
                data_to: @"{""active"":0,""project_code"":""TST-26-001""}");
            _auditLogService.GetByEntity("contractor", "deactivate_by_project")
                .Returns(new Response<IEnumerable<audit_log>> { Success = true, Data = new[] { cascade } });

            // Already active.
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(new ContractorBuilder().Build());
            var first = await CreateService().CascadeReactivateByProject("TST-26-001", Admin);
            Assert.Equal(0, first.Data);

            // Inactive but no longer assigned to this project.
            _contractorRepository.GetByEmployeeId("TST-000001")
                .Returns(new ContractorBuilder().AsInactive().WithProjectCodesCsv("TST-26-002").Build());
            var second = await CreateService().CascadeReactivateByProject("TST-26-001", Admin);
            Assert.Equal(0, second.Data);

            await _contractorRepository.DidNotReceive().Update(Arg.Any<contractor_employee>());
        }

        [Fact]
        public async Task CascadeReactivate_restores_matching_contractor_from_compact_audit_json()
        {
            // MySQL JSON_OBJECT (compact) payload from the SQL expiry sweep.
            var cascade = CascadeEntry(5, "TST-000001",
                data_from: @"{""employee_id"":""TST-000001"",""active"":1}",
                data_to: @"{""active"":0,""project_code"":""TST-26-001""}");
            _auditLogService.GetByEntity("contractor", "deactivate_by_project")
                .Returns(new Response<IEnumerable<audit_log>> { Success = true, Data = new[] { cascade } });
            var contractor = new ContractorBuilder().AsInactive().WithProjectCodesCsv("TST-26-001,TST-26-002").Build();
            _contractorRepository.GetByEmployeeId("TST-000001").Returns(contractor);
            var captured = CaptureUpdates();

            var result = await CreateService().CascadeReactivateByProject("TST-26-001", Admin);

            Assert.True(result.Success);
            Assert.Equal("1 contractor(s) re-activated", result.Message);
            Assert.Equal(1, captured.Single().active);
            var audit = _auditCalls.Single();
            Assert.Equal("reactivate_by_project", audit.Action);
            // data_from is the pre-flip clone; data_to is the mutated entity.
            var previous = Assert.IsType<contractor_employee>(audit.DataFrom);
            Assert.Equal(0, previous.active);
            Assert.Same(contractor, audit.DataTo);
        }

        [Fact]
        public async Task CascadeReactivate_restores_matching_contractor_from_indented_audit_json()
        {
            // C#-serialized (indented) payload written by CascadeDeactivateByProject.
            var dataFrom = "{\n  \"employee_id\": \"TST-000001\",\n  \"active\": 1\n}";
            var dataTo = "{\n  \"project_codes\": \"TST-26-001,TST-26-002\",\n  \"active\": 0\n}";
            var cascade = CascadeEntry(5, "TST-000001", dataFrom, dataTo);
            _auditLogService.GetByEntity("contractor", "deactivate_by_project")
                .Returns(new Response<IEnumerable<audit_log>> { Success = true, Data = new[] { cascade } });
            _contractorRepository.GetByEmployeeId("TST-000001")
                .Returns(new ContractorBuilder().AsInactive().WithProjectCodesCsv("TST-26-001").Build());
            CaptureUpdates();

            var result = await CreateService().CascadeReactivateByProject("TST-26-001", Admin);

            Assert.Equal(1, result.Data);
        }

        [Fact]
        public async Task CascadeReactivate_with_unreadable_audit_log_restores_nothing()
        {
            _auditLogService.GetByEntity(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new Response<IEnumerable<audit_log>> { Success = false });

            var result = await CreateService().CascadeReactivateByProject("TST-26-001", Admin);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data);
            await _contractorRepository.DidNotReceive().Update(Arg.Any<contractor_employee>());
        }

        #endregion

        #region BulkCreate

        private static bulk_enrollment_row Row(int row_number, string name, string gender = "Male", DateTime? birthdate = null, string position = "Worker") =>
            new()
            {
                row_number = row_number,
                name = name,
                gender = gender,
                birthdate = birthdate ?? DateTime.Today.AddYears(-30),
                position = position
            };

        private bulk_enrollment_request BulkRequest(params bulk_enrollment_row[] rows) => new()
        {
            provider_code = "TST",
            project_code = "TST-26-001",
            file_name = "roster.csv",
            contractors = rows.ToList()
        };

        [Fact]
        public async Task BulkCreate_null_request_is_rejected()
        {
            var result = await CreateService().BulkCreate(null!, Admin);

            Assert.False(result.Success);
            Assert.Equal("No contractor rows were provided for import.", result.Message);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task BulkCreate_empty_rows_are_rejected()
        {
            var result = await CreateService().BulkCreate(BulkRequest(), Admin);

            Assert.False(result.Success);
            Assert.Equal("No contractor rows were provided for import.", result.Message);
        }

        [Fact]
        public async Task BulkCreate_requires_provider_and_project()
        {
            var request = BulkRequest(Row(1, "Juan Dela Cruz"));
            request.provider_code = "";

            var result = await CreateService().BulkCreate(request, Admin);

            Assert.False(result.Success);
            Assert.Equal("Provider and Project are required.", result.Message);
        }

        [Fact]
        public async Task BulkCreate_unknown_project_is_rejected()
        {
            _projectRepository.GetByProjectCode("TST-26-001").Returns((project?)null);

            var result = await CreateService().BulkCreate(BulkRequest(Row(1, "Juan Dela Cruz")), Admin);

            Assert.False(result.Success);
            Assert.Equal("Selected project was not found.", result.Message);
        }

        [Fact]
        public async Task BulkCreate_rejects_project_from_another_provider()
        {
            StubProject("TST-26-001", provider_code: "OTHER");

            var result = await CreateService().BulkCreate(BulkRequest(Row(1, "Juan Dela Cruz")), Admin);

            Assert.False(result.Success);
            Assert.Equal("The selected project does not belong to the selected provider.", result.Message);
        }

        [Fact]
        public async Task BulkCreate_unknown_provider_is_rejected()
        {
            StubProject("TST-26-001");
            _providerRepository.GetByProviderCode("TST").Returns((provider?)null);

            var result = await CreateService().BulkCreate(BulkRequest(Row(1, "Juan Dela Cruz")), Admin);

            Assert.False(result.Success);
            Assert.Equal("Selected provider was not found.", result.Message);
        }

        [Fact]
        public async Task BulkCreate_invalid_gender_is_a_row_error()
        {
            StubProject("TST-26-001");
            StubProvider();

            var result = await CreateService().BulkCreate(BulkRequest(Row(1, "Juan Dela Cruz", gender: "M")), Admin);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.success_count);
            Assert.Equal(1, result.Data.error_count);
            Assert.Equal("Gender must be Male or Female.", result.Data.errors.Single().message);
            Assert.Equal(1, result.Data.errors.Single().row);
        }

        // Gender is accepted case-insensitively and normalized to canonical casing
        // so the column stays consistent ("MALE" is stored as "Male").
        [Fact]
        public async Task BulkCreate_gender_casing_is_normalized()
        {
            StubProject("TST-26-001");
            StubProvider();
            var captured = CaptureCreates();

            var result = await CreateService().BulkCreate(BulkRequest(Row(1, "Juan Dela Cruz", gender: "MALE")), Admin);

            Assert.True(result.Success, result.Message);
            Assert.Equal(1, result.Data!.success_count);
            Assert.Equal("Male", captured.Single().gender);   // normalized, not "MALE"
        }

        [Fact]
        public async Task BulkCreate_reports_enrolled_and_merged_rows_and_writes_one_audit()
        {
            StubProject("TST-26-001");
            StubProvider();
            // The SP assigns the employee_id, so the Create stub returns an entity
            // carrying the generated id (a passthrough would return null here).
            var created = new List<contractor_employee>();
            _contractorRepository.Create(Arg.Do<contractor_employee>(e => created.Add(e)))
                .Returns(new contractor_employee { employee_id = "TST-000001", name = "Juan Dela Cruz" });
            var existing = ExistingWithPositions(@"{""TST-26-001"":""Helper""}", employee_id: "TST-000009")
                .Let(e => { e.name = "Maria Santos"; return e; });
            _contractorRepository.FindDuplicate("TST", "Maria Santos", Arg.Any<DateTime>())
                .Returns(new contractor_employee { employee_id = "TST-000009" });
            _contractorRepository.GetByEmployeeId("TST-000009").Returns(existing);
            var capturedUpdates = CaptureUpdates();

            var result = await CreateService().BulkCreate(
                BulkRequest(
                    Row(1, "Juan Dela Cruz"),
                    Row(2, "Maria Santos")),
                Admin);

            Assert.True(result.Success, result.Message);
            Assert.Equal("Enrolled 1 of 2 contractor(s) and updated 1 existing; 0 failed.", result.Message);
            Assert.Equal(2, result.Data!.success_count);
            Assert.Equal(1, result.Data.merged_count);
            Assert.Equal(new[] { "TST-000001" }, result.Data.enrolled_employee_ids);
            Assert.Equal(new[] { "TST-000009" }, result.Data.merged_employee_ids);
            Assert.Single(created);
            Assert.Single(capturedUpdates);

            // Exactly one batch audit entry (per-row audits suppressed).
            var audit = _auditCalls.Single();
            Assert.Equal("bulk_enrollment", audit.Entity);
            Assert.Equal("bulk_create", audit.Action);
            Assert.Equal("TST-26-001", audit.ReferenceId);
            Assert.Null(audit.DataFrom);
            Assert.Equal(42, result.Data.log_id);
        }

        [Fact]
        public async Task BulkCreate_underage_row_is_reported_with_the_row_number()
        {
            StubProject("TST-26-001");
            StubProvider();

            var result = await CreateService().BulkCreate(
                BulkRequest(Row(3, "Young Worker", birthdate: DateTime.Today.AddYears(-15))), Admin);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data!.error_count);
            var error = result.Data.errors.Single();
            Assert.Equal(3, error.row);
            Assert.Contains("under 18", error.message);
        }

        [Fact]
        public async Task BulkCreate_audit_failure_does_not_fail_the_batch()
        {
            StubProject("TST-26-001");
            StubProvider();
            CaptureCreates();
            _auditLogService.Log(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<string>())
                .Returns(new Response<audit_log> { Success = false });

            var result = await CreateService().BulkCreate(BulkRequest(Row(1, "Juan Dela Cruz")), Admin);

            Assert.True(result.Success, result.Message);
            Assert.Equal(1, result.Data!.success_count);
            Assert.Equal(0, result.Data.log_id);   // never set when logging failed
        }

        #endregion
    }

    /// <summary>Small helper so test lambdas can mutate a builder-built entity inline.</summary>
    internal static class ContractorTestExtensions
    {
        public static T Let<T>(this T value, Func<T, T> mutate)
        {
            mutate(value);
            return value;
        }
    }
}
