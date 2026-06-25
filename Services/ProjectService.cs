using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IProviderRepository _providerRepository;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(
            IProjectRepository projectRepository,
            IProviderRepository providerRepository,
            ILogger<ProjectService> logger)
        {
            _projectRepository = projectRepository;
            _providerRepository = providerRepository;
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

        public async Task<Response<project>> Create(project project)
        {
            try
            {
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

                // Validate project_code doesn't already exist
                var existing = await _projectRepository.GetByProjectCode(project.project_code);
                if (existing != null)
                {
                    return new Response<project>
                    {
                        Success = false,
                        Message = "Project with this Project Code already exists",
                        Data = null
                    };
                }

                var result = await _projectRepository.Create(project);
                _logger.LogInformation("Project created: {ProjectCode}", project.project_code);

                return new Response<project>
                {
                    Success = true,
                    Message = "Project created successfully",
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

        public async Task<Response<project>> Update(project project)
        {
            try
            {
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

                var result = await _projectRepository.Update(project);
                _logger.LogInformation("Project updated: {ProjectCode}", project.project_code);

                return new Response<project>
                {
                    Success = true,
                    Message = "Project updated successfully",
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
    }
}