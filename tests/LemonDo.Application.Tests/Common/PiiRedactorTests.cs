namespace LemonDo.Application.Tests.Common;

using LemonDo.Application.Common;

[TestClass]
public sealed class PiiRedactorTests
{
    [TestMethod]
    public void Should_RedactEmail_When_StandardEmail()
    {
        var redacted = PiiRedactor.RedactEmail("john@example.com");
        Assert.AreEqual("j***@example.com", redacted);
    }

    [TestMethod]
    public void Should_RedactEmail_When_SingleCharacterLocal()
    {
        var redacted = PiiRedactor.RedactEmail("j@example.com");
        Assert.AreEqual("***@***", redacted);
    }

    [TestMethod]
    public void Should_RedactEmail_When_NoAtSign()
    {
        var redacted = PiiRedactor.RedactEmail("invalid");
        Assert.AreEqual("***@***", redacted);
    }

    [TestMethod]
    public void Should_RedactName_When_StandardName()
    {
        var redacted = PiiRedactor.RedactName("John Doe");
        Assert.AreEqual("J***e", redacted);
    }

    [TestMethod]
    public void Should_RedactName_When_ShortName()
    {
        var redacted = PiiRedactor.RedactName("Jo");
        Assert.AreEqual("***", redacted);
    }

    [TestMethod]
    public void Should_RedactName_When_SingleCharacter()
    {
        var redacted = PiiRedactor.RedactName("J");
        Assert.AreEqual("***", redacted);
    }

    [TestMethod]
    public void Should_RedactName_When_ThreeCharacters()
    {
        var redacted = PiiRedactor.RedactName("Joe");
        Assert.AreEqual("J***e", redacted);
    }
}
