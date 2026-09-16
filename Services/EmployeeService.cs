using EmployeeAttendanceWithHealthDeclaration.Helpers;
using EmployeeAttendanceWithHealthDeclaration.Models;
using EmployeeAttendanceWithHealthDeclaration.Models.Domain;
using EmployeeAttendanceWithHealthDeclaration.Repositories;
using System.Text.Json;

namespace EmployeeAttendanceWithHealthDeclaration.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IProviderRepository _providerRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            IEmployeeRepository employeeRepository,
            IProjectRepository projectRepository,
            IProviderRepository providerRepository,
            IAuditLogService auditLogService,
            ILogger<EmployeeService> logger)
        {
            _employeeRepository = employeeRepository;
            _projectRepository = projectRepository;
            _providerRepository = providerRepository;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<Response<employee>> GetByEmployeeId(string employee_id)
        {
            try
            {
                var employee = await _employeeRepository.GetByEmployeeId(employee_id);
                if (employee == null)
                {
                    return new Response<employee>
                    {
                        Success = false,
                        Message = "Employee not found",
                        Data = null
                    };
                }

                return new Response<employee>
                {
                    Success = true,
                    Message = "Employee retrieved successfully",
                    Data = employee
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee: {EmployeeId}", employee_id);
                return new Response<employee>
                {
                    Success = false,
                    Message = "Error retrieving employee",
                    Data = null
                };
            }
        }

        public async Task<Response<IEnumerable<employee>>> GetAll()
        {
            try
            {
                var employees = await _employeeRepository.GetAll();
                return new Response<IEnumerable<employee>>
                {
                    Success = true,
                    Message = "Employees retrieved successfully",
                    Data = employees
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all employees");
                return new Response<IEnumerable<employee>>
                {
                    Success = false,
                    Message = "Error retrieving employees",
                    Data = null
                };
            }
        }

        // Export-only read (GET /Admin/GetEmployeeQrCodes): hands the stored QR blobs
        // to the client at export time. Kept off the domain model so audit snapshots
        // and the grid payload never carry base64.
        public async Task<Response<IEnumerable<employee_qr_code>>> GetQrCodes()
        {
            try
            {
                var qr_codes = await _employeeRepository.GetQrCodes();
                return new Response<IEnumerable<employee_qr_code>>
                {
                    Success = true,
                    Message = "QR codes retrieved successfully",
                    Data = qr_codes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee QR codes");
                return new Response<IEnumerable<employee_qr_code>>
                {
                    Success = false,
                    Message = "Error retrieving QR codes",
                    Data = null
                };
            }
        }

        public async Task<Response<employee>> Create(employee employee, string admin_employee_id, bool log_audit = true)
        {
            var (response, _) = await CreateOrMerge(employee, admin_employee_id, log_audit);
            return response;
        }

        // Create, or merge into the existing account when the same person (name +
        // birthdate, case-insensitive) is already enrolled under the provider.
        // BulkCreate calls this directly so it can report merged rows separately.
        private async Task<(Response<employee> Response, bool Merged)> CreateOrMerge(
            employee employee, string admin_employee_id, bool log_audit)
        {
            try
            {
                // Validate required fields + birthdate (DOLE 18+). employee_id is NOT required
                // on create - it is auto-generated by the stored procedure as {provider_code}-NNNNNN.
                var validationError = ValidationHelper.ValidateEmployee(employee);
                if (validationError != null)
                {
                    return (new Response<employee>
                    {
                        Success = false,
                        Message = validationError,
                        Data = null
                    }, false);
                }

                // Validate every per-project assignment: each project must exist and
                // belong to the employee's provider.
                foreach (var assignment in employee.assignments!)
                {
                    var code = assignment.project_code.Trim();
                    var project = await _projectRepository.GetByProjectCode(code);
                    if (project == null)
                    {
                        return (new Response<employee>
                        {
                            Success = false,
                            Message = $"Project not found: {code}",
                            Data = null
                        }, false);
                    }

                    if (!string.Equals(project.provider_code, employee.provider_code, StringComparison.OrdinalIgnoreCase))
                    {
                        return (new Response<employee>
                        {
                            Success = false,
                            Message = $"Project {code} does not belong to the selected provider.",
                            Data = null
                        }, false);
                    }
                }

                // Validate provider exists
                var provider = await _providerRepository.GetByProviderCode(employee.provider_code);
                if (provider == null)
                {
                    return (new Response<employee>
                    {
                        Success = false,
                        Message = "Provider not found",
                        Data = null
                    }, false);
                }

                // Merge-on-duplicate: a employee with the same name (case-insensitive)
                // and birthdate already enrolled under this provider (active or inactive,
                // any project) is NOT rejected - the submission is merged into that
                // account (details overwritten, assignments replace-or-append; name,
                // employee_id, provider and active status kept). Direct API payloads
                // containing intra-batch duplicate rows therefore also succeed as
                // create-then-merge; the bulk UI blocks those client-side.
                if (employee.birthdate.HasValue)
                {
                    var duplicate = await _employeeRepository.FindDuplicate(
                        employee.provider_code, employee.name, employee.birthdate.Value);
                    if (duplicate != null && !string.IsNullOrWhiteSpace(duplicate.employee_id))
                    {
                        var existing = await _employeeRepository.GetByEmployeeId(duplicate.employee_id);
                        if (existing != null)
                        {
                            return await MergeIntoExisting(employee, existing, admin_employee_id, log_audit);
                        }
                        // Race: the duplicate vanished between the two reads (concurrently
                        // deleted) - fall through and create a fresh account.
                    }
                }

                // Normalize once so the p_projects JSON is clean (the SP TRIMs again defensively)
                foreach (var assignment in employee.assignments)
                {
                    assignment.project_code = assignment.project_code.Trim();
                    assignment.position = assignment.position.Trim();
                }

                // employee_id is auto-generated by the stored procedure ({provider_code}-NNNNNN)
                var result = await _employeeRepository.Create(employee);
                if (result == null)
                {
                    return (new Response<employee>
                    {
                        Success = false,
                        Message = "Error creating employee",
                        Data = null
                    }, false);
                }

                _logger.LogInformation("Employee created: {EmployeeId}", result.employee_id);

                // Persist the QR badge image at registration time (content = employee_id)
                // so the Excel export only ever embeds stored blobs. Best-effort — see EnsureQrCode.
                await EnsureQrCode(result.employee_id);

                if (log_audit)
                {
                    await _auditLogService.Log("employee", "create", result.employee_id, null, result, admin_employee_id);
                }

                return (new Response<employee>
                {
                    Success = true,
                    Message = "Employee created successfully",
                    Data = result
                }, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee: {EmployeeId}", employee.employee_id);
                return (new Response<employee>
                {
                    Success = false,
                    Message = "Error creating employee",
                    Data = null
                }, false);
            }
        }

        // QR persistence for both enrollment paths (create + merge-on-duplicate).
        // Best-effort by design: the employee row is valid without the QR — a
        // failure is logged and leaves qr_code_image NULL (blank cell in the Excel
        // export) until the employee is re-enrolled, which regenerates it.
        private async Task EnsureQrCode(string employee_id)
        {
            try
            {
                await _employeeRepository.SetQrCode(employee_id, QrCodeHelper.GeneratePng(employee_id));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist QR code for {EmployeeId}", employee_id);
            }
        }

        // Merge-on-duplicate path: the submitted employee already has an account under
        // the same provider (matched by name + birthdate). Update that account instead of
        // creating a new one - gender/birthdate/contact_number/address overwritten from
        // the submission; assignments merged (a submitted position REPLACES the stored one
        // for an already-assigned project, new projects APPEND). employee_id, name
        // (registered casing), provider_code and active are kept from the existing
        // account; the Add form's Active checkbox is intentionally ignored on merge.
        private async Task<(Response<employee> Response, bool Merged)> MergeIntoExisting(
            employee submitted, employee existing, string admin_employee_id, bool log_audit)
        {
            try
            {
                // Materialize the existing account's assignments from project_positions
                // (read entities carry assignments = null). Null = unparseable snapshot -
                // abort rather than save (Update's delete-all + re-insert of the mappings
                // would otherwise wipe them).
                var existingAssignments = ParseProjectPositions(existing.project_positions);
                if (existingAssignments == null)
                {
                    return (new Response<employee>
                    {
                        Success = false,
                        Message = "Error merging employee",
                        Data = null
                    }, false);
                }

                // Normalize once so replace-matching is clean (the SP TRIMs again defensively)
                foreach (var assignment in submitted.assignments!)
                {
                    assignment.project_code = assignment.project_code.Trim();
                    assignment.position = assignment.position.Trim();
                }

                // Merge: existing order preserved, submitted positions replace by project
                // code, new projects appended. Legacy blank positions on un-resubmitted
                // projects survive as-is (the read SPs already render them as skipped).
                var merged = new List<employee_project_assignment>(existingAssignments);
                foreach (var assignment in submitted.assignments)
                {
                    var match = merged.FirstOrDefault(a =>
                        string.Equals(a.project_code?.Trim(), assignment.project_code, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        match.position = assignment.position;
                    }
                    else
                    {
                        merged.Add(new employee_project_assignment
                        {
                            project_code = assignment.project_code,
                            position = assignment.position
                        });
                    }
                }

                // Only the 50-row cap needs re-checking on the merged set (same message as
                // ValidationHelper): blank legacy positions are legitimate, and duplicate
                // or blank project codes cannot occur by construction.
                if (merged.Count > 50)
                {
                    return (new Response<employee>
                    {
                        Success = false,
                        Message = "An employee can be assigned to at most 50 projects",
                        Data = null
                    }, false);
                }

                // Overwrite the mutable details; keep employee_id/name/provider_code/active.
                var target = CloneEmployee(existing);
                target.gender = submitted.gender;
                target.birthdate = submitted.birthdate;
                target.contact_number = submitted.contact_number;
                target.address = submitted.address;
                target.assignments = merged;

                // Update rewrites employee_project from the merged p_projects JSON
                // (delete-all + re-insert) - exactly the merge result.
                var result = await _employeeRepository.Update(target);
                if (result == null)
                {
                    return (new Response<employee>
                    {
                        Success = false,
                        Message = "Error updating employee",
                        Data = null
                    }, false);
                }

                _logger.LogInformation("Employee merged into existing account: {EmployeeId}", existing.employee_id);

                // Same QR persistence as the create path: the content is the immutable
                // employee_id, so this is byte-identical for rows that already have a QR
                // and doubles as the backfill for rows enrolled before the feature.
                await EnsureQrCode(existing.employee_id);

                if (log_audit)
                {
                    await _auditLogService.Log("employee", "update", existing.employee_id, existing, result, admin_employee_id);
                }

                return (new Response<employee>
                {
                    Success = true,
                    Message = "Employee already enrolled under this provider — existing account updated with the submitted details",
                    Data = result
                }, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging employee into existing account: {EmployeeId}", existing.employee_id);
                return (new Response<employee>
                {
                    Success = false,
                    Message = "Error merging employee",
                    Data = null
                }, false);
            }
        }

        public async Task<Response<employee>> Update(employee employee, string admin_employee_id)
        {
            try
            {
                // Validate required fields. On update the employee_id is also required (it is the key).
                if (employee == null)
                {
                    return new Response<employee>
                    {
                        Success = false,
                        Message = "Employee data is required",
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(employee.employee_id))
                {
                    return new Response<employee>
                    {
                        Success = false,
                        Message = "Employee ID is required",
                        Data = null
                    };
                }

                var validationError = ValidationHelper.ValidateEmployee(employee);
                if (validationError != null)
                {
                    return new Response<employee>
                    {
                        Success = false,
                        Message = validationError,
                        Data = null
                    };
                }

                // Validate employee exists (include inactive so they can be reactivated)
                var existing = await _employeeRepository.GetByEmployeeId(employee.employee_id);
                if (existing == null)
                {
                    return new Response<employee>
                    {
                        Success = false,
                        Message = "Employee not found",
                        Data = null
                    };
                }

                // Validate every per-project assignment: each project must exist and
                // belong to the employee's provider.
                foreach (var assignment in employee.assignments!)
                {
                    var code = assignment.project_code.Trim();
                    var project = await _projectRepository.GetByProjectCode(code);
                    if (project == null)
                    {
                        return new Response<employee>
                        {
                            Success = false,
                            Message = $"Project not found: {code}",
                            Data = null
                        };
                    }

                    if (!string.Equals(project.provider_code, employee.provider_code, StringComparison.OrdinalIgnoreCase))
                    {
                        return new Response<employee>
                        {
                            Success = false,
                            Message = $"Project {code} does not belong to the selected provider.",
                            Data = null
                        };
                    }
                }

                // Validate provider exists
                var provider = await _providerRepository.GetByProviderCode(employee.provider_code);
                if (provider == null)
                {
                    return new Response<employee>
                    {
                        Success = false,
                        Message = "Provider not found",
                        Data = null
                    };
                }

                // Normalize once so the p_projects JSON is clean (the SP TRIMs again defensively)
                foreach (var assignment in employee.assignments)
                {
                    assignment.project_code = assignment.project_code.Trim();
                    assignment.position = assignment.position.Trim();
                }

                var result = await _employeeRepository.Update(employee);
                if (result == null)
                {
                    // The row vanished between the existence check and the save — report
                    // the failure instead of answering success with a null payload.
                    return new Response<employee>
                    {
                        Success = false,
                        Message = "Error updating employee",
                        Data = null
                    };
                }

                _logger.LogInformation("Employee updated: {EmployeeId}", employee.employee_id);

                await _auditLogService.Log("employee", "update", employee.employee_id, existing, result, admin_employee_id);

                return new Response<employee>
                {
                    Success = true,
                    Message = "Employee updated successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee: {EmployeeId}", employee.employee_id);
                return new Response<employee>
                {
                    Success = false,
                    Message = "Error updating employee",
                    Data = null
                };
            }
        }

        public async Task<Response<bool>> SetInactive(string employee_id)
        {
            try
            {
                var result = await _employeeRepository.SetInactive(employee_id);
                _logger.LogInformation("Employee set inactive: {EmployeeId}", employee_id);

                return new Response<bool>
                {
                    Success = true,
                    Message = "Employee set inactive successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting employee inactive: {EmployeeId}", employee_id);
                return new Response<bool>
                {
                    Success = false,
                    Message = "Error setting employee inactive",
                    Data = false
                };
            }
        }

        public async Task<Response<int>> Delete(List<string> employee_ids, string admin_employee_id)
        {
            try
            {
                // Manual validation (request DTOs carry no data annotations by convention).
                if (employee_ids == null || employee_ids.Count == 0)
                {
                    return new Response<int> { Success = false, Message = "No employee IDs were provided.", Data = 0 };
                }

                // Normalize: trim, drop blanks, reject comma-bearing ids (the SP
                // receives the ids as a CSV matched with FIND_IN_SET), de-duplicate.
                var ids = employee_ids
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id.Trim())
                    .Where(id => !id.Contains(','))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                if (ids.Count == 0)
                {
                    return new Response<int> { Success = false, Message = "No valid employee IDs were provided.", Data = 0 };
                }

                // Fetch existing (not-yet-deleted) employees: audit trail data_from +
                // skip ids that are unknown or already deleted (the lookup SP filters is_deleted = 0).
                var existing = new List<employee>();
                foreach (var id in ids)
                {
                    var employee = await _employeeRepository.GetByEmployeeId(id);
                    if (employee != null)
                    {
                        existing.Add(employee);
                    }
                }

                if (existing.Count == 0)
                {
                    return new Response<int> { Success = false, Message = "No employees were found to delete (they may have already been deleted).", Data = 0 };
                }

                var result = await _employeeRepository.Delete(existing.Select(c => c.employee_id));
                var deletedCount = result?.deleted_count ?? 0;

                _logger.LogInformation("Admin {AdminId} soft-deleted {DeletedCount} employee(s)", admin_employee_id, deletedCount);

                var skipped = ids.Count - deletedCount;

                if (deletedCount > 0)
                {
                    // One audit entry for the call (mirrors timelog/project delete: previous
                    // state in data_from, null data_to). audit_log.reference_id is VARCHAR(100),
                    // so bulk deletes use a count summary instead of the full id CSV.
                    // Skipped when the SP deleted nothing — a no-op is not a mutation.
                    var referenceId = existing.Count == 1 ? existing[0].employee_id : $"{existing.Count} employees";
                    await _auditLogService.Log("employee", "delete", referenceId, existing, null, admin_employee_id);
                }

                return new Response<int>
                {
                    Success = deletedCount > 0,
                    Message = deletedCount > 0
                        ? $"Deleted {deletedCount} employee(s) successfully"
                            + (skipped > 0 ? $" ({skipped} skipped - not found or already deleted)" : string.Empty)
                        : "No employees were deleted (they may have already been deleted)",
                    Data = deletedCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employees ({Count} ids)", employee_ids?.Count ?? 0);
                return new Response<int> { Success = false, Message = "Error deleting employees", Data = 0 };
            }
        }

        public async Task<Response<IEnumerable<employee>>> GetByProjectCode(string project_code)
        {
            try
            {
                var employees = await _employeeRepository.GetByProjectCode(project_code);
                return new Response<IEnumerable<employee>>
                {
                    Success = true,
                    Message = "Employees retrieved successfully",
                    Data = employees
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees for project: {ProjectCode}", project_code);
                return new Response<IEnumerable<employee>>
                {
                    Success = false,
                    Message = "Error retrieving employees"
                };
            }
        }

        public async Task<Response<int>> GetActiveCount()
        {
            try
            {
                var count = await _employeeRepository.GetActiveCount();
                return new Response<int>
                {
                    Success = true,
                    Message = "Active employees count retrieved successfully",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active employees count");
                return new Response<int>
                {
                    Success = false,
                    Message = "Error retrieving active employees count",
                    Data = 0
                };
            }
        }

        /// <summary>
        /// Project-status cascade: set every active employee whose LAST active
        /// project is this one to In-Active - employees still assigned to another
        /// active project are skipped (they keep working). Each deactivation is
        /// audit-logged as employee/deactivate_by_project so a later project
        /// re-activation can restore exactly these employees. Mirrors the nightly
        /// expiry sweep (sp_project_DeactivateExpired) for manual admin deactivation.
        /// </summary>
        public async Task<Response<int>> CascadeDeactivateByProject(string project_code, string admin_employee_id)
        {
            try
            {
                // GetByProjectCode returns active employees assigned to the project plus
                // other_active_project_count - the multi-project skip flag.
                var employees = await _employeeRepository.GetByProjectCode(project_code);
                var deactivated = 0;

                foreach (var employee in employees)
                {
                    if ((employee.other_active_project_count ?? 0) > 0)
                    {
                        // Still assigned to another active project - stays active.
                        continue;
                    }

                    // Re-fetch the full row: GetByProjectCode returns a slim projection,
                    // and Update() rewrites every column including the project_codes
                    // mappings, so the round-trip must carry complete data.
                    var full = await _employeeRepository.GetByEmployeeId(employee.employee_id);
                    if (full == null || full.active != 1)
                    {
                        continue;
                    }

                    var previous = CloneEmployee(full);
                    full.active = 0;
                    var updated = await _employeeRepository.Update(full);

                    if (updated != null)
                    {
                        deactivated++;
                        await _auditLogService.Log(
                            "employee", "deactivate_by_project", full.employee_id,
                            previous, updated, admin_employee_id);
                    }
                }

                _logger.LogInformation("Project cascade set {Count} employee(s) inactive: {ProjectCode}",
                    deactivated, project_code);

                return new Response<int>
                {
                    Success = true,
                    Message = $"{deactivated} employee(s) deactivated",
                    Data = deactivated
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cascading employee deactivation for project: {ProjectCode}", project_code);
                return new Response<int>
                {
                    Success = false,
                    Message = "Error deactivating employees",
                    Data = 0
                };
            }
        }

        /// <summary>
        /// Restores employees that were deactivated BY A PROJECT CASCADE for this
        /// project; the audit log is the source of truth. A employee is restored
        /// only when it is currently In-Active, still belongs to this project, and
        /// its latest active 1-&gt;0 audit transition is a deactivate_by_project entry
        /// for this project. Employees deactivated individually by an admin (their
        /// latest transition is a manual employee/update) stay In-Active.
        /// </summary>
        public async Task<Response<int>> CascadeReactivateByProject(string project_code, string admin_employee_id)
        {
            try
            {
                // Candidates come from the audit trail, not GetByProjectCode (that
                // stored procedure returns active employees only).
                var cascadeEntries = await _auditLogService.GetByEntity("employee", "deactivate_by_project");
                var updateEntries = await _auditLogService.GetByEntity("employee", "update");

                // employee_id -> latest audit entry that flipped active from 1 to 0
                var latestDeactivation = new Dictionary<string, audit_log>();
                TrackLatestDeactivation(cascadeEntries, latestDeactivation);
                TrackLatestDeactivation(updateEntries, latestDeactivation);

                var reactivated = 0;

                foreach (var (employee_id, entry) in latestDeactivation)
                {
                    if (!string.Equals(entry.action, "deactivate_by_project", StringComparison.Ordinal))
                    {
                        // Latest deactivation was a manual admin edit - leave In-Active.
                        continue;
                    }

                    if (!CascadeEntryMatchesProject(entry.data_to, project_code))
                    {
                        // The cascade belonged to a different project.
                        continue;
                    }

                    var employee = await _employeeRepository.GetByEmployeeId(employee_id);
                    if (employee == null || employee.active != 0 ||
                        !IsAssignedToProject(employee.project_codes, project_code))
                    {
                        // Deleted, already active, or no longer assigned to this project.
                        continue;
                    }

                    var previous = CloneEmployee(employee);
                    employee.active = 1;
                    var updated = await _employeeRepository.Update(employee);

                    if (updated != null)
                    {
                        reactivated++;
                        await _auditLogService.Log(
                            "employee", "reactivate_by_project", employee.employee_id,
                            previous, updated, admin_employee_id);
                    }
                }

                _logger.LogInformation("Project re-activation restored {Count} employee(s): {ProjectCode}",
                    reactivated, project_code);

                return new Response<int>
                {
                    Success = true,
                    Message = $"{reactivated} employee(s) re-activated",
                    Data = reactivated
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring cascade-deactivated employees for project: {ProjectCode}", project_code);
                return new Response<int>
                {
                    Success = false,
                    Message = "Error re-activating employees",
                    Data = 0
                };
            }
        }

        /// <summary>
        /// Bulk-import employees for a single provider/project from a parsed file.
        /// Provider/project are validated once (and must belong together); each row is
        /// then processed via CreateOrMerge so it reuses the same field + DOLE 18+
        /// validation and the {provider_code}-NNNNNN auto employee_id generation. A row
        /// whose name + birthdate is already enrolled under the provider is merged into
        /// that existing account (details updated, the batch project/position added or
        /// replaced) instead of failing; merged rows are reported separately via
        /// merged_count/merged_employee_ids. Invalid rows are collected with row number +
        /// reason. One audit_log batch row is written regardless of partial failures.
        /// </summary>
        public async Task<Response<bulk_enrollment_result>> BulkCreate(bulk_enrollment_request request, string admin_employee_id)
        {
            var result = new bulk_enrollment_result
            {
                provider_code = request?.provider_code,
                project_code = request?.project_code,
                file_name = request?.file_name
            };

            try
            {
                if (request == null || request.employees == null || request.employees.Count == 0)
                {
                    return new Response<bulk_enrollment_result>
                    {
                        Success = false,
                        Message = "No employee rows were provided for import.",
                        Data = result
                    };
                }

                result.total = request.employees.Count;

                // Fail fast: the modal's provider/project must be valid and belong together.
                if (string.IsNullOrWhiteSpace(request.provider_code) || string.IsNullOrWhiteSpace(request.project_code))
                {
                    return new Response<bulk_enrollment_result>
                    {
                        Success = false,
                        Message = "Provider and Project are required.",
                        Data = result
                    };
                }

                var project = await _projectRepository.GetByProjectCode(request.project_code);
                if (project == null)
                {
                    return new Response<bulk_enrollment_result>
                    {
                        Success = false,
                        Message = "Selected project was not found.",
                        Data = result
                    };
                }

                if (!string.Equals(project.provider_code, request.provider_code, StringComparison.OrdinalIgnoreCase))
                {
                    return new Response<bulk_enrollment_result>
                    {
                        Success = false,
                        Message = "The selected project does not belong to the selected provider.",
                        Data = result
                    };
                }

                var provider = await _providerRepository.GetByProviderCode(request.provider_code);
                if (provider == null)
                {
                    return new Response<bulk_enrollment_result>
                    {
                        Success = false,
                        Message = "Selected provider was not found.",
                        Data = result
                    };
                }

                // Insert each row. Create() enforces required fields, DOLE 18+ birthdate,
                // and provider/project existence; gender is checked here because Create()
                // does not validate it. Accepted values are normalized to canonical
                // casing ("MALE"/"male" -> "Male") so the column stays consistent.
                foreach (var row in request.employees)
                {
                    var trimmedGender = (row.gender ?? string.Empty).Trim();
                    var normalizedGender =
                        string.Equals(trimmedGender, "Male", StringComparison.OrdinalIgnoreCase) ? "Male" :
                        string.Equals(trimmedGender, "Female", StringComparison.OrdinalIgnoreCase) ? "Female" :
                        trimmedGender;   // rejected by the validation below
                    if (!string.Equals(normalizedGender, "Male", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(normalizedGender, "Female", StringComparison.OrdinalIgnoreCase))
                    {
                        result.error_count++;
                        result.errors.Add(new bulk_enrollment_error
                        {
                            row = row.row_number,
                            name = row.name,
                            message = "Gender must be Male or Female."
                        });
                        continue;
                    }

                    var employee = new employee
                    {
                        name = row.name,
                        gender = normalizedGender,
                        birthdate = row.birthdate,
                        contact_number = row.contact_number,
                        address = row.address,
                        provider_code = request.provider_code,
                        // One assignment per batch: the row's position is the employee's
                        // position ON the batch project; more can be added via edit.
                        assignments = new List<employee_project_assignment>
                        {
                            new() { project_code = request.project_code, position = row.position ?? string.Empty }
                        },
                        active = 1
                    };

                    // Per-row audit is suppressed here; BulkCreate writes one summary
                    // bulk_enrollment audit entry for the whole batch (see below).
                    var (created, merged) = await CreateOrMerge(employee, admin_employee_id, log_audit: false);
                    if (created.Success && created.Data != null)
                    {
                        result.success_count++;
                        if (merged)
                        {
                            result.merged_count++;
                            result.merged_employee_ids.Add(created.Data.employee_id);
                        }
                        else
                        {
                            result.enrolled_employee_ids.Add(created.Data.employee_id);
                        }
                    }
                    else
                    {
                        result.error_count++;
                        result.errors.Add(new bulk_enrollment_error
                        {
                            row = row.row_number,
                            name = row.name,
                            message = created.Message ?? "Could not enroll this employee."
                        });
                    }
                }

                // Record the batch in the reusable audit log (best-effort: never fail the
                // import if logging itself fails).
                var logResponse = await _auditLogService.Log(
                    "bulk_enrollment",
                    "bulk_create",
                    request.project_code,
                    null,
                    result,
                    admin_employee_id);

                if (logResponse.Success && logResponse.Data != null)
                {
                    result.log_id = logResponse.Data.log_id;
                }

                _logger.LogInformation(
                    "Bulk enrollment complete: {Success} succeeded, {Errors} failed (project {ProjectCode}, admin {Admin})",
                    result.success_count, result.error_count, request.project_code, admin_employee_id);

                var createdCount = result.success_count - result.merged_count;
                var mergedSuffix = result.merged_count > 0 ? $" and updated {result.merged_count} existing" : string.Empty;

                return new Response<bulk_enrollment_result>
                {
                    Success = true,
                    Message = $"Enrolled {createdCount} of {result.total} employee(s){mergedSuffix}; {result.error_count} failed.",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk enrollment (project {ProjectCode})", request?.project_code);
                return new Response<bulk_enrollment_result>
                {
                    Success = false,
                    Message = "An error occurred during bulk enrollment.",
                    Data = result
                };
            }
        }

        /// <summary>
        /// Shallow copy so audit data_from captures the pre-change state before the
        /// cascade methods mutate the entity in place.
        /// </summary>
        private static employee CloneEmployee(employee source) => new()
        {
            employee_id = source.employee_id,
            name = source.name,
            gender = source.gender,
            birthdate = source.birthdate,
            contact_number = source.contact_number,
            address = source.address,
            project_codes = source.project_codes,
            provider_code = source.provider_code,
            provider_name = source.provider_name,
            project_names = source.project_names,
            areas = source.areas,
            positions = source.positions,
            project_positions = source.project_positions,
            assignments = source.assignments,
            active_project_count = source.active_project_count,
            active = source.active,
            create_at = source.create_at,
            update_at = source.update_at
        };

        /// <summary>
        /// Parses the read-derived project_positions JSON map ({"code":"position"}) into
        /// assignments. Mirrors the repository's EnsureAssignmentsHydrated, but returns
        /// null on an unparseable snapshot so the merge can abort instead of saving (an
        /// empty list fed to Update would wipe all mappings).
        /// </summary>
        private static List<employee_project_assignment>? ParseProjectPositions(string? projectPositions)
        {
            if (string.IsNullOrWhiteSpace(projectPositions))
            {
                return new List<employee_project_assignment>();
            }

            try
            {
                var map = JsonSerializer.Deserialize<Dictionary<string, string>>(projectPositions);
                return map?
                    .Select(kv => new employee_project_assignment
                    {
                        project_code = kv.Key,
                        position = kv.Value ?? string.Empty
                    })
                    .ToList() ?? new List<employee_project_assignment>();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>True when the project_codes CSV contains the given project code.</summary>
        private static bool IsAssignedToProject(string? projectCodesCsv, string project_code)
        {
            if (string.IsNullOrWhiteSpace(projectCodesCsv))
            {
                return false;
            }

            return projectCodesCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(code => code.Trim())
                .Contains(project_code, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when a deactivate_by_project audit entry belongs to this project. The
        /// SQL expiry sweep writes a single "project_code" key; the C# cascade serializes
        /// the employee entity, which carries the multi-project "project_codes" CSV.
        /// </summary>
        private static bool CascadeEntryMatchesProject(string? dataToJson, string project_code)
        {
            if (TryGetJsonString(dataToJson, "project_code", out var single) &&
                string.Equals(single, project_code, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return TryGetJsonString(dataToJson, "project_codes", out var csv) &&
                IsAssignedToProject(csv, project_code);
        }

        /// <summary>
        /// Folds audit entries into latestDeactivation, keeping the highest log_id
        /// per employee for entries that flipped active from 1 to 0.
        /// </summary>
        private static void TrackLatestDeactivation(
            Response<IEnumerable<audit_log>>? response,
            Dictionary<string, audit_log> latestDeactivation)
        {
            if (response?.Success != true || response.Data == null)
            {
                return;
            }

            foreach (var entry in response.Data)
            {
                if (string.IsNullOrWhiteSpace(entry.reference_id) || !IsActiveDeactivation(entry))
                {
                    continue;
                }

                if (latestDeactivation.TryGetValue(entry.reference_id, out var current) && current.log_id >= entry.log_id)
                {
                    continue;
                }

                latestDeactivation[entry.reference_id] = entry;
            }
        }

        /// <summary>
        /// True when the entry flipped active from 1 (data_from) to 0 (data_to).
        /// Handles both C#-serialized (indented) and MySQL JSON_OBJECT (compact)
        /// payloads; edits that leave active unchanged are not transitions.
        /// </summary>
        private static bool IsActiveDeactivation(audit_log entry)
        {
            return TryGetJsonInt(entry.data_from, "active", out var fromActive) && fromActive == 1
                && TryGetJsonInt(entry.data_to, "active", out var toActive) && toActive == 0;
        }

        private static bool TryGetJsonInt(string? json, string property, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.TryGetProperty(property, out var element)
                    && element.ValueKind == JsonValueKind.Number)
                {
                    return element.TryGetInt32(out value);
                }
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryGetJsonString(string? json, string property, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.TryGetProperty(property, out var element)
                    && element.ValueKind == JsonValueKind.String)
                {
                    value = element.GetString() ?? string.Empty;
                    return value != string.Empty;
                }
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}