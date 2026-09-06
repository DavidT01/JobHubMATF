using System.Net.Mail;

namespace Recruitment.API.Features.Commands.Validators;

public static class EmailValidation
{
    public static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
