namespace SharedKernel.Common;

/// <summary>
/// 跨平台时区辅助工具（默认中国标准时间，支持跨平台无缝解析）
/// </summary>
public static class TimeZoneHelper
{
    private static readonly Lazy<TimeZoneInfo> DefaultZoneLazy = new(() =>
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "China Standard Time" : "Asia/Shanghai"));

    /// <summary>
    /// 获取应用配置的时区信息（跨平台安全）
    /// </summary>
    public static TimeZoneInfo AppTimeZone => DefaultZoneLazy.Value;

    /// <summary>
    /// PostgreSQL 数据库统一支持的 IANA 时区标识名
    /// </summary>
    public const string DbTimeZone = "Asia/Shanghai";

    /// <summary>
    /// 将 UTC 时间转换为应用本地业务时间
    /// </summary>
    /// <param name="utcDateTime">UTC 时间</param>
    /// <returns>应用本地时间</returns>
    public static DateTime ConvertToLocalTime(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Local
            ? utcDateTime.ToUniversalTime()
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utc, AppTimeZone);
    }
}
