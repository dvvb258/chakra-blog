namespace SharedKernel.Application.Exceptions;

/// <summary>
/// 业务异常基类
/// </summary>
public class BusinessException : Exception
{
    /// <summary>
    /// HTTP 状态码
    /// </summary>
    public int Code { get; }

    public BusinessException(string message, int code = 400)
        : base(message)
    {
        Code = code;
    }
}
