namespace LemonDo.Domain.Administration;

/// <summary>
/// Justification reason for system-initiated PII decryption.
/// Recorded in the audit trail whenever the system (not a human admin) accesses encrypted PII.
/// </summary>
public enum SystemPiiAccessReason
{
    /// <summary>Decrypting email to send a transactional notification.</summary>
    TransactionalEmail,

    /// <summary>Decrypting email to send a password-reset link.</summary>
    PasswordResetEmail,

    /// <summary>Decrypting email to send an account-verification message.</summary>
    AccountVerificationEmail,

    /// <summary>Decrypting PII for a GDPR/HIPAA data-export request.</summary>
    DataExport,

    /// <summary>Decrypting PII during a one-time data migration.</summary>
    SystemMigration,
}
