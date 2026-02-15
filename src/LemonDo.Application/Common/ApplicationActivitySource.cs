namespace LemonDo.Application.Common;

using System.Diagnostics;

/// <summary>
/// Shared <see cref="ActivitySource"/> for custom tracing spans in the application layer.
/// Register this source name in the OpenTelemetry pipeline so spans are exported.
/// </summary>
public static class ApplicationActivitySource
{
    /// <summary>The source name used for OpenTelemetry registration.</summary>
    public const string SourceName = "LemonDo.Application";

    /// <summary>The shared activity source instance for creating custom spans.</summary>
    public static readonly ActivitySource Source = new(SourceName);
}
