using ContractorAttendanceWithHealthDeclaration.Models;
using ContractorAttendanceWithHealthDeclaration.Models.Domain;
using ContractorAttendanceWithHealthDeclaration.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContractorAttendanceWithHealthDeclaration.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IHistoryLogsService _historyLogsService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IAttendanceService attendanceService,
            IHistoryLogsService historyLogsService,
            ILogger<HomeController> logger)
        {
            _attendanceService = attendanceService;
            _historyLogsService = historyLogsService;
            _logger = logger;
        }

        // GET: /
        public IActionResult Index()
        {
            // Main kiosk interface (ID scanning + health declaration)
            return View();
        }

        // POST: /Home/Scan
        [HttpPost]
        public async Task<IActionResult> Scan(string employee_id)
        {
            _logger.LogInformation("Scan request received for: {EmployeeId}", employee_id);

            var result = await _attendanceService.ProcessScan(employee_id);

            if (result.Success)
            {
                // Include contractor info in response
                var response_data = new
                {
                    attendance_id = result.Data.attendance_id,
                    employee_id = result.Data.employee_id,
                    time_in = result.Data.time_in,
                    health_status = result.Data.health_status,
                    contractor_info = new
                    {
                        employee_id = employee_id,
                        name = "Contractor Name", // Will be populated from repository
                        provider_code = "PROV-001",
                        position = "Worker",
                        area_of_destination = "Area A"
                    }
                };

                return Json(new { success = true, message = result.Message, data = response_data });
            }

            return Json(new { success = false, message = result.Message, data = (object?)null });
        }

        // POST: /Home/UpdateHealthStatus
        [HttpPost]
        public async Task<IActionResult> UpdateHealthStatus(int attendance_id, string health_status)
        {
            _logger.LogInformation("Health status update request for attendance: {AttendanceId} to {HealthStatus}",
                attendance_id, health_status);

            var result = await _attendanceService.UpdateHealthStatus(attendance_id, health_status);

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
            string employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string health_status = null)
        {
            _logger.LogInformation("History logs request - EmployeeId: {EmployeeId}, FromDate: {FromDate}, ToDate: {ToDate}, HealthStatus: {HealthStatus}",
                employee_id, from_date, to_date, health_status);

            var result = await _historyLogsService.GetFilteredAttendance(employee_id, from_date, to_date, health_status);

            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        // GET: /Home/GetHistoryLogsForExport
        [HttpGet]
        public async Task<IActionResult> GetHistoryLogsForExport(
            string employee_id = null,
            DateTime? from_date = null,
            DateTime? to_date = null,
            string health_status = null)
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