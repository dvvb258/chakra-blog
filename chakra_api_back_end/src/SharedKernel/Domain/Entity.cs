namespace SharedKernel.Domain;

/// <summary>
/// 领域实体基类
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// 实体ID
    /// </summary>
    public long Id { get; protected set; }

    /// <summary>
    /// 创建时间（UTC）
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后更新时间（UTC）
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// 版本号（用于乐观锁）
    /// </summary>
    public int Version { get; protected set; }

    /// <summary>
    /// 是否已软删除
    /// </summary>
    public bool IsDeleted { get; protected set; }

    /// <summary>
    /// 软删除时间（UTC）
    /// </summary>
    public DateTime? DeletedAt { get; protected set; }

    protected Entity()
    {
    }

    protected Entity(long id)
    {
        Id = id;
    }

    /// <summary>
    /// 标记为删除（软删除）
    /// </summary>
    public virtual void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 恢复删除
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }

    /// <summary>
    /// 更新修改时间
    /// </summary>
    public void SetUpdatedAt()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 递增实体修订版本号（用于乐观并发控制）
    /// </summary>
    public void IncrementVersion()
    {
        Version++;
    }

    /// <summary>
    /// 获取当前尚未分发的领域事件
    /// </summary>
    /// <returns>领域事件只读集合</returns>
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    /// <summary>
    /// 添加领域事件
    /// </summary>
    /// <param name="domainEvent">领域事件</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 清除已完成分发的领域事件
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
