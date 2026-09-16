using Microsoft.AspNetCore.Mvc.Filters;

namespace EmployeeAttendanceWithHealthDeclaration.Attributes
{
    public class NoCacheAttribute : Attribute, IResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            context.HttpContext.Response.Headers.Add("Pragma", "no-cache");
            context.HttpContext.Response.Headers.Add("Expires", "0");
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
            // Method required by interface but not needed
        }
    }
}
