namespace ApplicationService.Application.Exceptions;

public sealed class DependencyUnavailableException(string service, Exception? innerException = null)
    : Exception($"{service} service is unavailable. Please try again later.", innerException);
