namespace Recruitment.API.Exceptions;

public sealed class RecruitmentForbiddenException()
    : Exception("You are not allowed to access this recruitment resource.");
