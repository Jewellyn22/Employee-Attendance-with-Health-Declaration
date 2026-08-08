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

        public async Task<Response<double>> GetDebounceThresholdSeconds()
        {
            try
            {
                var config = await _systemConfigRepository.GetByKey("DebounceThresholdSeconds");
                if (config == null)
                {
                    return new Response<double>
                    {
                        Success = false,
                        Message = "DebounceThresholdSeconds configuration not found",
                        Data = 30.0 // default in seconds
                    };
                }

                if (double.TryParse(config.value, out double threshold))
                {
                    return new Response<double>
                    {
                        Success = true,
                        Message = "Configuration retrieved successfully",
                        Data = threshold
                    };
                }

                return new Response<double>
                {
                    Success = false,
                    Message = "Invalid configuration value",
                    Data = 30.0 // default in seconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting debounce threshold");
                return new Response<double>
                {
                    Success = false,
                    Message = "Error retrieving configuration",
                    Data = 30.0 // default in seconds
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

        public async Task<Response<bool>> GetScanInputReadOnly()
        {
            try
            {
                var config = await _systemConfigRepository.GetByKey("ScanInputReadOnly");
                if (config == null)
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = "ScanInputReadOnly configuration not found",
                        Data = true // default to readonly (safer)
                    };
                }

                if (bool.TryParse(config.value, out bool readOnly))
                {
                    return new Response<bool>
                    {
                        Success = true,
                        Message = "Configuration retrieved successfully",
                        Data = readOnly
                    };
                }

                return new Response<bool>
                {
                    Success = false,
                    Message = "Invalid configuration value",
                    Data = true // default to readonly (safer)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scan input readonly configuration");
                return new Response<bool>
                {
                    Success = false,
                    Message = "Error retrieving configuration",
                    Data = true // default to readonly (safer)
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

        public async Task<Response<double>> GetHealthDeclarationWindowSeconds()
        {
            try
            {
                var config = await _systemConfigRepository.GetByKey("HealthDeclarationWindowSeconds");
                if (config == null)
                {
                    return new Response<double>
                    {
                        Success = false,
                        Message = "HealthDeclarationWindowSeconds configuration not found",
                        Data = 120.0 // default to 120 seconds
                    };
                }

                if (double.TryParse(config.value, out double seconds))
                {
                    return new Response<double>
                    {
                        Success = true,
                        Message = "Configuration retrieved successfully",
                        Data = seconds
                    };
                }

                return new Response<double>
                {
                    Success = false,
                    Message = "Invalid configuration value",
                    Data = 120.0 // default in seconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health declaration window seconds");
                return new Response<double>
                {
                    Success = false,
                    Message = "Error retrieving configuration",
                    Data = 120.0 // default in seconds
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

        public async Task<Response<List<string>>> GetHealthDeclarationSicknessItems()
        {
            try
            {
                var config = await _systemConfigRepository.GetByKey("Health_Declaration_Sickness");

                // Default sickness items if config is missing or empty
                var defaultItems = new List<string>
                {
                    "Fever", "Cough", "Cold", "Body Pain", "Headache",
                    "Sore Throat", "Fatigue", "Nausea", "Diarrhea"
                };

                if (config == null || string.IsNullOrWhiteSpace(config.value))
                {
                    return new Response<List<string>>
                    {
                        Success = false,
                        Message = "Health_Declaration_Sickness configuration not found, using defaults",
                        Data = defaultItems
                    };
                }

                // Parse semicolon-separated values
                var items = config.value.Split(';')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                if (items.Count == 0)
                {
                    return new Response<List<string>>
                    {
                        Success = false,
                        Message = "Empty sickness configuration, using defaults",
                        Data = defaultItems
                    };
                }

                return new Response<List<string>>
                {
                    Success = true,
                    Message = "Sickness items retrieved successfully",
                    Data = items
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health declaration sickness items");
                return new Response<List<string>>
                {
                    Success = false,
                    Message = "Error retrieving sickness configuration",
                    Data = new List<string> { "Fever", "Cough", "Cold" } // minimal default
                };
            }
        }
    }
}