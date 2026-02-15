namespace LemonDo.Infrastructure.Persistence.Repositories;

using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Identity.ValueObjects;
using Microsoft.EntityFrameworkCore;

/// <summary>EF Core implementation of <see cref="IBoardRepository"/>. Eagerly loads columns and cards.</summary>
public sealed class BoardRepository(LemonDoDbContext context) : IBoardRepository
{
    public async Task<Board?> GetByIdAsync(BoardId id, CancellationToken ct = default)
    {
        return await context.Boards
            .Include(b => b.Columns)
            .Include(b => b.Cards)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<Board?> GetDefaultForUserAsync(UserId ownerId, CancellationToken ct = default)
    {
        return await context.Boards
            .Include(b => b.Columns)
            .Include(b => b.Cards)
            .FirstOrDefaultAsync(b => b.OwnerId == ownerId, ct);
    }

    public async Task AddAsync(Board board, CancellationToken ct = default)
    {
        await context.Boards.AddAsync(board, ct);
    }

    public Task UpdateAsync(Board board, CancellationToken ct = default)
    {
        context.Boards.Update(board);
        return Task.CompletedTask;
    }
}
