using Logging.Application.Logs.DTOs;
using Logging.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Logging.Application.Logs.Queries;

/// <summary>
/// 查询系统异常日志分页列表处理器
/// </summary>
public sealed class GetExceptionLogsQueryHandler : IQueryHandler<GetExceptionLogsQuery, PagedResult<ExceptionLogDto>>
{
    private readonly LoggingDbContext _dbContext;

    public GetExceptionLogsQueryHandler(LoggingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ExceptionLogDto>> Handle(GetExceptionLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.ExceptionLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            query = query.Where(x =>
                x.ExceptionType.Contains(kw) ||
                x.Message.Contains(kw) ||
                x.Path.Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(request.TraceId))
        {
            query = query.Where(x => x.TraceId != null && x.TraceId.Contains(request.TraceId.Trim()));
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= request.EndDate.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ExceptionLogDto
            {
                Id = x.Id,
                ExceptionType = x.ExceptionType,
                Message = x.Message,
                StackTrace = x.StackTrace,
                Path = x.Path,
                Method = x.Method,
                QueryString = x.QueryString,
                RequestBody = x.RequestBody,
                ClientIp = x.ClientIp,
                UserId = x.UserId,
                Username = x.Username,
                TraceId = x.TraceId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ExceptionLogDto>
        {
            Items = items,
            Total = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}
