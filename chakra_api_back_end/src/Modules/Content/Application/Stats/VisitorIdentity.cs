using System.Security.Cryptography;
using System.Text;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Stats;

/// <summary>
/// 统计接口匿名访客标识解析器
/// </summary>
internal static class VisitorIdentity
{
    /// <summary>
    /// 将前端访客标识（缺失时回退客户端 IP）转换为固定长度的匿名哈希
    /// </summary>
    /// <param name="visitorId">前端持久化访客标识</param>
    /// <param name="clientIp">客户端 IP 地址</param>
    /// <returns>可用于去重与存储的匿名标识</returns>
    public static string Resolve(string? visitorId, string? clientIp)
    {
        var source = string.IsNullOrWhiteSpace(visitorId) ? clientIp?.Trim() : visitorId.Trim();
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new BusinessException("无法识别访客身份，请刷新页面后重试", 400);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }
}
