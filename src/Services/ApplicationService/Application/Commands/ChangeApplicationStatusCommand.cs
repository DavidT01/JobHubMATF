using ApplicationService.Domain.Enums;
using MediatR;

namespace ApplicationService.Application.Commands;

public sealed record ChangeApplicationStatusCommand(Guid ApplicationId, ApplicationStatus? Status) : ICommand<Unit>;
