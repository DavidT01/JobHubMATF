namespace ApplicationService.Domain.Exceptions;

public sealed class ApplicationDomainException : Exception
{
    public ApplicationDomainException(string message)
        : base(message)
    {
    }
}
