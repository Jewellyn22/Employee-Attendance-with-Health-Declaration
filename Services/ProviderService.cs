using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Repositories;

namespace ContractorAttendanceWithHealthDeclaration.Services
{
    public class ProviderService : IProviderService
    {
        private readonly IProviderRepository _providerRepository;
        private readonly ILogger<ProviderService> _logger;

        public ProviderService(
            IProviderRepository providerRepository,
            ILogger<ProviderService> logger)
        {
            _providerRepository = providerRepository;
            _logger = logger;
        }

        public async Task<Response<provider>> GetByProviderCode(string provider_code)
        {
            try
            {
                var provider = await _providerRepository.GetByProviderCode(provider_code);
                if (provider == null)
                {
                    return new Response<provider>
                    {
                        Success = false,
                        Message = "Provider not found",
                        Data = null
                    };
                }

                return new Response<provider>
                {
                    Success = true,
                    Message = "Provider retrieved successfully",
                    Data = provider
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider: {ProviderCode}", provider_code);
                return new Response<provider>
                {
                    Success = false,
                    Message = "Error retrieving provider",
                    Data = null
                };
            }
        }

        public async Task<Response<IEnumerable<provider>>> GetAll()
        {
            try
            {
                var providers = await _providerRepository.GetAll();
                return new Response<IEnumerable<provider>>
                {
                    Success = true,
                    Message = "Providers retrieved successfully",
                    Data = providers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all providers");
                return new Response<IEnumerable<provider>>
                {
                    Success = false,
                    Message = "Error retrieving providers",
                    Data = null
                };
            }
        }

        public async Task<Response<provider>> Create(provider provider)
        {
            try
            {
                // Validate provider_code doesn't already exist
                var existing = await _providerRepository.GetByProviderCode(provider.provider_code);
                if (existing != null)
                {
                    return new Response<provider>
                    {
                        Success = false,
                        Message = "Provider with this Provider Code already exists",
                        Data = null
                    };
                }

                var result = await _providerRepository.Create(provider);
                _logger.LogInformation("Provider created: {ProviderCode}", provider.provider_code);

                return new Response<provider>
                {
                    Success = true,
                    Message = "Provider created successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating provider: {ProviderCode}", provider.provider_code);
                return new Response<provider>
                {
                    Success = false,
                    Message = "Error creating provider",
                    Data = null
                };
            }
        }

        public async Task<Response<provider>> Update(provider provider)
        {
            try
            {
                // Validate provider exists
                var existing = await _providerRepository.GetByProviderCode(provider.provider_code);
                if (existing == null)
                {
                    return new Response<provider>
                    {
                        Success = false,
                        Message = "Provider not found",
                        Data = null
                    };
                }

                var result = await _providerRepository.Update(provider);
                _logger.LogInformation("Provider updated: {ProviderCode}", provider.provider_code);

                return new Response<provider>
                {
                    Success = true,
                    Message = "Provider updated successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider: {ProviderCode}", provider.provider_code);
                return new Response<provider>
                {
                    Success = false,
                    Message = "Error updating provider",
                    Data = null
                };
            }
        }

        public async Task<Response<bool>> SetInactive(string provider_code)
        {
            try
            {
                var result = await _providerRepository.SetInactive(provider_code);
                _logger.LogInformation("Provider set inactive: {ProviderCode}", provider_code);

                return new Response<bool>
                {
                    Success = true,
                    Message = "Provider set inactive successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting provider inactive: {ProviderCode}", provider_code);
                return new Response<bool>
                {
                    Success = false,
                    Message = "Error setting provider inactive",
                    Data = false
                };
            }
        }

        public async Task<Response<int>> GetActiveCount()
        {
            try
            {
                var count = await _providerRepository.GetActiveCount();
                return new Response<int>
                {
                    Success = true,
                    Message = "Active providers count retrieved successfully",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active providers count");
                return new Response<int>
                {
                    Success = false,
                    Message = "Error retrieving active providers count",
                    Data = 0
                };
            }
        }
    }
}