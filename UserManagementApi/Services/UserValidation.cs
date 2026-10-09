using System.Net.Mail;

namespace UserManagementAPI.Services;

public static class UserValidation
{
    public static string? Validate(string? name, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return "Email is required.";
        }

        try
        {
            var parsedEmail = new MailAddress(email);

            if (!string.Equals(
                    parsedEmail.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Please provide a valid email address.";
            }
        }
        catch (FormatException)
        {
            return "Please provide a valid email address.";
        }

        return null;
    }
}