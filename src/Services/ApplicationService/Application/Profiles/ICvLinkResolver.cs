namespace ApplicationService.Application.Profiles;

public interface ICvLinkResolver
{
    string Resolve(string cvPath);
}
