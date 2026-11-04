using System.Data;

namespace SharedKernel.Infrastructure;

/// <summary>
/// Dapper数据库连接工厂接口（用于读操作）
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// 创建数据库连接
    /// </summary>
    /// <returns>数据库连接</returns>
    IDbConnection CreateConnection();
}
