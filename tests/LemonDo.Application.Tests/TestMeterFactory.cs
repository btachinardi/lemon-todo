namespace LemonDo.Application.Tests;

using System.Diagnostics.Metrics;

/// <summary>Simple <see cref="IMeterFactory"/> for unit tests that creates real meters.</summary>
internal sealed class TestMeterFactory : IMeterFactory
{
    public Meter Create(MeterOptions options) => new(options);
    public void Dispose() { }
}
