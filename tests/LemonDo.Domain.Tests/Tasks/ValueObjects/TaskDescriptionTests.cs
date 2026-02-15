namespace LemonDo.Domain.Tests.Tasks.ValueObjects;

using LemonDo.Domain.Tasks.ValueObjects;

[TestClass]
public sealed class TaskDescriptionTests
{
    [TestMethod]
    public void Should_CreateDescription_When_ValidString()
    {
        var result = TaskDescription.Create("A detailed description");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("A detailed description", result.Value.Value);
    }

    [TestMethod]
    public void Should_CreateEmptyDescription_When_Null()
    {
        var result = TaskDescription.Create(null);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("", result.Value.Value);
    }

    [TestMethod]
    public void Should_CreateEmptyDescription_When_EmptyString()
    {
        var result = TaskDescription.Create("");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("", result.Value.Value);
    }

    [TestMethod]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        var result = TaskDescription.Create(new string('x', 10_001));

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("description.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Succeed_When_DescriptionIsExactlyMaxLength()
    {
        var result = TaskDescription.Create(new string('x', 10_000));

        Assert.IsTrue(result.IsSuccess);
    }
}
