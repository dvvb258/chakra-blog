using Dapper;
using Logging.Application.Common;
using Logging.Application.Logs.DTOs;
using SharedKernel.Application.CQRS;

namespace Logging.Application.Logs.Queries;

/// <summary>
/// 根据ID获取系统异常日志详情处理器
/// </summary>
public sealed class GetExceptionLogByIdQueryHandler : IQueryHandler<GetExceptionLogByIdQuery, ExceptionLogDto?>
{
    private readonly ILoggingDbConnectionFactory _connectionFactory;

    public GetExceptionLogByIdQueryHandler(ILoggingDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ExceptionLogDto?> Handle(GetExceptionLogByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT id, exception_type, message, stack_trace, path, method, query_string,
                   request_body, client_ip, user_id, username, trace_id, created_at
            FROM exception_logs
            WHERE id = @Id AND is_del = false
            """;

        var command = new CommandDefinition(sql, new { Id = request.Id }, cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<ExceptionLogDto>(command);
    }
}
