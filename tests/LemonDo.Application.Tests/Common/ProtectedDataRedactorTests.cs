namespace LemonDo.Application.Tests.Common;

using LemonDo.Application.Common;

[TestClass]
public sealed class ProtectedDataRedactorTests
{
    [TestMethod]
    public void Should_RedactEmail_When_StandardEmail()
    {
        var redacted = ProtectedDataRedactor.RedactEmail("john@example.com");
        Assert.AreEqual("j***@example.com", redacted);
    }

    [TestMethod]
    public void Should_RedactEmail_When_SingleCharacterLocal()
    {
        var redacted = ProtectedDataRedactor.RedactEmail("j@example.com");
        Assert.AreEqual("***@***", redacted);
    }

    [TestMethod]
    public void Should_RedactEmail_When_NoAtSign()
    {
        var redacted = ProtectedDataRedactor.RedactEmail("invalid");
        Assert.AreEqual("***@***", redacted);
    }

    [TestMethod]
    public void Should_RedactName_When_StandardName()
    {
        var redacted = ProtectedDataRedactor.RedactName("John Doe");
        Assert.AreEqual("J***e", redacted);
    }

    [TestMethod]
    public void Should_RedactName_When_ShortName()
    {
        var redacted = ProtectedDataRedactor.RedactName("Jo");
        Assert.AreEqual("***", redacted);
    }

    [TestMethod]
    public void Should_RedactName_When_SingleCharacter()
    {
        var redacted = ProtectedDataRedactor.RedactName("J");
        Assert.AreEqual("***", redacted);
    }

    [TestMethod]
    public void Should_RedactName_When_ThreeCharacters()
    {
        var redacted = ProtectedDataRedactor.RedactName("Joe");
        Assert.AreEqual("J***e", redacted);
    }
}
