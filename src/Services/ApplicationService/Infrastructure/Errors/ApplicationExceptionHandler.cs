using ApplicationService.Application.Exceptions;
using ApplicationService.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ApplicationService.Infrastructure.Errors;

public sealed class ApplicationExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApplicationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var problem = exception switch
        {
            RequestValidationException validation => new HttpValidationProblemDetails(validation.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Request validation failed."
            },
            ApplicationDomainException => CreateProblem(400, "Business rule violation.", exception.Message),
            ResourceNotFoundException => CreateProblem(404, "Resource not found.", exception.Message),
            ConflictException => CreateProblem(409, "Request conflicts with the current state.", exception.Message),
            ForbiddenException => CreateProblem(403, "Access denied.", exception.Message),
            BadHttpRequestException badRequest => CreateProblem(
                badRequest.StatusCode, "Invalid HTTP request.", "The request could not be processed."),
            _ => CreateProblem(500, "An unexpected error occurred.", "Please try again later.")
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Application request failed. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
        }

        problem.Instance = httpContext.Request.Path;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = problem.Status!.Value;

        var written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });

        if (!written)
        {
            await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null,
                contentType: "application/problem+json", cancellationToken: cancellationToken);
        }

        return true;
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };
}
