using Content.Domain.Entities;
using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Settings.Commands;

/// <summary>
/// 更新站点设置命令处理器
/// </summary>
public sealed class UpdateSiteSettingsCommandHandler : ICommandHandler<UpdateSiteSettingsCommand>
{
    private readonly BlogDbContext _dbContext;

    /// <summary>
    /// 初始化命令处理器
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public UpdateSiteSettingsCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task Handle(UpdateSiteSettingsCommand command, CancellationToken cancellationToken)
    {
        var setting = await _dbContext.SiteSettings
            .FirstOrDefaultAsync(x => x.Id == SiteSetting.SingletonId, cancellationToken);

        if (setting == null)
        {
            _dbContext.SiteSettings.Add(new SiteSetting(command.ConfigJson));
        }
        else
        {
            setting.Update(command.ConfigJson);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            throw new BusinessException(
                "站点设置表不存在：请先执行升级脚本 scripts/sql/upgrade/001_site_settings.sql 后再保存。",
                500);
        }
    }
}
