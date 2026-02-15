namespace LemonDo.Domain.Tests.Tasks.ValueObjects;

using LemonDo.Domain.Tasks.ValueObjects;

[TestClass]
public sealed class TaskTitleTests
{
    [TestMethod]
    public void Should_CreateTitle_When_ValidString()
    {
        var result = TaskTitle.Create("Buy groceries");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Buy groceries", result.Value.Value);
    }

    [TestMethod]
    public void Should_TrimWhitespace_When_Creating()
    {
        var result = TaskTitle.Create("  Buy groceries  ");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Buy groceries", result.Value.Value);
    }

    [TestMethod]
    public void Should_Fail_When_NullTitle()
    {
        var result = TaskTitle.Create(null);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("title.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Fail_When_EmptyTitle()
    {
        var result = TaskTitle.Create("");

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_Fail_When_WhitespaceOnlyTitle()
    {
        var result = TaskTitle.Create("   ");

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_Fail_When_TitleExceedsMaxLength()
    {
        var result = TaskTitle.Create(new string('x', 501));

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("title.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Succeed_When_TitleIsExactlyMaxLength()
    {
        var result = TaskTitle.Create(new string('x', 500));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(500, result.Value.Value.Length);
    }

    [TestMethod]
    public void Should_BeEqual_When_SameValue()
    {
        var a = TaskTitle.Create("Buy groceries").Value;
        var b = TaskTitle.Create("Buy groceries").Value;

        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void Should_NotBeEqual_When_DifferentValue()
    {
        var a = TaskTitle.Create("Buy groceries").Value;
        var b = TaskTitle.Create("Clean house").Value;

        Assert.AreNotEqual(a, b);
    }

    [TestMethod]
    public void Should_AlwaysSucceed_When_LengthWithinBounds()
    {
        var random = new Random(42);
        for (var i = 0; i < 100; i++)
        {
            var length = random.Next(1, 501);
            var title = new string((char)random.Next('a', 'z' + 1), length);
            Assert.IsTrue(TaskTitle.Create(title).IsSuccess, $"Failed for length {length}");
        }
    }
}
