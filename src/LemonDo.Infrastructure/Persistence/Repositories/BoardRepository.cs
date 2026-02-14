namespace LemonDo.Infrastructure.Persistence.Repositories;

using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;

public sealed class BoardRepository(LemonDoDbContext context) : IBoardRepository
{
    public async Task<Board?> GetByIdAsync(BoardId id, CancellationToken ct = default)
    {
        return await context.Boards
            .Include(b => b.Columns)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<Board?> GetDefaultForUserAsync(UserId ownerId, CancellationToken ct = default)
    {
        return await context.Boards
            .Include(b => b.Columns)
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
