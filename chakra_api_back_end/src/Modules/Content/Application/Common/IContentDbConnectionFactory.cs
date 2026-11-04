using SharedKernel.Infrastructure;

namespace Content.Application.Common;

/// <summary>
/// Content 模块 Dapper 连接工厂接口（供应用层读操作使用）
/// </summary>
public interface IContentDbConnectionFactory : IDbConnectionFactory
{
}
