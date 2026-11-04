using Identity.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Infrastructure;

namespace Identity.Infrastructure.Data;

/// <summary>
/// 身份认证模块数据库上下文
/// </summary>
public class IdentityDbContext : ModuleDbContext
{
    /// <summary>
    /// 初始化身份认证数据库上下文
    /// </summary>
    /// <param name="options">上下文选项</param>
    /// <param name="publisher">领域事件发布器</param>
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, IPublisher publisher)
        : base(options, publisher)
    {
    }

    /// <summary>
    /// 用户数据集
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// 角色数据集
    /// </summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>
    /// 刷新令牌数据集
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// 菜单与权限资源数据集
    /// </summary>
    public DbSet<Menu> Menus => Set<Menu>();

    /// <summary>
    /// 角色与菜单关联数据集
    /// </summary>
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 应用全局实体约定（雪花 ID + 软删除列名映射 + 全局软删除过滤器）
        modelBuilder.ApplyEntityConventions();

        // 配置 User 实体
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");

            builder.Property(u => u.Username)
                .HasMaxLength(50)
                .IsRequired();
            builder.HasIndex(u => u.Username)
                .IsUnique();

            builder.Property(u => u.Email)
                .HasMaxLength(100);
            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasFilter("email IS NOT NULL AND is_del = false");

            builder.Property(u => u.Nickname)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(u => u.Role)
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(u => u.GithubId)
                .HasMaxLength(100);
            builder.HasIndex(u => u.GithubId)
                .IsUnique()
                .HasFilter("github_id IS NOT NULL AND is_del = false");
        });

        // 配置 RefreshToken 实体
        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.ToTable("refresh_tokens");

            builder.Property(t => t.TokenHash)
                .HasColumnName("token")
                .HasMaxLength(256)
                .IsRequired();
            builder.HasIndex(t => t.TokenHash)
                .HasDatabaseName("ix_refresh_tokens_token")
                .IsUnique();

            builder.Property(t => t.ReplacedByTokenHash)
                .HasColumnName("replaced_by_token")
                .HasMaxLength(256);

            builder.HasIndex(t => t.UserId);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置 Role 实体
        modelBuilder.Entity<Role>(builder =>
        {
            builder.ToTable("roles");

            builder.Property(r => r.Code)
                .HasMaxLength(30)
                .IsRequired();
            builder.HasIndex(r => r.Code)
                .IsUnique();

            builder.Property(r => r.Name)
                .HasMaxLength(50)
                .IsRequired();
            builder.Property(r => r.Description)
                .HasMaxLength(200);

            builder.Property(r => r.IsSystem)
                .IsRequired()
                .HasDefaultValue(false);
        });

        // 配置 Menu 实体
        modelBuilder.Entity<Menu>(builder =>
        {
            builder.ToTable("menus");

            builder.Property(m => m.Name)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(m => m.Path)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(m => m.PermissionCode)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(m => m.Icon)
                .HasMaxLength(50);

            builder.HasIndex(m => m.Path);
            builder.HasIndex(m => m.Sort);
        });

        // 配置 RoleMenu 实体
        modelBuilder.Entity<RoleMenu>(builder =>
        {
            builder.ToTable("role_menus");

            builder.HasIndex(rm => new { rm.RoleId, rm.MenuId })
                .IsUnique();

            builder.HasOne<Role>()
                .WithMany()
                .HasForeignKey(rm => rm.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Menu>()
                .WithMany()
                .HasForeignKey(rm => rm.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
