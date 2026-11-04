using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Series.Commands;

/// <summary>
/// 更新专栏系列命令处理器
/// </summary>
public sealed class UpdateSeriesCommandHandler : ICommandHandler<UpdateSeriesCommand>
{
    private readonly BlogDbContext _dbContext;

    public UpdateSeriesCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateSeriesCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var series = await _dbContext.Series
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (series == null)
        {
            throw new NotFoundException("Series", command.Id);
        }

        // 乐观并发锁核验：若客户端提供了版本号且与当前数据库不一致，阻断修改防覆盖
        if (req.Version.HasValue && req.Version.Value >= 0 && series.Version != req.Version.Value)
        {
            throw new BusinessException(
                $"当前专栏已被其他页面或用户更新（数据库当前: v{series.Version}，提交版本: v{req.Version.Value}）。请刷新页面后重试。",
                409);
        }

        series.Update(
            name: req.Name.Trim(),
            description: req.Description?.Trim() ?? string.Empty,
            coverUrl: req.CoverUrl?.Trim() ?? string.Empty,
            theme: req.Theme?.Trim() ?? string.Empty);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
