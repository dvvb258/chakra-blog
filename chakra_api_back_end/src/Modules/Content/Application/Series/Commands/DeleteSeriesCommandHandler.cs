using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Series.Commands;

/// <summary>
/// 删除专栏系列命令处理器
/// </summary>
public sealed class DeleteSeriesCommandHandler : ICommandHandler<DeleteSeriesCommand>
{
    private readonly BlogDbContext _dbContext;

    public DeleteSeriesCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteSeriesCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var series = await _dbContext.Series
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (series == null)
        {
            throw new NotFoundException("Series", command.Id);
        }

        // 软删除专栏系列
        series.MarkAsDeleted();

        // 将关联该专栏的所有文章解除专栏绑定
        var updatedAt = DateTime.UtcNow;
        await _dbContext.Articles
            .Where(a => a.SeriesId == command.Id && !a.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(a => a.SeriesId, (long?)null)
                    .SetProperty(a => a.UpdatedAt, updatedAt)
                    .SetProperty(a => a.Version, a => a.Version + 1),
                cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
