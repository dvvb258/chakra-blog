using Dapper;
using Logging.Application.Common;
using Logging.Application.Logs.DTOs;
using SharedKernel.Application;
using SharedKernel.Application.CQRS;

namespace Logging.Application.Logs.Queries;

/// <summary>
/// 查询操作审计日志分页列表处理器
/// </summary>
public sealed class GetAuditLogsQueryHandler : IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly ILoggingDbConnectionFactory _connectionFactory;

    public GetAuditLogsQueryHandler(ILoggingDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var conditions = new List<string> { "is_del = false" };

        if (!string.IsNullOrWhiteSpace(request.Module))
        {
            conditions.Add("module = @Module");
        }

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            conditions.Add("username ILIKE @Username");
        }

        if (request.IsSuccess.HasValue)
        {
            conditions.Add("is_success = @IsSuccess");
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
            FROM audit_logs
            {whereClause}
            """;
        var dataSql = $"""
            SELECT id, user_id, username, module, action, method, path, parameters,
                   client_ip, duration_ms, is_success, error_message, trace_id, created_at
            FROM audit_logs
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
            Module = request.Module?.Trim(),
            Username = $"%{request.Username?.Trim()}%",
            IsSuccess = request.IsSuccess,
            TraceId = $"%{request.TraceId?.Trim()}%",
            request.StartDate,
            request.EndDate,
            Offset = (request.PageIndex - 1) * request.PageSize,
            PageSize = request.PageSize,
        };

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(batchSql, parameters, cancellationToken: cancellationToken));

        var total = await multi.ReadFirstAsync<int>();
        var items = (await multi.ReadAsync<AuditLogDto>()).ToList();

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            Total = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}
