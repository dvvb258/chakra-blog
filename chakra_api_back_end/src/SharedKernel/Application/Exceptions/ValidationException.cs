namespace SharedKernel.Application.Exceptions;

/// <summary>
/// 验证异常
/// </summary>
public class ValidationException : BusinessException
{
    /// <summary>
    /// 字段错误字典（字段名 → 错误消息列表）
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("验证失败", 422)
    {
        Errors = errors;
    }
}
