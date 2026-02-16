namespace LemonDo.Domain.Common;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Deterministic hashing utility for PII values. Produces SHA-256 hashes suitable
/// for database lookups (e.g., finding a user by email without storing the email in plaintext).
/// </summary>
public static class PiiHasher
{
    /// <summary>
    /// Hashes an email address using SHA-256 after case-normalization.
    /// The result is a 64-character uppercase hex string.
    /// </summary>
    public static string HashEmail(string email)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
