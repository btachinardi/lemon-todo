namespace LemonDo.Domain.Tests.Tasks.ValueObjects;

using LemonDo.Domain.Tasks.ValueObjects;

[TestClass]
public sealed class TagTests
{
    [TestMethod]
    public void Should_CreateTag_When_ValidString()
    {
        var result = Tag.Create("urgent");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("urgent", result.Value.Value);
    }

    [TestMethod]
    public void Should_ConvertToLowercase_When_Creating()
    {
        var result = Tag.Create("URGENT");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("urgent", result.Value.Value);
    }

    [TestMethod]
    public void Should_TrimWhitespace_When_Creating()
    {
        var result = Tag.Create("  urgent  ");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("urgent", result.Value.Value);
    }

    [TestMethod]
    public void Should_Fail_When_NullTag()
    {
        var result = Tag.Create(null);

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_Fail_When_EmptyTag()
    {
        var result = Tag.Create("");

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_Fail_When_TagExceedsMaxLength()
    {
        var result = Tag.Create(new string('x', 51));

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_AlwaysBeLowercase_When_Created()
    {
        var random = new Random(42);
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
        for (var i = 0; i < 100; i++)
        {
            var length = random.Next(1, 51);
            var input = new string(Enumerable.Range(0, length)
                .Select(_ => chars[random.Next(chars.Length)]).ToArray());
            var result = Tag.Create(input);
            Assert.IsTrue(result.IsSuccess, $"Failed for input '{input}'");
            Assert.AreEqual(result.Value.Value, result.Value.Value.ToLowerInvariant(),
                $"Not lowercase for input '{input}'");
        }
    }
}
