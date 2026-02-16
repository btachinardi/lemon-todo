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
        Assert.AreEqual(email.Redacted, result.Value.RedactedEmail);
        Assert.AreEqual(displayName.Redacted, result.Value.RedactedDisplayName);
        Assert.IsFalse(result.Value.IsDeactivated);
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

        var user = User.Reconstitute(id, "r***@example.com", "R***d", false);

        Assert.AreEqual(id, user.Id);
        Assert.AreEqual("r***@example.com", user.RedactedEmail);
        Assert.AreEqual("R***d", user.RedactedDisplayName);
        Assert.IsFalse(user.IsDeactivated);
        Assert.IsEmpty(user.DomainEvents);
    }

    [TestMethod]
    public void Should_Deactivate_When_Active()
    {
        var user = CreateActiveUser();

        var result = user.Deactivate();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(user.IsDeactivated);
    }

    [TestMethod]
    public void Should_FailDeactivation_When_AlreadyDeactivated()
    {
        var user = User.Reconstitute(UserId.New(), "t***@example.com", "T***t", isDeactivated: true);

        var result = user.Deactivate();

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("user.deactivation", result.Error.Code);
    }

    [TestMethod]
    public void Should_Reactivate_When_Deactivated()
    {
        var user = User.Reconstitute(UserId.New(), "t***@example.com", "T***t", isDeactivated: true);

        var result = user.Reactivate();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(user.IsDeactivated);
    }

    [TestMethod]
    public void Should_FailReactivation_When_NotDeactivated()
    {
        var user = CreateActiveUser();

        var result = user.Reactivate();

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("user.reactivation", result.Error.Code);
    }

    private static User CreateActiveUser()
    {
        var email = Email.Create("test@example.com").Value;
        var displayName = DisplayName.Create("Test User").Value;
        return User.Create(email, displayName).Value;
    }
}
