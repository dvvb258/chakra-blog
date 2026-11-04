using Logging.Application.Logs.DTOs;
using Logging.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;

namespace Logging.Application.Logs.Queries;

/// <summary>
/// 根据ID获取系统异常日志详情处理器
/// </summary>
public sealed class GetExceptionLogByIdQueryHandler : IQueryHandler<GetExceptionLogByIdQuery, ExceptionLogDto?>
{
    private readonly LoggingDbContext _dbContext;

    public GetExceptionLogByIdQueryHandler(LoggingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExceptionLogDto?> Handle(GetExceptionLogByIdQuery request, CancellationToken cancellationToken)
    {
        var x = await _dbContext.ExceptionLogs.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        if (x == null)
        {
            return null;
        }

        return new ExceptionLogDto
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
        };
    }
}
