using Yitter.IdGenerator;

namespace SharedKernel.Domain;

/// <summary>
/// 雪花ID生成器
/// </summary>
public static class SnowflakeIdGenerator
{
    private static volatile bool _isInitialized;
    private static readonly object LockObj = new();

    /// <summary>
    /// 初始化雪花ID生成器
    /// </summary>
    /// <param name="workerId">工作节点ID</param>
    public static void Initialize(ushort workerId = 1)
    {
        if (_isInitialized)
        {
            return;
        }

        lock (LockObj)
        {
            if (_isInitialized)
            {
                return;
            }

            var options = new IdGeneratorOptions(workerId)
            {
                BaseTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                WorkerIdBitLength = 4,
                SeqBitLength = 8,
            };
            YitIdHelper.SetIdGenerator(options);
            _isInitialized = true;
        }
    }

    /// <summary>
    /// 生成下一个ID
    /// </summary>
    /// <returns>雪花ID</returns>
    public static long NextId()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return YitIdHelper.NextId();
    }
}
