namespace LemonDo.Domain.Tests.Tasks.ValueObjects;

using LemonDo.Domain.Tasks.ValueObjects;

[TestClass]
public sealed class ColumnNameTests
{
    [TestMethod]
    public void Should_CreateColumnName_When_ValidString()
    {
        var result = ColumnName.Create("To Do");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("To Do", result.Value.Value);
    }

    [TestMethod]
    public void Should_TrimWhitespace_When_Creating()
    {
        var result = ColumnName.Create("  In Progress  ");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("In Progress", result.Value.Value);
    }

    [TestMethod]
    public void Should_Fail_When_NullName()
    {
        var result = ColumnName.Create(null);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("column_name.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Fail_When_EmptyName()
    {
        var result = ColumnName.Create("");

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_Fail_When_WhitespaceOnlyName()
    {
        var result = ColumnName.Create("   ");

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        var result = ColumnName.Create(new string('x', 51));

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("column_name.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Succeed_When_NameIsExactlyMaxLength()
    {
        var result = ColumnName.Create(new string('x', 50));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(50, result.Value.Value.Length);
    }

    [TestMethod]
    public void Should_BeEqual_When_SameValue()
    {
        var a = ColumnName.Create("Done").Value;
        var b = ColumnName.Create("Done").Value;

        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void Should_NotBeEqual_When_DifferentValue()
    {
        var a = ColumnName.Create("To Do").Value;
        var b = ColumnName.Create("Done").Value;

        Assert.AreNotEqual(a, b);
    }
}
