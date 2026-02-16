namespace LemonDo.Api.Tests.Encryption;

using System.Security.Cryptography;
using LemonDo.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

[TestClass]
public sealed class AesFieldEncryptionServiceTests
{
    private static AesFieldEncryptionService CreateService()
    {
        var keyBytes = RandomNumberGenerator.GetBytes(32);
        var keyBase64 = Convert.ToBase64String(keyBytes);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:FieldEncryptionKey"] = keyBase64
            })
            .Build();

        return new AesFieldEncryptionService(config);
    }

    [TestMethod]
    public void Should_RoundtripEncryptDecrypt_When_SimpleString()
    {
        var service = CreateService();
        const string plaintext = "test@example.com";

        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(encrypted);

        Assert.AreEqual(plaintext, decrypted);
    }

    [TestMethod]
    public void Should_ProduceDifferentCiphertext_When_SamePlaintextEncryptedTwice()
    {
        var service = CreateService();
        const string plaintext = "same value";

        var encrypted1 = service.Encrypt(plaintext);
        var encrypted2 = service.Encrypt(plaintext);

        Assert.AreNotEqual(encrypted1, encrypted2, "Each encryption should use a unique IV");
    }

    [TestMethod]
    public void Should_RoundtripEncryptDecrypt_When_EmptyString()
    {
        var service = CreateService();
        const string plaintext = "";

        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(encrypted);

        Assert.AreEqual(plaintext, decrypted);
    }

    [TestMethod]
    public void Should_RoundtripEncryptDecrypt_When_UnicodeString()
    {
        var service = CreateService();
        const string plaintext = "Usuário José — açúcar & café ☕";

        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(encrypted);

        Assert.AreEqual(plaintext, decrypted);
    }

    [TestMethod]
    public void Should_RoundtripEncryptDecrypt_When_LongString()
    {
        var service = CreateService();
        var plaintext = new string('x', 10_000);

        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(encrypted);

        Assert.AreEqual(plaintext, decrypted);
    }

    [TestMethod]
    public void Should_ThrowCryptographicException_When_CiphertextTampered()
    {
        var service = CreateService();
        var encrypted = service.Encrypt("sensitive data");

        // Tamper with the ciphertext by flipping a byte
        var bytes = Convert.FromBase64String(encrypted);
        bytes[^1] ^= 0xFF;
        var tampered = Convert.ToBase64String(bytes);

        // AES-GCM throws AuthenticationTagMismatchException (subclass of CryptographicException)
        Assert.ThrowsExactly<AuthenticationTagMismatchException>(() => service.Decrypt(tampered));
    }

    [TestMethod]
    public void Should_ThrowCryptographicException_When_CiphertextTooShort()
    {
        var service = CreateService();
        var tooShort = Convert.ToBase64String(new byte[10]); // Less than IV + Tag (28 bytes)

        Assert.ThrowsExactly<CryptographicException>(() => service.Decrypt(tooShort));
    }

    [TestMethod]
    public void Should_ThrowInvalidOperationException_When_KeyMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Assert.ThrowsExactly<InvalidOperationException>(() => new AesFieldEncryptionService(config));
    }

    [TestMethod]
    public void Should_ThrowInvalidOperationException_When_KeyWrongSize()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:FieldEncryptionKey"] = Convert.ToBase64String(new byte[16]) // 16 bytes instead of 32
            })
            .Build();

        Assert.ThrowsExactly<InvalidOperationException>(() => new AesFieldEncryptionService(config));
    }

    [TestMethod]
    public void Should_RoundtripArbitraryStrings_When_PropertyBased()
    {
        var service = CreateService();
        var random = new Random(42);

        for (var i = 0; i < 100; i++)
        {
            var length = random.Next(0, 500);
            var chars = new char[length];
            for (var j = 0; j < length; j++)
                chars[j] = (char)random.Next(32, 0xD800); // Valid UTF-16 range

            var plaintext = new string(chars);
            var encrypted = service.Encrypt(plaintext);
            var decrypted = service.Decrypt(encrypted);

            Assert.AreEqual(plaintext, decrypted, $"Failed for string of length {length}");
        }
    }
}
