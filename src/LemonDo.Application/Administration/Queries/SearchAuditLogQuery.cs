namespace LemonDo.Application.Administration.Queries;

using LemonDo.Application.Administration.DTOs;
using LemonDo.Domain.Administration;
using LemonDo.Domain.Administration.Repositories;
using LemonDo.Domain.Common;
using Microsoft.Extensions.Logging;

/// <summary>Query to search audit log entries with optional filters and pagination.</summary>
public sealed record SearchAuditLogQuery(
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    AuditAction? Action = null,
    Guid? ActorId = null,
    string? ResourceType = null,
    int Page = 1,
    int PageSize = 20);

/// <summary>Handles <see cref="SearchAuditLogQuery"/> by querying the audit entry repository.</summary>
public sealed class SearchAuditLogQueryHandler(
    IAuditEntryRepository repository,
    ILogger<SearchAuditLogQueryHandler> logger)
{
    /// <summary>Executes the audit log search query.</summary>
    public async Task<PagedResult<AuditEntryDto>> HandleAsync(
        SearchAuditLogQuery query, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Searching audit log: action={Action}, page={Page}", query.Action, query.Page);

        var result = await repository.SearchAsync(
            query.DateFrom,
            query.DateTo,
            query.Action,
            query.ActorId,
            query.ResourceType,
            query.Page,
            query.PageSize,
            cancellationToken);

        var dtos = result.Items.Select(e => new AuditEntryDto(
            e.Id.Value,
            e.CreatedAt,
            e.ActorId,
            e.Action,
            e.ResourceType,
            e.ResourceId,
            e.Details,
            e.IpAddress)).ToList();

        return new PagedResult<AuditEntryDto>(dtos, result.TotalCount, result.Page, result.PageSize);
    }
}
