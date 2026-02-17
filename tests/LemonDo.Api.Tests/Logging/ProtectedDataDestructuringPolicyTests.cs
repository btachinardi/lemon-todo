using LemonDo.Api.Logging;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.ValueObjects;
using Serilog.Core;
using Serilog.Events;

namespace LemonDo.Api.Tests.Logging;

[TestClass]
public sealed class ProtectedDataDestructuringPolicyTests
{
    private readonly ProtectedDataDestructuringPolicy _policy = new();
    private readonly ILogEventPropertyValueFactory _factory = new StubPropertyValueFactory();

    private sealed class StubPropertyValueFactory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false)
            => new ScalarValue(value);
    }

    // --- MaskIfSensitive (existing name-based tests) ---

    [TestMethod]
    public void Should_MaskEmail_When_PropertyNameIsEmail()
    {
        var value = new ScalarValue("user@example.com");
        var result = ProtectedDataDestructuringPolicy.MaskIfSensitive("Email", value);
        Assert.AreEqual("u***@example.com", ((ScalarValue)result).Value);
    }

    [TestMethod]
    public void Should_MaskEmail_When_PropertyNameIsEmailAddress()
    {
        var value = new ScalarValue("john.doe@company.org");
        var result = ProtectedDataDestructuringPolicy.MaskIfSensitive("emailAddress", value);
        Assert.AreEqual("j***@company.org", ((ScalarValue)result).Value);
    }

    [TestMethod]
    public void Should_MaskGenericValue_When_PropertyNameIsDisplayName()
    {
        var value = new ScalarValue("John Doe");
        var result = ProtectedDataDestructuringPolicy.MaskIfSensitive("DisplayName", value);
        Assert.AreEqual("J***e", ((ScalarValue)result).Value);
    }

    [TestMethod]
    public void Should_MaskGenericValue_When_PropertyNameIsPassword()
    {
        var value = new ScalarValue("secretpass123");
        var result = ProtectedDataDestructuringPolicy.MaskIfSensitive("password", value);
        Assert.AreEqual("s***3", ((ScalarValue)result).Value);
    }

    [TestMethod]
    public void Should_ReturnOriginalValue_When_PropertyNameIsNotSensitive()
    {
        var value = new ScalarValue("some-task-title");
        var result = ProtectedDataDestructuringPolicy.MaskIfSensitive("TaskTitle", value);
        Assert.AreSame(value, result);
    }

    [TestMethod]
    public void Should_MaskShortEmail_When_LocalPartIsSingleChar()
    {
        var value = new ScalarValue("a@b.com");
        var result = ProtectedDataDestructuringPolicy.MaskIfSensitive("email", value);
        Assert.AreEqual("***@***", ((ScalarValue)result).Value);
    }

    [TestMethod]
    public void Should_ReturnTripleStars_When_NonStringScalar()
    {
        var value = new ScalarValue(12345);
        var result = ProtectedDataDestructuringPolicy.MaskIfSensitive("email", value);
        Assert.AreEqual("***", ((ScalarValue)result).Value);
    }

    // --- TryDestructure: IProtectedData value objects ---

    [TestMethod]
    public void Should_ReturnRedacted_When_DestructuringEmailValueObject()
    {
        var email = Email.Reconstruct("user@example.com");

        var handled = _policy.TryDestructure(email, _factory, out var result);

        Assert.IsTrue(handled, "Policy should handle IProtectedData objects");
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<ScalarValue>(result);
        Assert.AreEqual("u***@example.com", ((ScalarValue)result).Value);
    }

    [TestMethod]
    public void Should_ReturnRedacted_When_DestructuringDisplayNameValueObject()
    {
        var displayName = DisplayName.Reconstruct("Alice Smith");

        var handled = _policy.TryDestructure(displayName, _factory, out var result);

        Assert.IsTrue(handled, "Policy should handle IProtectedData objects");
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<ScalarValue>(result);
        Assert.AreEqual("A***h", ((ScalarValue)result).Value);
    }

    [TestMethod]
    public void Should_ReturnRedacted_When_DestructuringSensitiveNoteValueObject()
    {
        var note = SensitiveNote.Reconstruct("my secret medical info");

        var handled = _policy.TryDestructure(note, _factory, out var result);

        Assert.IsTrue(handled, "Policy should handle IProtectedData objects");
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<ScalarValue>(result);
        Assert.AreEqual("[PROTECTED]", ((ScalarValue)result).Value);
    }

    [TestMethod]
    public void Should_NotHandle_When_DestructuringNonProtectedObject()
    {
        var plainObject = new { Name = "test", Count = 42 };

        var handled = _policy.TryDestructure(plainObject, _factory, out var result);

        Assert.IsFalse(handled, "Policy should not handle non-IProtectedData objects");
        Assert.IsNull(result);
    }
}
