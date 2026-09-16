using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace EmployeeAttendanceWithHealthDeclaration.Attributes
{
    public class AuthorizeAdminAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            var role = session.GetString("Role");

            if (role != "Admin")
            {
                context.Result = new RedirectToActionResult("Login", "Auth", new
                {
                    returnUrl = context.HttpContext.Request.Path
                });
            }
        }
    }
}