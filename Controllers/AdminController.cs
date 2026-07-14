using ContractorAttendanceWithHealthDeclaration.Attributes;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContractorAttendanceWithHealthDeclaration.Controllers
{
    [AuthorizeAdmin]
    [NoCache]
    public class AdminController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IContractorService _contractorService;
        private readonly ISystemConfigService _systemConfigService;
        private readonly ITimeLogsManagementService _timeLogsManagementService;
        private readonly IProviderService _providerService;
        private readonly IHistoryLogsService _historyLogsService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IProjectService projectService,
            IContractorService contractorService,
            ISystemConfigService systemConfigService,
            ITimeLogsManagementService timeLogsManagementService,
            IProviderService providerService,
            IHistoryLogsService historyLogsService,
            ILogger<AdminController> logger)
        {
            _projectService = projectService;
            _contractorService = contractorService;
            _systemConfigService = systemConfigService;
            _timeLogsManagementService = timeLogsManagementService;
            _providerService = providerService;
            _historyLogsService = historyLogsService;
            _logger = logger;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            // Fetch all dashboard statistics
            var activeProjects = await _projectService.GetActiveCount();
            var activeProviders = await _providerService.GetActiveCount();
            var activeContractors = await _contractorService.GetActiveCount();
            var openSessions = await _historyLogsService.GetOpenSessionsCount();

            // Pass to view
            ViewBag.ActiveProjects = activeProjects.Success ? activeProjects.Data : 0;
            ViewBag.ActiveProviders = activeProviders.Success ? activeProviders.Data : 0;
            ViewBag.ActiveContractors = activeContractors.Success ? activeContractors.Data : 0;
            ViewBag.OpenSessions = openSessions.Success ? openSessions.Data : 0;
            ViewBag.AdminName = HttpContext.Session.GetString("DisplayName");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentTimeLogs()
        {
            var result = await _historyLogsService.GetRecentForDashboard();
            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message });
            }
            return Json(new { success = true, data = result.Data });
        }

        #region Project Management

        // GET: /Admin/Projects
        public IActionResult Projects()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("DisplayName");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProjects()
        {
            var result = await _projectService.GetAll();
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProviders(string search = null)
        {
            var result = await _providerService.GetAll();
            // Filter to only active providers
            var activeProviders = result.Data?.Where(p => p.active == 1);

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                activeProviders = activeProviders?.Where(p =>
                    p.provider_name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.provider_code.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return Json(new { success = result.Success, message = result.Message, data = activeProviders });
        }

        [HttpPost]
        public async Task<IActionResult> CreateProvider([FromBody] provider newProvider)
        {
            try
            {
                // Validate provider name
                if (string.IsNullOrWhiteSpace(newProvider.provider_name))
                {
                    return Json(new { success = false, message = "Provider name is required" });
                }

                // Validate provider code
                if (string.IsNullOrWhiteSpace(newProvider.provider_code))
                {
                    return Json(new { success = false, message = "Provider code is required" });
                }

                // Check if provider already exists by code
                var existingByCode = await _providerService.GetByProviderCode(newProvider.provider_code);
                if (existingByCode != null)
                {
                    return Json(new { success = false, message = "Provider with this code already exists" });
                }

                // Set default values
                newProvider.active = 1;
                newProvider.created_at = DateTime.Now;
                newProvider.updated_at = DateTime.Now;

                // Create provider
                var result = await _providerService.Create(newProvider);
                if (result.Success)
                {
                    return Json(new { success = true, data = result.Data, message = "Provider created successfully" });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating provider");
                return Json(new { success = false, message = "Error creating provider" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] project project)
        {
            try
            {
                // Check if provider exists
                var existingProvider = await _providerService.GetByProviderCode(project.provider_code);

                if (existingProvider == null || !existingProvider.Success || existingProvider.Data == null)
                {
                    // Create new provider first
                    var newProvider = new provider
                    {
                        provider_code = project.provider_code,
                        provider_name = project.provider_name,
                        provider_address = string.Empty,
                        active = 1,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };

                    var createProviderResult = await _providerService.Create(newProvider);
                    if (!createProviderResult.Success)
                    {
                        return Json(new { success = false, message = $"Failed to create new provider: {createProviderResult.Message}" });
                    }

                    _logger.LogInformation("New provider created during project creation: {ProviderCode}", project.provider_code);
                }

                // Now create the project
                var result = await _projectService.Create(project);
                return Json(new { success = result.Success, message = result.Message, data = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project with provider");
                return Json(new { success = false, message = "Error creating project. Please ensure all data is valid." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProject([FromBody] project project)
        {
            var result = await _projectService.Update(project);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> SetProjectInactive([FromBody] project project)
        {
            project.active = 0;
            var result = await _projectService.Update(project);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectContractors(string project_code)
        {
            var result = await _contractorService.GetByProjectCode(project_code);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        #endregion

        #region Contractor Management

        // GET: /Admin/Contractors
        public IActionResult Contractors()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("DisplayName");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllContractors()
        {
            var result = await _contractorService.GetAll();
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> CreateContractor([FromBody] contractor_employee contractor)
        {
            var result = await _contractorService.Create(contractor);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateContractor([FromBody] contractor_employee contractor)
        {
            var result = await _contractorService.Update(contractor);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> SetContractorInactive([FromBody] contractor_employee contractor)
        {
            contractor.active = 0;
            var result = await _contractorService.Update(contractor);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        #endregion

        #region System Configuration Management

        // GET: /Admin/SystemConfig
        public IActionResult SystemConfig()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("DisplayName");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSystemConfigs()
        {
            var result = await _systemConfigService.GetAll();
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSystemConfig([FromBody] system_config config)
        {
            var result = await _systemConfigService.Update(config);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        #endregion

        #region TimeLogs Management

        // GET: /Admin/TimeLogs
        public IActionResult TimeLogs()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("DisplayName");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTimeLogs(
            string? employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string? health_status = null)
        {
            // Handle null string by converting to empty string
            var employeeIdParam = employee_id ?? string.Empty;
            var healthStatusParam = health_status ?? string.Empty;

            var result = await _timeLogsManagementService.GetAllTimeLogs(employeeIdParam, from_date, to_date, healthStatusParam);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetTimeLogById(int attendance_id)
        {
            var result = await _timeLogsManagementService.GetTimeLogById(attendance_id);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> EditTimeLog([FromBody] time_log timelog)
        {
            var admin_employee_id = HttpContext.Session.GetString("EmployeeNumber") ?? "System";
            var result = await _timeLogsManagementService.UpdateTimeLog(timelog, admin_employee_id);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTimeLog(int attendance_id)
        {
            var result = await _timeLogsManagementService.DeleteTimeLog(attendance_id);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        #endregion
    }
}