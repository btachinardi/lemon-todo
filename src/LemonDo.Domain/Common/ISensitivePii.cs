namespace LemonDo.Domain.Common;

/// <summary>
/// Marker interface for value objects that contain Personally Identifiable Information (PII)
/// or Protected Health Information (PHI). Types implementing this interface declare that their
/// <see cref="ValueObject{T}.Value"/> may contain sensitive data, and that the
/// <see cref="Redacted"/> form should be used for display, logging, and non-encrypted storage.
/// </summary>
public interface ISensitivePii
{
    /// <summary>
    /// Returns a redacted form of the value, safe for display, logging,
    /// and storage in non-encrypted database columns.
    /// </summary>
    string Redacted { get; }
}
