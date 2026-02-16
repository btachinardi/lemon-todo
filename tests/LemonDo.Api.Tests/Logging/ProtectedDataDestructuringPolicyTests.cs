using LemonDo.Api.Logging;
using Serilog.Events;

namespace LemonDo.Api.Tests.Logging;

[TestClass]
public sealed class ProtectedDataDestructuringPolicyTests
{
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
}
