namespace LemonDo.Application.Administration.DTOs;

using LemonDo.Domain.Administration;

/// <summary>Data transfer object for an audit trail entry.</summary>
public sealed record AuditEntryDto(
    Guid Id,
    DateTimeOffset Timestamp,
    Guid? ActorId,
    AuditAction Action,
    string ResourceType,
    string? ResourceId,
    string? Details,
    string? IpAddress);
