using SharedKernel.Infrastructure;

namespace Identity.Application.Common;

/// <summary>
/// Identity 模块 Dapper 连接工厂接口（供应用层读操作使用）
/// </summary>
public interface IIdentityDbConnectionFactory : IDbConnectionFactory
{
}
