using ApplicationService.Domain.Enums;

namespace ApplicationService.Application.DTOs;

public sealed record ChangeApplicationStatusRequest(ApplicationStatus? Status);
