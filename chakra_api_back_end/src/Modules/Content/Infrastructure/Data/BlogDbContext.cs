using Content.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Infrastructure;

namespace Content.Infrastructure.Data;

/// <summary>
/// EF Core数据库上下文
/// </summary>
public class BlogDbContext : ModuleDbContext
{
    /// <summary>
    /// 初始化博客数据库上下文
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="publisher">领域事件发布器</param>
    public BlogDbContext(DbContextOptions<BlogDbContext> options, IPublisher publisher)
        : base(options, publisher)
    {
    }

    /// <summary>
    /// 文章数据集
    /// </summary>
    public DbSet<Article> Articles => Set<Article>();

    /// <summary>
    /// 系列数据集
    /// </summary>
    public DbSet<Series> Series => Set<Series>();

    /// <summary>
    /// 分类数据集
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 应用全局实体约定（雪花 ID + 软删除列名映射 + 全局软删除过滤器）
        modelBuilder.ApplyEntityConventions();
    }
}
