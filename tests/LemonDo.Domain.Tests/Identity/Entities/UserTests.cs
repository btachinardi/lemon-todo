namespace LemonDo.Domain.Tests.Identity.Entities;

using LemonDo.Domain.Identity.Entities;
using LemonDo.Domain.Identity.Events;
using LemonDo.Domain.Identity.ValueObjects;

[TestClass]
public sealed class UserTests
{
    [TestMethod]
    public void Should_CreateUser_When_ValidInputs()
    {
        var email = Email.Create("user@example.com").Value;
        var displayName = DisplayName.Create("John Doe").Value;

        var result = User.Create(email, displayName);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("user@example.com", result.Value.Email.Value);
        Assert.AreEqual("John Doe", result.Value.DisplayName.Value);
        Assert.AreNotEqual(Guid.Empty, result.Value.Id.Value);
    }

    [TestMethod]
    public void Should_RaiseUserRegisteredEvent_When_Created()
    {
        var email = Email.Create("user@example.com").Value;
        var displayName = DisplayName.Create("John Doe").Value;

        var result = User.Create(email, displayName);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.Value.DomainEvents);

        var domainEvent = result.Value.DomainEvents[0];
        Assert.IsInstanceOfType<UserRegisteredEvent>(domainEvent);

        var registeredEvent = (UserRegisteredEvent)domainEvent;
        Assert.AreEqual(result.Value.Id, registeredEvent.UserId);
        Assert.AreEqual("user@example.com", registeredEvent.Email);
        Assert.AreEqual("John Doe", registeredEvent.DisplayName);
    }

    [TestMethod]
    public void Should_GenerateUniqueIds_When_MultipleUsersCreated()
    {
        var email1 = Email.Create("user1@example.com").Value;
        var email2 = Email.Create("user2@example.com").Value;
        var name = DisplayName.Create("Test").Value;

        var user1 = User.Create(email1, name).Value;
        var user2 = User.Create(email2, name).Value;

        Assert.AreNotEqual(user1.Id, user2.Id);
    }

    [TestMethod]
    public void Should_Reconstitute_When_ValidData()
    {
        var id = UserId.New();
        var email = Email.Create("restored@example.com").Value;
        var displayName = DisplayName.Create("Restored User").Value;

        var user = User.Reconstitute(id, email, displayName);

        Assert.AreEqual(id, user.Id);
        Assert.AreEqual("restored@example.com", user.Email.Value);
        Assert.AreEqual("Restored User", user.DisplayName.Value);
        Assert.IsEmpty(user.DomainEvents);
    }
}
