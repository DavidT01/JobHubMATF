using ApplicationService.Application.DTOs;

namespace ApplicationService.Application.Commands;

public sealed record SubmitApplicationCommand(string? JobId, string? CoverLetter) : ICommand<ApplicationListItemDto>;
