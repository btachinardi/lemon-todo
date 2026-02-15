namespace LemonDo.Application.Common;

/// <summary>Utilities for safe log output that avoids PII exposure.</summary>
public static class LogHelpers
{
    /// <summary>
    /// Masks an email address for log output. Shows first character and domain.
    /// Example: "user@example.com" → "u***@example.com".
    /// </summary>
    public static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return "***@***";
        return $"{email[0]}***@{email[(atIndex + 1)..]}";
    }
}
