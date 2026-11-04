using Content.Infrastructure.Data;
using SharedKernel.Application.CQRS;
using SeriesEntity = Content.Domain.Entities.Series;

namespace Content.Application.Series.Commands;

/// <summary>
/// 创建专栏系列命令处理器
/// </summary>
public sealed class CreateSeriesCommandHandler : ICommandHandler<CreateSeriesCommand, long>
{
    private readonly BlogDbContext _dbContext;

    public CreateSeriesCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<long> Handle(CreateSeriesCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var author = string.IsNullOrWhiteSpace(command.Author) ? "博主" : command.Author.Trim();

        var series = new SeriesEntity(
            name: req.Name.Trim(),
            description: req.Description?.Trim() ?? string.Empty,
            coverUrl: req.CoverUrl?.Trim() ?? string.Empty,
            theme: req.Theme?.Trim() ?? string.Empty,
            author: author);

        _dbContext.Series.Add(series);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return series.Id;
    }
}
