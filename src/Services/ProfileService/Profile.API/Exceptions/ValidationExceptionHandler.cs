using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Profile.API.Exceptions
{
    public class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

            var details = new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "Server Error", Detail = exception.Message };

            if(exception is ProfileValidationException validationException)
            {
                details.Status = StatusCodes.Status400BadRequest;
                details.Title = "Profile Validation Error";
                details.Detail = "One or more profile validation errors occurred.";
                details.Extensions.Add("errors", validationException.Errors);
            }
            else if (exception is ProfileForbiddenException)
            {
                details.Status = StatusCodes.Status403Forbidden;
                details.Title = "Forbidden";
                details.Detail = exception.Message;
            }

            httpContext.Response.StatusCode = details.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(details, cancellationToken);

            return true;
        }
    }
}
