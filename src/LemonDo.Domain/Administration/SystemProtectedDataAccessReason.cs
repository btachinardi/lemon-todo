namespace LemonDo.Domain.Administration;

/// <summary>
/// Justification reason for system-initiated protected data decryption.
/// Recorded in the audit trail whenever the system (not a human admin) accesses encrypted protected data.
/// </summary>
public enum SystemProtectedDataAccessReason
{
    /// <summary>Decrypting email to send a transactional notification.</summary>
    TransactionalEmail,

    /// <summary>Decrypting email to send a password-reset link.</summary>
    PasswordResetEmail,

    /// <summary>Decrypting email to send an account-verification message.</summary>
    AccountVerificationEmail,

    /// <summary>Decrypting protected data for a GDPR/HIPAA data-export request.</summary>
    DataExport,

    /// <summary>Decrypting protected data during a one-time data migration.</summary>
    SystemMigration,
}
