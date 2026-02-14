namespace LemonDo.Domain.Tests.Boards.ValueObjects;

using LemonDo.Domain.Boards.ValueObjects;

[TestClass]
public sealed class BoardNameTests
{
    [TestMethod]
    public void Should_CreateBoardName_When_ValidString()
    {
        var result = BoardName.Create("My Board");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("My Board", result.Value.Value);
    }

    [TestMethod]
    public void Should_TrimWhitespace_When_Creating()
    {
        var result = BoardName.Create("  My Board  ");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("My Board", result.Value.Value);
    }

    [TestMethod]
    public void Should_Fail_When_NullName()
    {
        var result = BoardName.Create(null);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board_name.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Fail_When_EmptyName()
    {
        var result = BoardName.Create("");

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_Fail_When_WhitespaceOnlyName()
    {
        var result = BoardName.Create("   ");

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        var result = BoardName.Create(new string('x', 101));

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board_name.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Succeed_When_NameIsExactlyMaxLength()
    {
        var result = BoardName.Create(new string('x', 100));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100, result.Value.Value.Length);
    }

    [TestMethod]
    public void Should_BeEqual_When_SameValue()
    {
        var a = BoardName.Create("My Board").Value;
        var b = BoardName.Create("My Board").Value;

        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void Should_NotBeEqual_When_DifferentValue()
    {
        var a = BoardName.Create("Board A").Value;
        var b = BoardName.Create("Board B").Value;

        Assert.AreNotEqual(a, b);
    }

    [TestMethod]
    public void Should_AlwaysSucceed_When_LengthWithinBounds()
    {
        var random = new Random(42);
        for (var i = 0; i < 100; i++)
        {
            var length = random.Next(1, 101);
            var name = new string((char)random.Next('a', 'z' + 1), length);
            Assert.IsTrue(BoardName.Create(name).IsSuccess, $"Failed for length {length}");
        }
    }
}
