namespace LemonDo.Api.Endpoints;

using System.Security.Claims;
using LemonDo.Domain.Identity.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Application.Common;

/// <summary>Minimal API endpoints for user onboarding status.</summary>
public static class OnboardingEndpoints
{
    /// <summary>Maps onboarding endpoints under <c>/api/onboarding</c>.</summary>
    public static RouteGroupBuilder MapOnboardingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/onboarding").WithTags("Onboarding").RequireAuthorization();

        group.MapGet("/status", GetStatus).Produces<OnboardingStatusResponse>();
        group.MapPost("/complete", Complete).Produces<OnboardingStatusResponse>();

        return group;
    }

    private static async Task<IResult> GetStatus(
        ClaimsPrincipal principal,
        IUserRepository userRepository,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) return Results.Unauthorized();

        return Results.Ok(new OnboardingStatusResponse(
            user.OnboardingCompletedAt.HasValue,
            user.OnboardingCompletedAt?.ToString("O")));
    }

    private static async Task<IResult> Complete(
        ClaimsPrincipal principal,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) return Results.Unauthorized();

        user.CompleteOnboarding();
        await unitOfWork.SaveChangesAsync(ct);

        return Results.Ok(new OnboardingStatusResponse(true, user.OnboardingCompletedAt?.ToString("O")));
    }

    private static UserId? GetUserId(ClaimsPrincipal principal)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr is null || !Guid.TryParse(userIdStr, out var guid))
            return null;
        return UserId.Reconstruct(guid);
    }
}

/// <summary>Onboarding status response.</summary>
/// <param name="Completed">Whether the user has completed onboarding.</param>
/// <param name="CompletedAt">ISO 8601 timestamp of completion, or null.</param>
public sealed record OnboardingStatusResponse(bool Completed, string? CompletedAt);
