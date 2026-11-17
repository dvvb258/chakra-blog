using SharedKernel.Infrastructure;

namespace Logging.Application.Common;

/// <summary>
/// Logging 模块 Dapper 连接工厂接口（供应用层读操作使用）
/// </summary>
public interface ILoggingDbConnectionFactory : IDbConnectionFactory
{
}
