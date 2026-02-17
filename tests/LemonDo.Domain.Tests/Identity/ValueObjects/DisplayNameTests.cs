namespace LemonDo.Domain.Tests.Identity.ValueObjects;

using LemonDo.Domain.Identity.ValueObjects;

[TestClass]
public sealed class DisplayNameTests
{
    [TestMethod]
    public void Should_CreateDisplayName_When_ValidString()
    {
        var result = DisplayName.Create("John Doe");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("John Doe", result.Value.Value);
    }

    [TestMethod]
    public void Should_TrimWhitespace_When_Created()
    {
        var result = DisplayName.Create("  John Doe  ");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("John Doe", result.Value.Value);
    }

    [TestMethod]
    public void Should_Succeed_When_MinLength()
    {
        var result = DisplayName.Create("AB");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("AB", result.Value.Value);
    }

    [TestMethod]
    public void Should_Succeed_When_MaxLength()
    {
        var name = new string('A', DisplayName.MaxLength);
        var result = DisplayName.Create(name);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void Should_Fail_When_Null()
    {
        var result = DisplayName.Create(null);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("displayName.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Fail_When_Empty()
    {
        var result = DisplayName.Create("");

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("displayName.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Fail_When_TooShort()
    {
        var result = DisplayName.Create("A");

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("displayName.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Fail_When_ExceedsMaxLength()
    {
        var name = new string('A', DisplayName.MaxLength + 1);
        var result = DisplayName.Create(name);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("displayName.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_BeEqual_When_SameValue()
    {
        var name1 = DisplayName.Create("John").Value;
        var name2 = DisplayName.Create("John").Value;

        Assert.AreEqual(name1, name2);
    }

    [TestMethod]
    public void Should_NotBeEqual_When_DifferentValue()
    {
        var name1 = DisplayName.Create("John").Value;
        var name2 = DisplayName.Create("Jane").Value;

        Assert.AreNotEqual(name1, name2);
    }

    [TestMethod]
    public void Should_ReturnRedacted_When_ToString()
    {
        var name = DisplayName.Create("Test User").Value;
        // IProtectedData VOs return redacted form from ToString() to prevent accidental logging
        Assert.AreEqual("T***r", name.ToString());
    }

    // --- Redacted ---

    [TestMethod]
    public void Should_RedactDisplayName_When_StandardName()
    {
        var name = DisplayName.Create("John Doe").Value;
        Assert.AreEqual("J***e", name.Redacted);
    }

    [TestMethod]
    public void Should_RedactDisplayName_When_MinLength()
    {
        var name = DisplayName.Create("AB").Value;
        Assert.AreEqual("***", name.Redacted);
    }

    [TestMethod]
    public void Should_RedactDisplayName_When_ThreeChars()
    {
        var name = DisplayName.Create("Bob").Value;
        Assert.AreEqual("B***b", name.Redacted);
    }

    [TestMethod]
    public void Should_RedactDisplayName_When_LongName()
    {
        var name = DisplayName.Create("Alexander Hamilton").Value;
        Assert.AreEqual("A***n", name.Redacted);
    }

    [TestMethod]
    public void Should_RedactConsistently_When_SameName()
    {
        var name1 = DisplayName.Create("Test User").Value;
        var name2 = DisplayName.Create("Test User").Value;
        Assert.AreEqual(name1.Redacted, name2.Redacted);
    }
}
