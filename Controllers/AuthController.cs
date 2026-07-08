using ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Models;
using ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Services;
using ContractorAttendanceWithHealthDeclaration.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContractorAttendanceWithHealthDeclaration.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILdapService _ldapService;
        private readonly ISystemConfigService _systemConfigService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ILdapService ldapService,
            ISystemConfigService systemConfigService,
            ILogger<AuthController> logger)
        {
            _ldapService = ldapService;
            _systemConfigService = systemConfigService;
            _logger = logger;
        }

        // GET: Auth/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            _logger.LogInformation("Login attempt for user: {Username}", username);

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return Json(new { success = false, message = "Username and password are required" });
            }

            // Step 1: LDAP Authentication
            var ldapResult = await _ldapService.Login(username, password);
            if (!ldapResult.Success || ldapResult.Data == null)
            {
                return Json(new { success = false, message = "Invalid credentials" });
            }

            // Step 2: Check Admin Group Membership
            var adminGroupResult = await _systemConfigService.GetAdminADGroup();
            string adminGroup = adminGroupResult.Data;

            if (ldapResult.Data.member_of == null || !ldapResult.Data.member_of.Contains(adminGroup))
            {
                _logger.LogWarning("User {Username} is not a member of admin group: {AdminGroup}", username, adminGroup);
                return Json(new { success = false, message = "Not authorized as admin" });
            }

            // Step 3: Set Session
            HttpContext.Session.SetString("EmployeeNumber", ldapResult.Data.office);
            HttpContext.Session.SetString("DisplayName", ldapResult.Data.displayName);
            HttpContext.Session.SetString("Role", "Admin");
            HttpContext.Session.SetString("Email", ldapResult.Data.mail ?? "");

            _logger.LogInformation("User {Username} logged in successfully as admin", username);

            return Json(new
            {
                success = true,
                message = "Login successful",
                redirect = returnUrl ?? "/Admin/Providers/Index"
            });
        }

        // POST: Auth/Logout
        [HttpPost]
        public IActionResult Logout()
        {
            _logger.LogInformation("User logged out: {DisplayName}", HttpContext.Session.GetString("DisplayName"));

            HttpContext.Session.Clear();

            return Json(new { success = true, message = "Logged out", redirect = "/" });
        }

        // GET: Auth/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}