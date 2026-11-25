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
    /// 文章点赞关系数据集
    /// </summary>
    public DbSet<ArticleLike> ArticleLikes => Set<ArticleLike>();

    /// <summary>
    /// 系列数据集
    /// </summary>
    public DbSet<Series> Series => Set<Series>();

    /// <summary>
    /// 分类数据集
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// 站点设置数据集（全站单行配置）
    /// </summary>
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 应用全局实体约定（雪花 ID + 软删除列名映射 + 全局软删除过滤器）
        modelBuilder.ApplyEntityConventions();

        // 文章点赞关系：同一访客对同一文章只允许一条有效记录
        modelBuilder.Entity<ArticleLike>(builder =>
        {
            builder.ToTable("article_likes");
            builder.Property(x => x.VisitorId)
                .HasMaxLength(64)
                .IsRequired();
            builder.HasIndex(x => new { x.ArticleId, x.VisitorId })
                .HasDatabaseName("ix_article_likes_article_id_visitor_id")
                .HasFilter("is_del = FALSE")
                .IsUnique();
            builder.HasOne<Article>()
                .WithMany()
                .HasForeignKey(x => x.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 站点设置：单行 JSON 配置，使用 PostgreSQL jsonb 存储覆盖项
        modelBuilder.Entity<SiteSetting>(builder =>
        {
            builder.ToTable("site_settings");
            builder.Property(x => x.ConfigJson)
                .HasColumnName("config")
                .HasColumnType("jsonb")
                .IsRequired();
        });
    }
}
