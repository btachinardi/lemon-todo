using LemonDo.Domain.Common;
using Serilog.Core;
using Serilog.Events;

namespace LemonDo.Api.Logging;

/// <summary>
/// Serilog destructuring policy that automatically masks protected data.
/// Uses two strategies:
/// <list type="number">
/// <item><description>Type-based: any object implementing <see cref="IProtectedData"/> is replaced
/// with its <see cref="IProtectedData.Redacted"/> value (covers all VOs regardless of property name).</description></item>
/// <item><description>Name-based: <see cref="MaskIfSensitive"/> masks scalar values whose property name
/// matches a known sensitive field (defense-in-depth for raw strings).</description></item>
/// </list>
/// </summary>
public sealed class ProtectedDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "email",
        "emailaddress",
        "password",
        "displayname",
        "display_name",
        "username",
        "user_name",
        "firstname",
        "first_name",
        "lastname",
        "last_name",
        "phonenumber",
        "phone_number",
    };

    /// <inheritdoc />
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        if (value is IProtectedData protectedData)
        {
            result = new ScalarValue(protectedData.Redacted);
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Masks a scalar value if the property name matches a known protected data field.
    /// Returns the original value if the property name is not sensitive.
    /// </summary>
    public static LogEventPropertyValue MaskIfSensitive(string propertyName, LogEventPropertyValue value)
    {
        if (!SensitivePropertyNames.Contains(propertyName))
            return value;

        if (value is ScalarValue scalar && scalar.Value is string stringValue)
        {
            var masked = propertyName.Contains("email", StringComparison.OrdinalIgnoreCase)
                ? MaskEmail(stringValue)
                : MaskGeneric(stringValue);

            return new ScalarValue(masked);
        }

        return new ScalarValue("***");
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return "***@***";
        return $"{email[0]}***@{email[(atIndex + 1)..]}";
    }

    private static string MaskGeneric(string value)
    {
        if (value.Length <= 2) return "***";
        return $"{value[0]}***{value[^1]}";
    }
}
