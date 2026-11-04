namespace SharedKernel.Application.Exceptions;

/// <summary>
/// 并发冲突异常（乐观锁冲突）
/// </summary>
public class ConcurrencyException : BusinessException
{
    public ConcurrencyException(string message = "数据已被其他用户修改，请刷新后重试")
        : base(message, 409)
    {
    }

    public ConcurrencyException(string entityName, object key)
        : base($"实体 {entityName} (Id: {key}) 已被其他用户修改，请刷新后重试", 409)
    {
    }
}
