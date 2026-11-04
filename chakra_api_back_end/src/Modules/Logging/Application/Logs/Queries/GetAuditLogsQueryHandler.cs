using Logging.Application.Logs.DTOs;
using Logging.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Logging.Application.Logs.Queries;

/// <summary>
/// 查询操作审计日志分页列表处理器
/// </summary>
public sealed class GetAuditLogsQueryHandler : IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly LoggingDbContext _dbContext;

    public GetAuditLogsQueryHandler(LoggingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Module))
        {
            query = query.Where(x => x.Module == request.Module.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            query = query.Where(x => x.Username != null && x.Username.Contains(request.Username.Trim()));
        }

        if (request.IsSuccess.HasValue)
        {
            query = query.Where(x => x.IsSuccess == request.IsSuccess.Value);
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
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Username = x.Username,
                Module = x.Module,
                Action = x.Action,
                Method = x.Method,
                Path = x.Path,
                Parameters = x.Parameters,
                ClientIp = x.ClientIp,
                DurationMs = x.DurationMs,
                IsSuccess = x.IsSuccess,
                ErrorMessage = x.ErrorMessage,
                TraceId = x.TraceId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            Total = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}
