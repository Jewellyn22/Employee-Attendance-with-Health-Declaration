using ContractorAttendanceWithHealthDeclaration.Attributes;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContractorAttendanceWithHealthDeclaration.Controllers
{
    [AuthorizeAdmin]
    public class AdminController : Controller
    {
        private readonly IProviderService _providerService;
        private readonly IProjectService _projectService;
        private readonly IContractorService _contractorService;
        private readonly ISystemConfigService _systemConfigService;
        private readonly ITimeLogsManagementService _timeLogsManagementService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IProviderService providerService,
            IProjectService projectService,
            IContractorService contractorService,
            ISystemConfigService systemConfigService,
            ITimeLogsManagementService timeLogsManagementService,
            ILogger<AdminController> logger)
        {
            _providerService = providerService;
            _projectService = projectService;
            _contractorService = contractorService;
            _systemConfigService = systemConfigService;
            _timeLogsManagementService = timeLogsManagementService;
            _logger = logger;
        }

        // GET: /Admin
        public IActionResult Index()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("DisplayName");
            return View();
        }

        #region Provider Management

        // GET: /Admin/Providers
        public IActionResult Providers()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("DisplayName");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProviders()
        {
            var result = await _providerService.GetAll();
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> CreateProvider([FromBody] provider provider)
        {
            var result = await _providerService.Create(provider);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProvider([FromBody] provider provider)
        {
            var result = await _providerService.Update(provider);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> SetProviderInactive([FromBody] provider provider)
        {
            provider.active = 0;
            var result = await _providerService.Update(provider);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        #endregion

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

        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] project project)
        {
            var result = await _projectService.Create(project);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
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
            string employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string health_status = null)
        {
            var result = await _timeLogsManagementService.GetAllTimeLogs(employee_id, from_date, to_date, health_status);
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
            var admin_employee_id = HttpContext.Session.GetString("EmployeeNumber");
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