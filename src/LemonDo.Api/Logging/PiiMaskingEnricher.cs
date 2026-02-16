using Serilog.Core;
using Serilog.Events;

namespace LemonDo.Api.Logging;

/// <summary>
/// Serilog enricher that scans all log event properties and masks those
/// matching known PII field names (email, password, displayName, etc.).
/// </summary>
public sealed class PiiMaskingEnricher : ILogEventEnricher
{
    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var propertiesToMask = new List<LogEventProperty>();

        foreach (var property in logEvent.Properties)
        {
            var masked = PiiDestructuringPolicy.MaskIfSensitive(property.Key, property.Value);
            if (!ReferenceEquals(masked, property.Value))
            {
                propertiesToMask.Add(new LogEventProperty(property.Key, masked));
            }
        }

        foreach (var property in propertiesToMask)
        {
            logEvent.AddOrUpdateProperty(property);
        }
    }
}
