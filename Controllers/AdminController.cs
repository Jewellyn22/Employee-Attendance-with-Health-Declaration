using ContractorAttendanceWithHealthDeclaration.Attributes;
using ContractorAttendanceWithHealthDeclaration.Models;
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
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IProjectService projectService,
            IContractorService contractorService,
            ISystemConfigService systemConfigService,
            ITimeLogsManagementService timeLogsManagementService,
            IProviderService providerService,
            IHistoryLogsService historyLogsService,
            IAuditLogService auditLogService,
            ILogger<AdminController> logger)
        {
            _projectService = projectService;
            _contractorService = contractorService;
            _systemConfigService = systemConfigService;
            _timeLogsManagementService = timeLogsManagementService;
            _providerService = providerService;
            _historyLogsService = historyLogsService;
            _auditLogService = auditLogService;
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
        public async Task<IActionResult> GetAllProviders(string? search = null)
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

        // GET: /Admin/GetProjectsByProvider?provider_code=...
        // Returns only projects belonging to the selected provider (for the cascade dropdown)
        [HttpGet]
        public async Task<IActionResult> GetProjectsByProvider(string provider_code)
        {
            var result = await _projectService.GetByProviderCode(provider_code);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> CreateProvider([FromBody] provider newProvider)
        {
            try
            {
                var admin = HttpContext.Session.GetString("EmployeeNumber") ?? "System";

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
                var result = await _providerService.Create(newProvider, admin);
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
                var admin = HttpContext.Session.GetString("EmployeeNumber") ?? "System";

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

                    var createProviderResult = await _providerService.Create(newProvider, admin);
                    if (!createProviderResult.Success)
                    {
                        return Json(new { success = false, message = $"Failed to create new provider: {createProviderResult.Message}" });
                    }

                    _logger.LogInformation("New provider created during project creation: {ProviderCode}", project.provider_code);
                }

                // Now create the project
                var result = await _projectService.Create(project, admin);
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
            var admin = HttpContext.Session.GetString("EmployeeNumber") ?? "System";
            var result = await _projectService.Update(project, admin);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> SetProjectInactive([FromBody] project project)
        {
            project.active = 0;
            var admin = HttpContext.Session.GetString("EmployeeNumber") ?? "System";
            var result = await _projectService.Update(project, admin);
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
            try
            {
                // Check if contractor object is null
                if (contractor == null)
                {
                    return Json(new { success = false, message = "Contractor data is required" });
                }

                // employee_id is auto-generated by the service/stored procedure on create,
                // so exclude it from ModelState validation (non-nullable string is otherwise required)
                ModelState.Remove("employee_id");

                // contact_number is optional: skip [Phone] format validation when blank
                if (string.IsNullOrWhiteSpace(contractor.contact_number))
                {
                    contractor.contact_number = string.Empty;
                    ModelState.Remove("contact_number");
                }

                // Validate ModelState
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new
                    {
                        success = false,
                        message = "Validation failed: " + string.Join(", ", errors)
                    });
                }

                var admin = HttpContext.Session.GetString("EmployeeNumber") ?? "System";
                var result = await _contractorService.Create(contractor, admin);
                return Json(new { success = result.Success, message = result.Message, data = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contractor");
                return Json(new { success = false, message = "An error occurred while creating the contractor" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateContractor([FromBody] contractor_employee contractor)
        {
            try
            {
                // Check if contractor object is null
                if (contractor == null)
                {
                    return Json(new { success = false, message = "Contractor data is required" });
                }

                // contact_number is optional: skip [Phone] format validation when blank
                if (string.IsNullOrWhiteSpace(contractor.contact_number))
                {
                    contractor.contact_number = string.Empty;
                    ModelState.Remove("contact_number");
                }

                // Validate ModelState
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new
                    {
                        success = false,
                        message = "Validation failed: " + string.Join(", ", errors)
                    });
                }

                var admin = HttpContext.Session.GetString("EmployeeNumber") ?? "System";
                var result = await _contractorService.Update(contractor, admin);
                return Json(new { success = result.Success, message = result.Message, data = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contractor");
                return Json(new { success = false, message = "An error occurred while updating the contractor" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetContractorInactive([FromBody] contractor_employee contractor)
        {
            contractor.active = 0;
            var admin = HttpContext.Session.GetString("EmployeeNumber") ?? "System";
            var result = await _contractorService.Update(contractor, admin);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        // POST: /Admin/BulkCreateContractors
        // Accepts provider_code + project_code (from the modal selects) and a list of
        // contractor rows parsed client-side from the uploaded CSV/Excel file.
        [HttpPost]
        public async Task<IActionResult> BulkCreateContractors([FromBody] bulk_enrollment_request request)
        {
            try
            {
                if (request == null)
                {
                    return Json(new { success = false, message = "No import data was provided." });
                }

                // Admin identity for the audit trail (matches TimeLogsManagement pattern).
                var adminEmployeeId = HttpContext.Session.GetString("EmployeeNumber") ?? "System";

                var result = await _contractorService.BulkCreate(request, adminEmployeeId);
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk contractor enrollment");
                return Json(new { success = false, message = "An error occurred during bulk enrollment." });
            }
        }

        #endregion

        #region Audit Logs

        // GET: /Admin/AuditLogs
        public IActionResult AuditLogs()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("DisplayName");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAuditLogs()
        {
            var result = await _auditLogService.GetAll();
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
            var admin = HttpContext.Session.GetString("EmployeeNumber") ?? "System";
            var result = await _systemConfigService.Update(config, admin);
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
            var admin_employee_id = HttpContext.Session.GetString("EmployeeNumber") ?? "System";
            var result = await _timeLogsManagementService.DeleteTimeLog(attendance_id, admin_employee_id);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        #endregion
    }
}