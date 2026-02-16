using LemonDo.Domain.Administration;
using LemonDo.Domain.Administration.Entities;

namespace LemonDo.Domain.Tests.Administration;

[TestClass]
public sealed class AuditEntryTests
{
    [TestMethod]
    public void Should_CreateAuditEntry_When_ValidParameters()
    {
        var actorId = Guid.NewGuid();
        var entry = AuditEntry.Create(actorId, AuditAction.TaskCreated, "Task", "task-123", "Created a task");

        Assert.IsNotNull(entry);
        Assert.AreEqual(actorId, entry.ActorId);
        Assert.AreEqual(AuditAction.TaskCreated, entry.Action);
        Assert.AreEqual("Task", entry.ResourceType);
        Assert.AreEqual("task-123", entry.ResourceId);
        Assert.AreEqual("Created a task", entry.Details);
    }

    [TestMethod]
    public void Should_AllowNullActorId_When_SystemInitiated()
    {
        var entry = AuditEntry.Create(null, AuditAction.UserRegistered, "User");

        Assert.IsNull(entry.ActorId);
    }

    [TestMethod]
    public void Should_GenerateUniqueId_When_Creating()
    {
        var entry1 = AuditEntry.Create(Guid.NewGuid(), AuditAction.TaskCreated, "Task");
        var entry2 = AuditEntry.Create(Guid.NewGuid(), AuditAction.TaskCreated, "Task");

        Assert.AreNotEqual(entry1.Id, entry2.Id);
    }

    [TestMethod]
    public void Should_SetTimestamps_When_Creating()
    {
        var before = DateTimeOffset.UtcNow;
        var entry = AuditEntry.Create(Guid.NewGuid(), AuditAction.TaskDeleted, "Task");
        var after = DateTimeOffset.UtcNow;

        Assert.IsTrue(entry.CreatedAt >= before && entry.CreatedAt <= after);
    }

    [TestMethod]
    public void Should_StoreIpAndUserAgent_When_Provided()
    {
        var entry = AuditEntry.Create(
            Guid.NewGuid(), AuditAction.ProtectedDataRevealed, "User", "user-456",
            ipAddress: "192.168.1.1", userAgent: "Mozilla/5.0");

        Assert.AreEqual("192.168.1.1", entry.IpAddress);
        Assert.AreEqual("Mozilla/5.0", entry.UserAgent);
    }
}
