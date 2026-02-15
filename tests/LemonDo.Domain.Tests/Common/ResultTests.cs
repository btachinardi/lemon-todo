namespace LemonDo.Domain.Tests.Common;

using LemonDo.Domain.Common;

[TestClass]
public sealed class ResultTests
{
    [TestMethod]
    public void Should_BeSuccess_When_CreatedWithValue()
    {
        var result = Result<int, string>.Success(42);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.IsFailure);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void Should_BeFailure_When_CreatedWithError()
    {
        var result = Result<int, string>.Failure("something went wrong");

        Assert.IsTrue(result.IsFailure);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("something went wrong", result.Error);
    }

    [TestMethod]
    public void Should_ThrowInvalidOperation_When_AccessingValueOnFailure()
    {
        var result = Result<int, string>.Failure("error");

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = result.Value);
    }

    [TestMethod]
    public void Should_ThrowInvalidOperation_When_AccessingErrorOnSuccess()
    {
        var result = Result<int, string>.Success(42);

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = result.Error);
    }

    [TestMethod]
    public void Should_MapValue_When_Success()
    {
        var result = Result<int, string>.Success(42);

        var mapped = result.Map(v => v.ToString());

        Assert.IsTrue(mapped.IsSuccess);
        Assert.AreEqual("42", mapped.Value);
    }

    [TestMethod]
    public void Should_PreserveError_When_MappingFailure()
    {
        var result = Result<int, string>.Failure("error");

        var mapped = result.Map(v => v.ToString());

        Assert.IsTrue(mapped.IsFailure);
        Assert.AreEqual("error", mapped.Error);
    }

    [TestMethod]
    public void Should_BeSuccess_When_CreatedWithUnitResult()
    {
        var result = Result<string>.Success();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.IsFailure);
    }

    [TestMethod]
    public void Should_BeFailure_When_UnitResultCreatedWithError()
    {
        var result = Result<string>.Failure("validation failed");

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("validation failed", result.Error);
    }
}
