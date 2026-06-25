using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public class SystemConfigService : ISystemConfigService
    {
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly ILogger<SystemConfigService> _logger;

        public SystemConfigService(
            ISystemConfigRepository systemConfigRepository,
            ILogger<SystemConfigService> logger)
        {
            _systemConfigRepository = systemConfigRepository;
            _logger = logger;
        }

        public async Task<Response<int>> GetDebounceThresholdMinutes()
        {
            try
            {
                var config = await _systemConfigRepository.GetByKey("DebounceThresholdMinutes");
                if (config == null)
                {
                    return new Response<int>
                    {
                        Success = false,
                        Message = "DebounceThresholdMinutes configuration not found",
                        Data = 15 // default
                    };
                }

                if (int.TryParse(config.value, out int threshold))
                {
                    return new Response<int>
                    {
                        Success = true,
                        Message = "Configuration retrieved successfully",
                        Data = threshold
                    };
                }

                return new Response<int>
                {
                    Success = false,
                    Message = "Invalid configuration value",
                    Data = 15 // default
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting debounce threshold");
                return new Response<int>
                {
                    Success = false,
                    Message = "Error retrieving configuration",
                    Data = 15 // default
                };
            }
        }

        public async Task<Response<int>> GetHealthDeclarationValidityHours()
        {
            try
            {
                var config = await _systemConfigRepository.GetByKey("HealthDeclarationValidityHours");
                if (config == null)
                {
                    return new Response<int>
                    {
                        Success = false,
                        Message = "HealthDeclarationValidityHours configuration not found",
                        Data = 12 // default
                    };
                }

                if (int.TryParse(config.value, out int hours))
                {
                    return new Response<int>
                    {
                        Success = true,
                        Message = "Configuration retrieved successfully",
                        Data = hours
                    };
                }

                return new Response<int>
                {
                    Success = false,
                    Message = "Invalid configuration value",
                    Data = 12 // default
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health declaration validity hours");
                return new Response<int>
                {
                    Success = false,
                    Message = "Error retrieving configuration",
                    Data = 12 // default
                };
            }
        }

        public async Task<Response<string>> GetAdminADGroup()
        {
            try
            {
                var config = await _systemConfigRepository.GetByKey("AdminADGroup");
                if (config == null)
                {
                    return new Response<string>
                    {
                        Success = false,
                        Message = "AdminADGroup configuration not found",
                        Data = "app.your_app.admin" // default
                    };
                }

                return new Response<string>
                {
                    Success = true,
                    Message = "Configuration retrieved successfully",
                    Data = config.value
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin AD group");
                return new Response<string>
                {
                    Success = false,
                    Message = "Error retrieving configuration",
                    Data = "app.your_app.admin" // default
                };
            }
        }

        public async Task<Response<system_config>> GetByKey(string key)
        {
            try
            {
                var config = await _systemConfigRepository.GetByKey(key);
                if (config == null)
                {
                    return new Response<system_config>
                    {
                        Success = false,
                        Message = "Configuration not found",
                        Data = null
                    };
                }

                return new Response<system_config>
                {
                    Success = true,
                    Message = "Configuration retrieved successfully",
                    Data = config
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration by key: {Key}", key);
                return new Response<system_config>
                {
                    Success = false,
                    Message = "Error retrieving configuration",
                    Data = null
                };
            }
        }

        public async Task<Response<bool>> Update(system_config config)
        {
            try
            {
                var result = await _systemConfigRepository.Update(config);
                return new Response<bool>
                {
                    Success = true,
                    Message = "Configuration updated successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration");
                return new Response<bool>
                {
                    Success = false,
                    Message = "Error updating configuration",
                    Data = false
                };
            }
        }

        public async Task<Response<int>> GetHealthDeclarationWindowMinutes()
        {
            try
            {
                var config = await _systemConfigRepository.GetByKey("HealthDeclarationWindowMinutes");
                if (config == null)
                {
                    return new Response<int>
                    {
                        Success = false,
                        Message = "HealthDeclarationWindowMinutes configuration not found",
                        Data = 2 // default to 2 minutes
                    };
                }

                if (int.TryParse(config.value, out int minutes))
                {
                    return new Response<int>
                    {
                        Success = true,
                        Message = "Configuration retrieved successfully",
                        Data = minutes
                    };
                }

                return new Response<int>
                {
                    Success = false,
                    Message = "Invalid configuration value",
                    Data = 2 // default
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health declaration window minutes");
                return new Response<int>
                {
                    Success = false,
                    Message = "Error retrieving configuration",
                    Data = 2 // default
                };
            }
        }

        public async Task<Response<IEnumerable<system_config>>> GetAll()
        {
            try
            {
                var configs = await _systemConfigRepository.GetAll();
                return new Response<IEnumerable<system_config>>
                {
                    Success = true,
                    Message = "All configurations retrieved successfully",
                    Data = configs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all configurations");
                return new Response<IEnumerable<system_config>>
                {
                    Success = false,
                    Message = "Error retrieving configurations",
                    Data = null
                };
            }
        }
    }
}