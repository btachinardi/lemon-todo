namespace LemonDo.Domain.Administration;

/// <summary>
/// Justification reason for revealing redacted protected data.
/// Part of the break-the-glass audit trail required for HIPAA-grade access controls.
/// </summary>
public enum ProtectedDataRevealReason
{
    /// <summary>Revealing protected data to assist with a user support ticket.</summary>
    SupportTicket,

    /// <summary>Revealing protected data to fulfill a legal request (e.g., court order, subpoena).</summary>
    LegalRequest,

    /// <summary>Revealing protected data for account recovery or identity verification.</summary>
    AccountRecovery,

    /// <summary>Revealing protected data during a security investigation (e.g., suspected breach).</summary>
    SecurityInvestigation,

    /// <summary>Revealing protected data to fulfill a data subject access request (GDPR Article 15 / HIPAA right of access).</summary>
    DataSubjectRequest,

    /// <summary>Revealing protected data as part of an internal or external compliance audit.</summary>
    ComplianceAudit,

    /// <summary>Other reason — requires a free-form explanation in ReasonDetails.</summary>
    Other,
}
