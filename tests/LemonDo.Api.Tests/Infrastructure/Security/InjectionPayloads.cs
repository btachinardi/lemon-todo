namespace LemonDo.Api.Tests.Infrastructure.Security;

/// <summary>
/// Shared attack strings for input validation / injection tests.
/// </summary>
public static class InjectionPayloads
{
    public const string SqlInjection = "'; DROP TABLE Tasks; --";
    public const string XssScript = "<script>alert('xss')</script>";
    public const string TemplateInjection = "{{7*7}} ${7*7} #{7*7}";
    public const string NullByte = "test\0injected";
    public const string PathTraversal = "../../etc/passwd";

    /// <summary>All payloads as DynamicData rows.</summary>
    public static IEnumerable<object[]> AllInjectionPayloads =>
    [
        [SqlInjection, "SqlInjection"],
        [XssScript, "XssScript"],
        [TemplateInjection, "TemplateInjection"],
        [NullByte, "NullByte"],
        [PathTraversal, "PathTraversal"],
    ];
}
