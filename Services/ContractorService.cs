using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public class ContractorService : IContractorService
    {
        private readonly IContractorEmployeeRepository _contractorRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IProviderRepository _providerRepository;
        private readonly ILogger<ContractorService> _logger;

        public ContractorService(
            IContractorEmployeeRepository contractorRepository,
            IProjectRepository projectRepository,
            IProviderRepository providerRepository,
            ILogger<ContractorService> logger)
        {
            _contractorRepository = contractorRepository;
            _projectRepository = projectRepository;
            _providerRepository = providerRepository;
            _logger = logger;
        }

        public async Task<Response<contractor_employee>> GetByEmployeeId(string employee_id)
        {
            try
            {
                var contractor = await _contractorRepository.GetByEmployeeId(employee_id);
                if (contractor == null)
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Contractor not found",
                        Data = null
                    };
                }

                return new Response<contractor_employee>
                {
                    Success = true,
                    Message = "Contractor retrieved successfully",
                    Data = contractor
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contractor: {EmployeeId}", employee_id);
                return new Response<contractor_employee>
                {
                    Success = false,
                    Message = "Error retrieving contractor",
                    Data = null
                };
            }
        }

        public async Task<Response<IEnumerable<contractor_employee>>> GetAll()
        {
            try
            {
                var contractors = await _contractorRepository.GetAll();
                return new Response<IEnumerable<contractor_employee>>
                {
                    Success = true,
                    Message = "Contractors retrieved successfully",
                    Data = contractors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all contractors");
                return new Response<IEnumerable<contractor_employee>>
                {
                    Success = false,
                    Message = "Error retrieving contractors",
                    Data = null
                };
            }
        }

        public async Task<Response<contractor_employee>> Create(contractor_employee employee)
        {
            try
            {
                // Validate required fields
                if (employee == null)
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Contractor data is required",
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(employee.employee_id))
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Employee ID is required",
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(employee.name))
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Name is required",
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(employee.project_code))
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Project code is required",
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(employee.provider_code))
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Provider code is required",
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(employee.position))
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Position is required",
                        Data = null
                    };
                }

                // Validate employee_id doesn't already exist
                var existing = await _contractorRepository.GetByEmployeeId(employee.employee_id);
                if (existing != null)
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Contractor with this Employee ID already exists",
                        Data = null
                    };
                }

                // Validate project exists
                var project = await _projectRepository.GetByProjectCode(employee.project_code);
                if (project == null)
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Project not found",
                        Data = null
                    };
                }

                // Validate provider exists
                var provider = await _providerRepository.GetByProviderCode(employee.provider_code);
                if (provider == null)
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Provider not found",
                        Data = null
                    };
                }

                var result = await _contractorRepository.Create(employee);
                _logger.LogInformation("Contractor created: {EmployeeId}", employee.employee_id);

                return new Response<contractor_employee>
                {
                    Success = true,
                    Message = "Contractor created successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contractor: {EmployeeId}", employee.employee_id);
                return new Response<contractor_employee>
                {
                    Success = false,
                    Message = "Error creating contractor",
                    Data = null
                };
            }
        }

        public async Task<Response<contractor_employee>> Update(contractor_employee employee)
        {
            try
            {
                // Validate required fields
                if (employee == null)
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Contractor data is required",
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(employee.employee_id))
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Employee ID is required",
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(employee.name))
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Name is required",
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(employee.project_code))
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Project code is required",
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(employee.provider_code))
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Provider code is required",
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(employee.position))
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Position is required",
                        Data = null
                    };
                }

                // Validate contractor exists
                var existing = await _contractorRepository.GetByEmployeeId(employee.employee_id);
                if (existing == null)
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Contractor not found",
                        Data = null
                    };
                }

                // Validate project exists
                var project = await _projectRepository.GetByProjectCode(employee.project_code);
                if (project == null)
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Project not found",
                        Data = null
                    };
                }

                // Validate provider exists
                var provider = await _providerRepository.GetByProviderCode(employee.provider_code);
                if (provider == null)
                {
                    return new Response<contractor_employee>
                    {
                        Success = false,
                        Message = "Provider not found",
                        Data = null
                    };
                }

                var result = await _contractorRepository.Update(employee);
                _logger.LogInformation("Contractor updated: {EmployeeId}", employee.employee_id);

                return new Response<contractor_employee>
                {
                    Success = true,
                    Message = "Contractor updated successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contractor: {EmployeeId}", employee.employee_id);
                return new Response<contractor_employee>
                {
                    Success = false,
                    Message = "Error updating contractor",
                    Data = null
                };
            }
        }

        public async Task<Response<bool>> SetInactive(string employee_id)
        {
            try
            {
                var result = await _contractorRepository.SetInactive(employee_id);
                _logger.LogInformation("Contractor set inactive: {EmployeeId}", employee_id);

                return new Response<bool>
                {
                    Success = true,
                    Message = "Contractor set inactive successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting contractor inactive: {EmployeeId}", employee_id);
                return new Response<bool>
                {
                    Success = false,
                    Message = "Error setting contractor inactive",
                    Data = false
                };
            }
        }

        public async Task<Response<IEnumerable<contractor_employee>>> GetByProjectCode(string project_code)
        {
            try
            {
                var contractors = await _contractorRepository.GetByProjectCode(project_code);
                return new Response<IEnumerable<contractor_employee>>
                {
                    Success = true,
                    Message = "Contractors retrieved successfully",
                    Data = contractors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contractors for project: {ProjectCode}", project_code);
                return new Response<IEnumerable<contractor_employee>>
                {
                    Success = false,
                    Message = "Error retrieving contractors"
                };
            }
        }

        public async Task<Response<int>> GetActiveCount()
        {
            try
            {
                var count = await _contractorRepository.GetActiveCount();
                return new Response<int>
                {
                    Success = true,
                    Message = "Active contractors count retrieved successfully",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active contractors count");
                return new Response<int>
                {
                    Success = false,
                    Message = "Error retrieving active contractors count",
                    Data = 0
                };
            }
        }
    }
}