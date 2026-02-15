namespace LemonDo.Domain.Tests.Identity.ValueObjects;

using LemonDo.Domain.Identity.ValueObjects;

[TestClass]
public sealed class EmailTests
{
    [TestMethod]
    public void Should_CreateEmail_When_ValidFormat()
    {
        var result = Email.Create("user@example.com");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("user@example.com", result.Value.Value);
    }

    [TestMethod]
    public void Should_NormalizeToLowercase_When_Created()
    {
        var result = Email.Create("User@EXAMPLE.COM");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("user@example.com", result.Value.Value);
    }

    [TestMethod]
    public void Should_TrimWhitespace_When_Created()
    {
        var result = Email.Create("  user@example.com  ");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("user@example.com", result.Value.Value);
    }

    [TestMethod]
    public void Should_Fail_When_Null()
    {
        var result = Email.Create(null);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("email.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Fail_When_Empty()
    {
        var result = Email.Create("");

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("email.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Fail_When_Whitespace()
    {
        var result = Email.Create("   ");

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("email.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Fail_When_MissingAtSign()
    {
        var result = Email.Create("userexample.com");

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("email.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Fail_When_MissingDomain()
    {
        var result = Email.Create("user@");

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_Fail_When_MissingTld()
    {
        var result = Email.Create("user@example");

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_Fail_When_ExceedsMaxLength()
    {
        var localPart = new string('a', 243);
        var email = $"{localPart}@example.com"; // 243 + 1 + 11 = 255 > 254
        var result = Email.Create(email);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("email.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_BeEqual_When_SameEmailDifferentCase()
    {
        var email1 = Email.Create("User@Example.com").Value;
        var email2 = Email.Create("user@example.com").Value;

        Assert.AreEqual(email1, email2);
    }

    [TestMethod]
    public void Should_ReturnValue_When_ToString()
    {
        var email = Email.Create("test@example.com").Value;
        Assert.AreEqual("test@example.com", email.ToString());
    }
}
