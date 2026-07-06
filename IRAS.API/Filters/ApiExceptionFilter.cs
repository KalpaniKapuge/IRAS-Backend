// IRAS.API/Filters/ApiExceptionFilter.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IRAS.API.Filters
{
    // Maps well-known domain exceptions to the right HTTP status code so individual
    // controller actions don't need to repeat the same try/catch boilerplate.
    // Anything not matched here falls through to the environment's exception handler (500).
    public class ApiExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var (statusCode, message) = context.Exception switch
            {
                KeyNotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
                UnauthorizedAccessException ex => (StatusCodes.Status401Unauthorized, ex.Message),
                ArgumentException ex => (StatusCodes.Status400BadRequest, ex.Message),
                InvalidOperationException ex => (StatusCodes.Status400BadRequest, ex.Message),
                _ => (0, string.Empty)
            };

            if (statusCode == 0)
                return;

            context.Result = new ObjectResult(new { message }) { StatusCode = statusCode };
            context.ExceptionHandled = true;
        }
    }
}
