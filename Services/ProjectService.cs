using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IProviderRepository _providerRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IContractorService _contractorService;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(
            IProjectRepository projectRepository,
            IProviderRepository providerRepository,
            IAuditLogService auditLogService,
            IContractorService contractorService,
            ILogger<ProjectService> logger)
        {
            _projectRepository = projectRepository;
            _providerRepository = providerRepository;
            _auditLogService = auditLogService;
            _contractorService = contractorService;
            _logger = logger;
        }

        public async Task<Response<project>> GetByProjectCode(string project_code)
        {
            try
            {
                var project = await _projectRepository.GetByProjectCode(project_code);
                if (project == null)
                {
                    return new Response<project>
                    {
                        Success = false,
                        Message = "Project not found",
                        Data = null
                    };
                }

                return new Response<project>
                {
                    Success = true,
                    Message = "Project retrieved successfully",
                    Data = project
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project: {ProjectCode}", project_code);
                return new Response<project>
                {
                    Success = false,
                    Message = "Error retrieving project",
                    Data = null
                };
            }
        }

        public async Task<Response<IEnumerable<project>>> GetAll()
        {
            try
            {
                var projects = await _projectRepository.GetAll();
                return new Response<IEnumerable<project>>
                {
                    Success = true,
                    Message = "Projects retrieved successfully",
                    Data = projects
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all projects");
                return new Response<IEnumerable<project>>
                {
                    Success = false,
                    Message = "Error retrieving projects",
                    Data = null
                };
            }
        }

        public async Task<Response<IEnumerable<project>>> GetByProviderCode(string provider_code)
        {
            try
            {
                // Validate provider exists
                var provider = await _providerRepository.GetByProviderCode(provider_code);
                if (provider == null)
                {
                    return new Response<IEnumerable<project>>
                    {
                        Success = false,
                        Message = "Provider not found",
                        Data = null
                    };
                }

                var projects = await _projectRepository.GetByProviderCode(provider_code);
                return new Response<IEnumerable<project>>
                {
                    Success = true,
                    Message = "Projects retrieved successfully",
                    Data = projects
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects for provider: {ProviderCode}", provider_code);
                return new Response<IEnumerable<project>>
                {
                    Success = false,
                    Message = "Error retrieving projects",
                    Data = null
                };
            }
        }

        public async Task<Response<project>> Create(project project, string admin_employee_id)
        {
            try
            {
                // Validate required fields
                var validationError = GetValidationError(project);
                if (validationError != null)
                {
                    return new Response<project>
                    {
                        Success = false,
                        Message = validationError,
                        Data = null
                    };
                }

                // Validate provider exists
                var provider = await _providerRepository.GetByProviderCode(project.provider_code);
                if (provider == null)
                {
                    return new Response<project>
                    {
                        Success = false,
                        Message = "Provider not found",
                        Data = null
                    };
                }

                // Note: project_code uniqueness validation removed since stored procedure auto-generates project_code
                // Format: ProviderCode-YY-### (e.g., PROV-001-26-005) which is guaranteed unique
                var result = await _projectRepository.Create(project);
                _logger.LogInformation("Project created: {ProjectCode}", project.project_code);

                if (result != null)
                {
                    await _auditLogService.Log("project", "create", result.project_code, null, result, admin_employee_id);
                }

                return new Response<project>
                {
                    Success = result != null,
                    Message = result != null ? "Project created successfully" : "Project creation failed",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project: {ProjectCode}", project.project_code);
                return new Response<project>
                {
                    Success = false,
                    Message = "Error creating project",
                    Data = null
                };
            }
        }

        public async Task<Response<project>> Update(project project, string admin_employee_id)
        {
            try
            {
                // Validate required fields
                var validationError = GetValidationError(project);
                if (validationError != null)
                {
                    return new Response<project>
                    {
                        Success = false,
                        Message = validationError,
                        Data = null
                    };
                }

                // Validate provider exists
                var provider = await _providerRepository.GetByProviderCode(project.provider_code);
                if (provider == null)
                {
                    return new Response<project>
                    {
                        Success = false,
                        Message = "Provider not found",
                        Data = null
                    };
                }

                // Validate project exists
                var existing = await _projectRepository.GetByProjectCode(project.project_code);
                if (existing == null)
                {
                    return new Response<project>
                    {
                        Success = false,
                        Message = "Project not found",
                        Data = null
                    };
                }

                // A project cannot be set to Active while its contract is already
                // expired - the nightly expiry event (sp_project_DeactivateExpired)
                // would immediately deactivate it again. Extend the end date first.
                if (project.active == 1 && project.contract_enddate < DateTime.Today)
                {
                    return new Response<project>
                    {
                        Success = false,
                        Message = "Cannot set the project to Active: the contract end date is already expired. Extend the contract end date first.",
                        Data = null
                    };
                }

                var result = await _projectRepository.Update(project);
                _logger.LogInformation("Project updated: {ProjectCode}", project.project_code);

                var message = result != null ? "Project updated successfully" : "Project update failed";

                if (result != null)
                {
                    await _auditLogService.Log("project", "update", project.project_code, existing, result, admin_employee_id);

                    // Cascade contractor status with the project so the kiosk gate and
                    // the admin lists stay consistent. Deactivation takes down the
                    // project's active contractors; re-activation restores ONLY the
                    // contractors that were deactivated by a cascade (audit-verified) -
                    // contractors an admin deactivated individually stay In-Active.
                    if (existing.active == 1 && result.active == 0)
                    {
                        var cascade = await _contractorService.CascadeDeactivateByProject(project.project_code, admin_employee_id);
                        if (cascade.Success && cascade.Data > 0)
                        {
                            message += $" - {cascade.Data} contractor(s) deactivated";
                        }
                    }
                    else if (existing.active == 0 && result.active == 1)
                    {
                        var restore = await _contractorService.CascadeReactivateByProject(project.project_code, admin_employee_id);
                        if (restore.Success && restore.Data > 0)
                        {
                            message += $" - {restore.Data} contractor(s) re-activated";
                        }
                    }
                }

                return new Response<project>
                {
                    Success = result != null,
                    Message = message,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project: {ProjectCode}", project.project_code);
                return new Response<project>
                {
                    Success = false,
                    Message = "Error updating project",
                    Data = null
                };
            }
        }

        public async Task<Response<bool>> SetInactive(string project_code)
        {
            try
            {
                var result = await _projectRepository.SetInactive(project_code);
                _logger.LogInformation("Project set inactive: {ProjectCode}", project_code);

                return new Response<bool>
                {
                    Success = true,
                    Message = "Project set inactive successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting project inactive: {ProjectCode}", project_code);
                return new Response<bool>
                {
                    Success = false,
                    Message = "Error setting project inactive",
                    Data = false
                };
            }
        }

        // Soft delete via sp_project_Delete: all enrolled employees are marked
        // is_deleted = 1 first, then the project — one transaction, no rows removed.
        public async Task<Response<bool>> Delete(string project_code, string admin_employee_id)
        {
            try
            {
                // Validate project exists (deleted projects are excluded by the SP, so a
                // second delete of the same code degrades to "not found")
                var existing = await _projectRepository.GetByProjectCode(project_code);
                if (existing == null)
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = "Project not found",
                        Data = false
                    };
                }

                var result = await _projectRepository.Delete(project_code);
                if (result == null || !result.success)
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = "Project delete failed",
                        Data = false
                    };
                }

                _logger.LogInformation(
                    "Project soft-deleted: {ProjectCode} ({DeletedEmployees} employee(s) marked deleted)",
                    project_code, result.deleted_employees);

                await _auditLogService.Log(
                    "project",
                    "delete",
                    project_code,
                    existing,
                    new { deleted_employees = result.deleted_employees },
                    admin_employee_id);

                return new Response<bool>
                {
                    Success = true,
                    Message = $"Project deleted successfully ({result.deleted_employees} enrolled employee(s) also removed)",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project: {ProjectCode}", project_code);
                return new Response<bool>
                {
                    Success = false,
                    Message = "Error deleting project",
                    Data = false
                };
            }
        }

        public async Task<Response<int>> GetActiveCount()
        {
            try
            {
                var count = await _projectRepository.GetActiveCount();
                return new Response<int>
                {
                    Success = true,
                    Message = "Active projects count retrieved successfully",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active projects count");
                return new Response<int>
                {
                    Success = false,
                    Message = "Error retrieving active projects count",
                    Data = 0
                };
            }
        }

        /// <summary>
        /// Validates required project fields. Returns an error message, or null when all fields are valid.
        /// </summary>
        private static string? GetValidationError(project project)
        {
            if (string.IsNullOrWhiteSpace(project.provider_name))
            {
                return "Provider name is required";
            }

            if (string.IsNullOrWhiteSpace(project.provider_pic))
            {
                return "Provider Project PIC is required";
            }

            if (string.IsNullOrWhiteSpace(project.provider_pic_number))
            {
                return "Contact No is required";
            }

            if (string.IsNullOrWhiteSpace(project.project_name))
            {
                return "Project name is required";
            }

            if (project.contract_startdate == null)
            {
                return "Contract start date is required";
            }

            if (project.contract_enddate == null)
            {
                return "Contract end date is required";
            }

            if (project.contract_startdate > project.contract_enddate)
            {
                return "Contract Start Date cannot be later than Contract End Date";
            }

            return null;
        }
    }
}