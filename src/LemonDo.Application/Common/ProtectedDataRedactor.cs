namespace LemonDo.Application.Common;

/// <summary>
/// Utility for redacting protected data fields in admin views.
/// Provides consistent masking for email addresses and names.
/// </summary>
public static class ProtectedDataRedactor
{
    /// <summary>Masks an email address, e.g. "john@example.com" → "j***@example.com".</summary>
    public static string RedactEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return "***@***";
        return $"{email[0]}***@{email[(atIndex + 1)..]}";
    }

    /// <summary>Masks a display name, e.g. "John Doe" → "J***e".</summary>
    public static string RedactName(string name)
    {
        if (name.Length <= 2) return "***";
        return $"{name[0]}***{name[^1]}";
    }
}
