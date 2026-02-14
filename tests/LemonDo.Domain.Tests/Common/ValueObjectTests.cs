namespace LemonDo.Domain.Tests.Common;

using LemonDo.Domain.Common;

[TestClass]
public sealed class ValueObjectTests
{
    private sealed class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }

        public Address(string street, string city)
        {
            Street = street;
            City = city;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
        }
    }

    [TestMethod]
    public void Should_BeEqual_When_SameComponents()
    {
        var a = new Address("123 Main St", "Springfield");
        var b = new Address("123 Main St", "Springfield");

        Assert.AreEqual(a, b);
        Assert.IsTrue(a == b);
        Assert.IsTrue(a.Equals(b));
    }

    [TestMethod]
    public void Should_NotBeEqual_When_DifferentComponents()
    {
        var a = new Address("123 Main St", "Springfield");
        var b = new Address("456 Oak Ave", "Springfield");

        Assert.AreNotEqual(a, b);
        Assert.IsTrue(a != b);
    }

    [TestMethod]
    public void Should_HaveSameHashCode_When_Equal()
    {
        var a = new Address("123 Main St", "Springfield");
        var b = new Address("123 Main St", "Springfield");

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Should_HaveDifferentHashCode_When_NotEqual()
    {
        var a = new Address("123 Main St", "Springfield");
        var b = new Address("456 Oak Ave", "Shelbyville");

        Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Should_NotBeEqual_When_ComparedWithNull()
    {
        var a = new Address("123 Main St", "Springfield");

        Assert.IsFalse(a.Equals(null));
        Assert.IsTrue(a != null);
    }
}
