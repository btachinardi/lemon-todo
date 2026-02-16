namespace LemonDo.Domain.Common;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Deterministic hashing utility for protected data values. Produces SHA-256 hashes suitable
/// for database lookups (e.g., finding a user by email without storing the email in plaintext).
/// </summary>
public static class ProtectedDataHasher
{
    /// <summary>
    /// Hashes an email address using SHA-256 after case-normalization.
    /// The result is a 64-character uppercase hex string.
    /// </summary>
    /// <param name="email">The email address to hash. Automatically trimmed and uppercased before hashing for consistent lookups.</param>
    public static string HashEmail(string email)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
