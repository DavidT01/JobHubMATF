using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Recruitment.API.Exceptions
{
    public class RecruitmentExceptionHandler(ILogger<RecruitmentExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

            var details = new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "Server Error", Detail = exception.Message };

            if (exception is RecruitmentValidationException validationException)
            {
                details.Status = StatusCodes.Status400BadRequest;
                details.Title = "Recruitment Validation Error";
                details.Detail = "One or more recruitment validation errors occurred.";
                details.Extensions.Add("errors", validationException.Errors);
            }

            httpContext.Response.StatusCode = details.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(details, cancellationToken);

            return true;
        }
    }
}
