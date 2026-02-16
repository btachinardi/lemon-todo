namespace LemonDo.Api.Tests.Encryption;

using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

[TestClass]
public sealed class FieldEncryptionIntegrationTests
{
    private static CustomWebApplicationFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        // Force host initialization
        _factory.CreateClient(new() { HandleCookies = false });
    }

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    [TestMethod]
    public void Should_ResolveEncryptionService_When_RequestedFromDI()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IFieldEncryptionService>();

        Assert.IsNotNull(service);
        Assert.IsInstanceOfType<AesFieldEncryptionService>(service);
    }

    [TestMethod]
    public void Should_EncryptAndDecryptThroughDI_When_UsingRegisteredService()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IFieldEncryptionService>();

        const string email = "test@example.com";
        var encrypted = service.Encrypt(email);
        var decrypted = service.Decrypt(encrypted);

        Assert.AreNotEqual(email, encrypted);
        Assert.AreEqual(email, decrypted);
    }
}
