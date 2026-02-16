namespace LemonDo.Domain.Tests.Common;

using LemonDo.Domain.Common;

[TestClass]
public sealed class PiiHasherTests
{
    [TestMethod]
    public void Should_Return64CharHexString_When_ValidEmail()
    {
        var hash = PiiHasher.HashEmail("test@example.com");

        Assert.AreEqual(64, hash.Length);
        Assert.IsTrue(hash.All(c => "0123456789ABCDEF".Contains(c)),
            "Hash should be uppercase hex string");
    }

    [TestMethod]
    public void Should_BeCaseInsensitive_When_HashingEmails()
    {
        var hash1 = PiiHasher.HashEmail("User@Example.COM");
        var hash2 = PiiHasher.HashEmail("user@example.com");

        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void Should_TrimWhitespace_When_HashingEmails()
    {
        var hash1 = PiiHasher.HashEmail("  test@example.com  ");
        var hash2 = PiiHasher.HashEmail("test@example.com");

        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void Should_ProduceDifferentHashes_When_DifferentEmails()
    {
        var hash1 = PiiHasher.HashEmail("alice@example.com");
        var hash2 = PiiHasher.HashEmail("bob@example.com");

        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public void Should_BeDeterministic_When_SameEmail()
    {
        var hash1 = PiiHasher.HashEmail("test@example.com");
        var hash2 = PiiHasher.HashEmail("test@example.com");

        Assert.AreEqual(hash1, hash2);
    }
}
