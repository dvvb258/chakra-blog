using Dapper;
using Logging.Application.Common;
using Logging.Application.Logs.DTOs;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Logging.Application.Logs.Queries;

/// <summary>
/// 查询系统异常日志分页列表处理器
/// </summary>
public sealed class GetExceptionLogsQueryHandler : IQueryHandler<GetExceptionLogsQuery, PagedResult<ExceptionLogDto>>
{
    private readonly ILoggingDbConnectionFactory _connectionFactory;

    public GetExceptionLogsQueryHandler(ILoggingDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<ExceptionLogDto>> Handle(GetExceptionLogsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var conditions = new List<string> { "is_del = false" };

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            conditions.Add("(exception_type ILIKE @Keyword OR message ILIKE @Keyword OR path ILIKE @Keyword)");
        }

        if (!string.IsNullOrWhiteSpace(request.TraceId))
        {
            conditions.Add("trace_id ILIKE @TraceId");
        }

        if (request.StartDate.HasValue)
        {
            conditions.Add("created_at >= @StartDate");
        }

        if (request.EndDate.HasValue)
        {
            conditions.Add("created_at <= @EndDate");
        }

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var countSql = $"""
            SELECT COUNT(*)
            FROM exception_logs
            {whereClause}
            """;
        var dataSql = $"""
            SELECT id, exception_type, message, stack_trace, path, method, query_string,
                   request_body, client_ip, user_id, username, trace_id, created_at
            FROM exception_logs
            {whereClause}
            ORDER BY created_at DESC, id DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        var batchSql = $"""
            {countSql};

            {dataSql};
            """;

        var parameters = new
        {
            Keyword = $"%{request.Keyword?.Trim()}%",
            TraceId = $"%{request.TraceId?.Trim()}%",
            request.StartDate,
            request.EndDate,
            Offset = (request.PageIndex - 1) * request.PageSize,
            PageSize = request.PageSize,
        };

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(batchSql, parameters, cancellationToken: cancellationToken));

        var total = await multi.ReadFirstAsync<int>();
        var items = (await multi.ReadAsync<ExceptionLogDto>()).ToList();

        return new PagedResult<ExceptionLogDto>
        {
            Items = items,
            Total = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}
