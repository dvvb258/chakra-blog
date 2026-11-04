using Logging.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Infrastructure;

namespace Logging.Infrastructure.Data;

/// <summary>
/// 系统日志模块数据库上下文
/// </summary>
public class LoggingDbContext : ModuleDbContext
{
    /// <summary>
    /// 初始化日志数据库上下文
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="publisher">领域事件发布器</param>
    public LoggingDbContext(DbContextOptions<LoggingDbContext> options, IPublisher publisher)
        : base(options, publisher)
    {
    }

    /// <summary>业务操作审计日志</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>系统未处理异常日志</summary>
    public DbSet<ExceptionLog> ExceptionLogs => Set<ExceptionLog>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 应用全局实体约定（雪花 ID + 软删除列名映射 + 全局软删除过滤器）
        modelBuilder.ApplyEntityConventions();

        // 配置 AuditLog 实体
        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("audit_logs");

            builder.Property(a => a.Username).HasMaxLength(50);
            builder.Property(a => a.Module).HasMaxLength(50).IsRequired();
            builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
            builder.Property(a => a.Method).HasMaxLength(10);
            builder.Property(a => a.Path).HasMaxLength(256);
            builder.Property(a => a.ClientIp).HasMaxLength(50);
            builder.Property(a => a.TraceId).HasMaxLength(64);

            builder.HasIndex(a => a.CreatedAt);
            builder.HasIndex(a => a.Module);
            builder.HasIndex(a => a.Username);
            builder.HasIndex(a => a.IsSuccess);
            builder.HasIndex(a => a.TraceId);
        });

        // 配置 ExceptionLog 实体
        modelBuilder.Entity<ExceptionLog>(builder =>
        {
            builder.ToTable("exception_logs");

            builder.Property(e => e.ExceptionType).HasMaxLength(200).IsRequired();
            builder.Property(e => e.Method).HasMaxLength(10);
            builder.Property(e => e.Path).HasMaxLength(256);
            builder.Property(e => e.ClientIp).HasMaxLength(50);
            builder.Property(e => e.Username).HasMaxLength(50);
            builder.Property(e => e.TraceId).HasMaxLength(64);

            builder.HasIndex(e => e.CreatedAt);
            builder.HasIndex(e => e.ExceptionType);
            builder.HasIndex(e => e.TraceId);
        });
    }
}
