namespace LemonDo.Api.Tests.Infrastructure.Security;

using System.Text;

/// <summary>
/// Shared constants for invalid JWT tokens used across security tests.
/// </summary>
public static class MalformedTokens
{
    /// <summary>A random string that is not a JWT.</summary>
    public const string RandomString = "randomstringnotajwt";

    /// <summary>A malformed JWT with invalid segments.</summary>
    public const string MalformedJwt = "this.is.not.a.valid.jwt";

    /// <summary>A JWT with valid structure but signed by a different key.</summary>
    public const string WrongKeyJwt =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" +
        ".eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkF0dGFja2VyIn0" +
        ".SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

    /// <summary>An empty string bearer token.</summary>
    public const string EmptyToken = "";

    /// <summary>All invalid tokens as DynamicData rows for parameterized tests.</summary>
    public static IEnumerable<object[]> AllInvalidTokens =>
    [
        [RandomString, "RandomString"],
        [MalformedJwt, "MalformedJwt"],
        [WrongKeyJwt, "WrongKeyJwt"],
        [EmptyToken, "EmptyToken"],
    ];

    /// <summary>Returns a human-readable name for test display.</summary>
    public static string GetTokenDisplayName(string token) => token switch
    {
        RandomString => "RandomString",
        MalformedJwt => "MalformedJwt",
        WrongKeyJwt => "WrongKeyJwt",
        EmptyToken => "EmptyToken",
        _ => token.Length > 20 ? token[..20] + "..." : token,
    };
}
