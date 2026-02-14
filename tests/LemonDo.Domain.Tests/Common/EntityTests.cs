namespace LemonDo.Domain.Tests.Common;

using LemonDo.Domain.Common;

[TestClass]
public sealed class EntityTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public string Name { get; set; }

        public TestEntity(Guid id, string name) : base(id)
        {
            Name = name;
        }
    }

    [TestMethod]
    public void Should_BeEqual_When_SameId()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id, "Alice");
        var b = new TestEntity(id, "Bob");

        Assert.AreEqual(a, b);
        Assert.IsTrue(a == b);
    }

    [TestMethod]
    public void Should_NotBeEqual_When_DifferentId()
    {
        var a = new TestEntity(Guid.NewGuid(), "Alice");
        var b = new TestEntity(Guid.NewGuid(), "Alice");

        Assert.AreNotEqual(a, b);
        Assert.IsTrue(a != b);
    }

    [TestMethod]
    public void Should_HaveSameHashCode_When_SameId()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id, "Alice");
        var b = new TestEntity(id, "Bob");

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Should_SetCreatedAt_When_Constructed()
    {
        var before = DateTimeOffset.UtcNow;
        var entity = new TestEntity(Guid.NewGuid(), "Test");
        var after = DateTimeOffset.UtcNow;

        Assert.IsTrue(entity.CreatedAt >= before && entity.CreatedAt <= after);
    }

    [TestMethod]
    public void Should_NotBeEqual_When_ComparedWithNull()
    {
        var entity = new TestEntity(Guid.NewGuid(), "Test");

        Assert.IsFalse(entity.Equals(null));
    }

    [TestMethod]
    public void Should_CollectDomainEvents()
    {
        var entity = new TestEntity(Guid.NewGuid(), "Test");

        Assert.IsEmpty(entity.DomainEvents);
    }
}
