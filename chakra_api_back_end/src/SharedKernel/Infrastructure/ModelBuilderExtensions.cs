using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;

namespace SharedKernel.Infrastructure;

/// <summary>
/// EF Core 模型构建器全局约定扩展
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// 应用实体通用约定（雪花 ID 客户端生成、is_del 软删除列名映射与全局软删除过滤器）
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    /// <returns>模型构建器</returns>
    public static ModelBuilder ApplyEntityConventions(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                var entityBuilder = modelBuilder.Entity(entityType.ClrType);

                // 1. 雪花 ID 由客户端生成，禁用数据库自增
                entityBuilder.Property(nameof(Entity.Id)).ValueGeneratedNever();

                // 2. 软删除列名统一映射到 is_del
                entityBuilder.Property(nameof(Entity.IsDeleted)).HasColumnName("is_del");

                // 3. 版本号参与 UPDATE/DELETE 条件，实现数据库级乐观并发控制
                entityBuilder.Property(nameof(Entity.Version)).IsConcurrencyToken();

                // 4. 应用软删除全局过滤器
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(Entity.IsDeleted));
                var falseConstant = Expression.Constant(false);
                var body = Expression.Equal(property, falseConstant);
                var lambda = Expression.Lambda(body, parameter);

                entityBuilder.HasQueryFilter(lambda);
            }
        }

        return modelBuilder;
    }
}
