using EmployeeAttendanceWithHealthDeclaration.Models;
using EmployeeAttendanceWithHealthDeclaration.Models.Domain;
using EmployeeAttendanceWithHealthDeclaration.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeAttendanceWithHealthDeclaration.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IHistoryLogsService _historyLogsService;
        private readonly ISystemConfigService _systemConfigService;
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IAttendanceService attendanceService,
            IHistoryLogsService historyLogsService,
            ISystemConfigService systemConfigService,
            IEmployeeService employeeService,
            ILogger<HomeController> logger)
        {
            _attendanceService = attendanceService;
            _historyLogsService = historyLogsService;
            _systemConfigService = systemConfigService;
            _employeeService = employeeService;
            _logger = logger;
        }

        // GET: /
        public async Task<IActionResult> Index()
        {
            // Main kiosk interface (ID scanning + health declaration)
            var healthWindowConfig = await _systemConfigService.GetHealthDeclarationWindowSeconds();
            ViewBag.HealthDeclarationWindowSeconds = healthWindowConfig.Data;  // Value is already in seconds

            // Get sickness items from database configuration
            var sicknessItemsConfig = await _systemConfigService.GetHealthDeclarationSicknessItems();
            ViewBag.SicknessItems = sicknessItemsConfig.Data;

            // Get scan input readonly configuration
            var scanInputConfig = await _systemConfigService.GetScanInputReadOnly();
            ViewBag.ScanInputReadOnly = scanInputConfig.Data;

            // Get waiver and declaration texts (falls back to default wording if not configured)
            var waiverCertificationConfig = await _systemConfigService.GetWaiverCertificationText();
            ViewBag.WaiverCertificationText = waiverCertificationConfig.Data;

            var waiverAcknowledgmentConfig = await _systemConfigService.GetWaiverAcknowledgmentText();
            ViewBag.WaiverAcknowledgmentText = waiverAcknowledgmentConfig.Data;

            var waiverLiabilityReleaseConfig = await _systemConfigService.GetWaiverLiabilityReleaseText();
            ViewBag.WaiverLiabilityReleaseText = waiverLiabilityReleaseConfig.Data;

            return View();
        }

        // GET: /Attendance/Index
        public async Task<IActionResult> Attendance()
        {
            // Real-time attendance monitoring dashboard
            var healthWindowConfig = await _systemConfigService.GetHealthDeclarationWindowSeconds();
            ViewBag.HealthDeclarationWindowSeconds = healthWindowConfig.Data;  // Value is already in seconds

            // Get scan input readonly configuration
            var scanInputConfig = await _systemConfigService.GetScanInputReadOnly();
            ViewBag.ScanInputReadOnly = scanInputConfig.Data;

            // Get waiver and declaration texts (falls back to default wording if not configured)
            var waiverCertificationConfig = await _systemConfigService.GetWaiverCertificationText();
            ViewBag.WaiverCertificationText = waiverCertificationConfig.Data;

            var waiverAcknowledgmentConfig = await _systemConfigService.GetWaiverAcknowledgmentText();
            ViewBag.WaiverAcknowledgmentText = waiverAcknowledgmentConfig.Data;

            var waiverLiabilityReleaseConfig = await _systemConfigService.GetWaiverLiabilityReleaseText();
            ViewBag.WaiverLiabilityReleaseText = waiverLiabilityReleaseConfig.Data;

            return View("Index", "Attendance");
        }

        // POST: /Home/Scan
        [HttpPost]
        public async Task<IActionResult> Scan(string employee_id)
        {
            _logger.LogInformation("Scan request received for: {EmployeeId}", employee_id);

            var result = await _attendanceService.ProcessScan(employee_id);

            if (result.Success)
            {
                // Retrieve actual employee details from database
                var employeeResult = await _employeeService.GetByEmployeeId(employee_id);
                var employee = employeeResult.Data;

                // Null check for result.Data
                if (result?.Data == null)
                {
                    return Json(new { success = false, message = "Failed to process attendance" });
                }

                // Null check for employee
                if (employee == null)
                {
                    return Json(new { success = false, message = "Employee not found" });
                }

                var response_data = new
                {
                    attendance_id = result.Data.attendance_id,
                    employee_id = result.Data.employee_id,
                    time_in = result.Data.time_in,
                    health_status = result.Data.health_status,
                    waiver_consent = result.Data.waiver_consent,
                    employee_info = new
                    {
                        employee_id = employee.employee_id,
                        name = employee.name,
                        provider_code = employee.provider_code,
                        provider_name = employee.provider_name,
                        // Multi-project CSVs: comma-joined codes/names, DISTINCT areas, and
                        // per-project positions of the employee's assigned projects (kiosk
                        // renders them joined).
                        project_codes = employee.project_codes,
                        project_names = employee.project_names,
                        areas = employee.areas,
                        positions = employee.positions,
                        // JSON array string [{project_name, area, position}] per assigned project --
                        // index-aligned (unlike the DISTINCT areas CSV); kiosk Personal Detail rows.
                        project_rows = employee.project_rows
                    }
                };

                return Json(new { success = true, message = result.Message, data = response_data });
            }

            return Json(new { success = false, message = result.Message, data = (object?)null });
        }

        // POST: /Home/UpdateHealthStatus
        [HttpPost]
        public async Task<IActionResult> UpdateHealthStatus(int attendance_id, string health_status, string waiver_consent)
        {
            _logger.LogInformation("Health status and waiver consent update request for attendance: {AttendanceId} to health={HealthStatus}, waiver={WaiverConsent}",
                attendance_id, health_status, waiver_consent);

            var result = await _attendanceService.UpdateHealthStatus(attendance_id, health_status, waiver_consent);

            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        // GET: /Home/HistoryLogs
        public IActionResult HistoryLogs()
        {
            // Public history logs page (no authentication required)
            return View();
        }

        // GET: /Home/GetHistoryLogs
        [HttpGet]
        public async Task<IActionResult> GetHistoryLogs(
            string? employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string? health_status = null)
        {
            _logger.LogInformation("History logs request - EmployeeId: {EmployeeId}, FromDate: {FromDate}, ToDate: {ToDate}, HealthStatus: {HealthStatus}",
                employee_id, from_date, to_date, health_status);

            var result = await _historyLogsService.GetFilteredAttendance(employee_id, from_date, to_date, health_status);

            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        // GET: /Home/GetHistoryLogsForExport
        [HttpGet]
        public async Task<IActionResult> GetHistoryLogsForExport(
            string? employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string? health_status = null)
        {
            _logger.LogInformation("History logs export request - EmployeeId: {EmployeeId}, FromDate: {FromDate}, ToDate: {ToDate}, HealthStatus: {HealthStatus}",
                employee_id, from_date, to_date, health_status);

            var result = await _historyLogsService.GetAttendanceForExport(employee_id, from_date, to_date, health_status);

            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }
}