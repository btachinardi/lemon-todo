using LemonDo.Api.Logging;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.ValueObjects;
using Serilog;
using Serilog.Events;

namespace LemonDo.Api.Tests.Logging;

[TestClass]
public sealed class ProtectedDataMaskingEnricherTests
{
    [TestMethod]
    public void Should_MaskEmailProperty_When_LogEventContainsEmail()
    {
        LogEvent? capturedEvent = null;
        var logger = new LoggerConfiguration()
            .Enrich.With<ProtectedDataMaskingEnricher>()
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
            .Enrich.With<ProtectedDataMaskingEnricher>()
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
            .Enrich.With<ProtectedDataMaskingEnricher>()
            .WriteTo.Sink(new DelegateSink(e => capturedEvent = e))
            .CreateLogger();

        logger.Information("Welcome {DisplayName}", "Alice Smith");

        Assert.IsNotNull(capturedEvent);
        Assert.AreEqual("\"A***h\"", capturedEvent.Properties["DisplayName"].ToString());
    }

    // --- End-to-end: IProtectedData value objects through full pipeline ---

    [TestMethod]
    public void Should_MaskSensitiveNote_When_DestructuredWithAnyPropertyName()
    {
        LogEvent? capturedEvent = null;
        var logger = new LoggerConfiguration()
            .Destructure.With<ProtectedDataDestructuringPolicy>()
            .Enrich.With<ProtectedDataMaskingEnricher>()
            .WriteTo.Sink(new DelegateSink(e => capturedEvent = e))
            .CreateLogger();

        var note = SensitiveNote.Reconstruct("my secret medical info");

        // Using {@Note} — destructuring operator triggers TryDestructure
        logger.Information("Task note: {@Note}", note);

        Assert.IsNotNull(capturedEvent);
        var noteProp = capturedEvent.Properties["Note"];
        Assert.AreEqual("\"[PROTECTED]\"", noteProp.ToString(),
            "SensitiveNote should be redacted via IProtectedData.Redacted, not logged raw");
    }

    [TestMethod]
    public void Should_MaskEmailVO_When_DestructuredWithNonStandardPropertyName()
    {
        LogEvent? capturedEvent = null;
        var logger = new LoggerConfiguration()
            .Destructure.With<ProtectedDataDestructuringPolicy>()
            .Enrich.With<ProtectedDataMaskingEnricher>()
            .WriteTo.Sink(new DelegateSink(e => capturedEvent = e))
            .CreateLogger();

        var email = Email.Reconstruct("user@example.com");

        // Property name "RecipientEmail" is NOT in the hardcoded name list
        logger.Information("Sent to {@RecipientEmail}", email);

        Assert.IsNotNull(capturedEvent);
        var prop = capturedEvent.Properties["RecipientEmail"];
        Assert.AreEqual("\"u***@example.com\"", prop.ToString(),
            "Email VO should be redacted via IProtectedData.Redacted regardless of property name");
    }

    [TestMethod]
    public void Should_MaskDisplayNameVO_When_DestructuredWithNonStandardPropertyName()
    {
        LogEvent? capturedEvent = null;
        var logger = new LoggerConfiguration()
            .Destructure.With<ProtectedDataDestructuringPolicy>()
            .Enrich.With<ProtectedDataMaskingEnricher>()
            .WriteTo.Sink(new DelegateSink(e => capturedEvent = e))
            .CreateLogger();

        var name = DisplayName.Reconstruct("Alice Smith");

        // Property name "AuthorName" is NOT in the hardcoded name list
        logger.Information("Written by {@AuthorName}", name);

        Assert.IsNotNull(capturedEvent);
        var prop = capturedEvent.Properties["AuthorName"];
        Assert.AreEqual("\"A***h\"", prop.ToString(),
            "DisplayName VO should be redacted via IProtectedData.Redacted regardless of property name");
    }

    /// <summary>Simple Serilog sink that delegates to a callback for test capture.</summary>
    private sealed class DelegateSink(Action<LogEvent> write) : Serilog.Core.ILogEventSink
    {
        public void Emit(LogEvent logEvent) => write(logEvent);
    }
}
