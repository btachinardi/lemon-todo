namespace LemonDo.Domain.Tests.Tasks.ValueObjects;

using LemonDo.Domain.Tasks.ValueObjects;

[TestClass]
public sealed class SensitiveNoteTests
{
    [TestMethod]
    public void Should_CreateSensitiveNote_When_ValidString()
    {
        var result = SensitiveNote.Create("This is a secret note");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("This is a secret note", result.Value.Value);
    }

    [TestMethod]
    public void Should_TrimWhitespace_When_Creating()
    {
        var result = SensitiveNote.Create("  trimmed  ");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("trimmed", result.Value.Value);
    }

    [TestMethod]
    public void Should_Fail_When_NullOrEmpty()
    {
        Assert.IsTrue(SensitiveNote.Create(null).IsFailure);
        Assert.IsTrue(SensitiveNote.Create("").IsFailure);
        Assert.IsTrue(SensitiveNote.Create("   ").IsFailure);
    }

    [TestMethod]
    public void Should_Fail_When_ExceedsMaxLength()
    {
        var result = SensitiveNote.Create(new string('x', SensitiveNote.MaxLength + 1));
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("sensitiveNote.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Succeed_When_ExactlyMaxLength()
    {
        var result = SensitiveNote.Create(new string('x', SensitiveNote.MaxLength));
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void Should_ReturnProtectedRedacted()
    {
        var note = SensitiveNote.Create("Secret content").Value;
        Assert.AreEqual("[PROTECTED]", note.Redacted);
    }

    [TestMethod]
    public void Should_ReconstructFromPersistedValue()
    {
        var original = SensitiveNote.Create("Some secret").Value;
        var reconstructed = SensitiveNote.Reconstruct(original.Value);
        Assert.AreEqual(original, reconstructed);
    }

    [TestMethod]
    public void Should_ImplementIProtectedData()
    {
        var note = SensitiveNote.Create("Secret").Value;
        Assert.IsInstanceOfType<LemonDo.Domain.Common.IProtectedData>(note);
    }
}
