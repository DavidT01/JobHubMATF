namespace Profile.API.Exceptions;

public sealed class ProfileForbiddenException()
    : Exception("You are not allowed to access this profile resource.");
