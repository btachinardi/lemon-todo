using LemonDo.Api.Logging;
using Serilog;
using Serilog.Events;

namespace LemonDo.Api.Tests.Logging;

[TestClass]
public sealed class PiiMaskingEnricherTests
{
    [TestMethod]
    public void Should_MaskEmailProperty_When_LogEventContainsEmail()
    {
        LogEvent? capturedEvent = null;
        var logger = new LoggerConfiguration()
            .Enrich.With<PiiMaskingEnricher>()
            .WriteTo.Sink(new DelegateSink(e => capturedEvent = e))
            .CreateLogger();

        logger.Information("User logged in {Email}", "test@example.com");

        Assert.IsNotNull(capturedEvent);
        var emailProp = capturedEvent.Properties["Email"];
        Assert.AreEqual("\"t***@example.com\"", emailProp.ToString());
    }

    [TestMethod]
    public void Should_PreserveNonSensitiveProperties_When_LogEventHasMixed()
    {
        LogEvent? capturedEvent = null;
        var logger = new LoggerConfiguration()
            .Enrich.With<PiiMaskingEnricher>()
            .WriteTo.Sink(new DelegateSink(e => capturedEvent = e))
            .CreateLogger();

        logger.Information("Task {TaskId} created by {Email}", "abc-123", "admin@test.com");

        Assert.IsNotNull(capturedEvent);
        Assert.AreEqual("\"abc-123\"", capturedEvent.Properties["TaskId"].ToString());
        Assert.AreEqual("\"a***@test.com\"", capturedEvent.Properties["Email"].ToString());
    }

    [TestMethod]
    public void Should_MaskDisplayName_When_LogEventContainsDisplayName()
    {
        LogEvent? capturedEvent = null;
        var logger = new LoggerConfiguration()
            .Enrich.With<PiiMaskingEnricher>()
            .WriteTo.Sink(new DelegateSink(e => capturedEvent = e))
            .CreateLogger();

        logger.Information("Welcome {DisplayName}", "Alice Smith");

        Assert.IsNotNull(capturedEvent);
        Assert.AreEqual("\"A***h\"", capturedEvent.Properties["DisplayName"].ToString());
    }

    /// <summary>Simple Serilog sink that delegates to a callback for test capture.</summary>
    private sealed class DelegateSink(Action<LogEvent> write) : Serilog.Core.ILogEventSink
    {
        public void Emit(LogEvent logEvent) => write(logEvent);
    }
}
