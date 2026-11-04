namespace SharedKernel.Application.Exceptions;

/// <summary>
/// 实体未找到异常
/// </summary>
public class NotFoundException : BusinessException
{
    public NotFoundException(string message)
        : base(message, 404)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"实体 {entityName} (Id: {key}) 未找到", 404)
    {
    }
}
