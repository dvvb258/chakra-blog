using Content.Application;
using Content.Application.Common;
using Content.Domain.Common;
using Content.Infrastructure.Data;
using Content.Infrastructure.Services;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using StackExchange.Redis;
using System.ClientModel;

namespace Content.Infrastructure;

/// <summary>
/// Content 模块依赖注入扩展
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 添加 Content 模块全部服务（Application + Infrastructure + Hangfire + AI）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddContentModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Application 层：MediatR handlers + FluentValidation validators
        services.AddContentApplication();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("未配置 ConnectionStrings:DefaultConnection 数据库连接字符串。");
        }

        // 注册EF Core（写操作），启用自动蛇形命名转换（PascalCase -> snake_case）
        services.AddDbContext<BlogDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention();
        });

        // HTTP 客户端
        services.AddHttpClient();

        // 注册 Dapper 连接工厂（读操作）
        services.AddSingleton<IContentDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));

        // 注册文章生成服务与定时任务
        services.AddScoped<IArticleGenerationService, ArticleGenerationService>();
        services.AddTransient<ArticleGenerationJob>();

        services.AddSingleton<ISystemStatsTracker, PostgresSystemStatsTracker>();

        var redisEnabled = configuration.GetValue<bool>("Redis:Enabled", false);
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (redisEnabled)
        {
            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new InvalidOperationException("开启 Redis 后必须配置 ConnectionStrings:Redis。");
            }

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false;
                var connection = ConnectionMultiplexer.Connect(options);
                if (!connection.IsConnected)
                {
                    sp.GetRequiredService<ILogger<ArticleGenerationJob>>()
                        .LogWarning("Redis 当前不可用，分布式 AI 任务将跳过执行；PostgreSQL 访问量统计不受影响。");
                }

                return connection;
            });
        }

        // Hangfire（内存存储）
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage());
        services.AddHangfireServer();

        // 存储模式切换（默认 Local 本地存储，支持 S3 兼容对象存储；配置层级统一规范并向下兼容）
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
        services.Configure<LocalStorageSettings>(configuration.GetSection(LocalStorageSettings.SectionName));
        services.Configure<S3StorageSettings>(options =>
        {
            var s3Section = configuration.GetSection("Storage:S3").Exists()
                ? configuration.GetSection("Storage:S3")
                : configuration.GetSection(S3StorageSettings.SectionName);
            s3Section.Bind(options);
        });

        var storageProvider = configuration["Storage:Provider"] ?? "Local";
        if (storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IStorageService, S3StorageService>();
        }
        else
        {
            services.AddScoped<IStorageService, LocalStorageService>();
        }

        // AI 配置（支持强类型绑定与环境变量逗号分隔的 RSS 源解析）
        var aiSettings = configuration.GetSection(AISettings.SectionName).Get<AISettings>() ?? new AISettings();
        var rssSourcesEnv = configuration["AI_RSS_SOURCES"] ?? configuration["AISettings:RssSourcesStr"];
        if (!string.IsNullOrWhiteSpace(rssSourcesEnv))
        {
            aiSettings.RssSources = rssSourcesEnv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        services.Configure<AISettings>(options =>
        {
            configuration.GetSection(AISettings.SectionName).Bind(options);
            if (!string.IsNullOrWhiteSpace(rssSourcesEnv))
            {
                options.RssSources = rssSourcesEnv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }
        });

        // 安全注册 OpenAI / 兼容端点客户端（防止未配置端点时抛出 UriFormatException 导致应用启动崩溃）
        var hasValidEndpoint = Uri.TryCreate(aiSettings?.Endpoint, UriKind.Absolute, out var endpointUri);
        var apiKey = string.IsNullOrWhiteSpace(aiSettings?.Key) ? "placeholder-key" : aiSettings.Key;
        var validUri = hasValidEndpoint ? endpointUri! : new Uri("https://api.openai.com/v1");

        services.AddSingleton(new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = validUri }));

        services.AddChatClient(sp =>
            sp.GetRequiredService<OpenAIClient>()
              .GetChatClient(aiSettings?.ModelName ?? "gpt-4o-mini")
              .AsIChatClient());

        return services;
    }

    /// <summary>
    /// 启用 Content 模块中间件（Hangfire 控制面板 + 定时任务）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseContentModule(this IApplicationBuilder app)
    {
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        if (configuration.GetValue<bool>("Hangfire:Dashboard:Enabled", true))
        {
            var username = configuration["Hangfire:Dashboard:Username"] ?? "admin";
            var password = configuration["Hangfire:Dashboard:Password"] ?? "AdminPassword123!";

            if (password == "AdminPassword123!")
            {
                var logger = app.ApplicationServices.GetRequiredService<ILogger<ArticleGenerationJob>>();
                logger.LogWarning("Hangfire 控制面板正使用默认口令运行，生产环境请务必通过 HANGFIRE_PASSWORD 环境变量进行替换以防止未授权访问！");
            }

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new HangfireCustomBasicAuthFilter(username, password)],
                DashboardTitle = "Chakra Background Jobs",
            });
        }

        // 仅当显式开启且配置有效时注册定时调度任务（默认 false 关闭）
        var autoGenEnabled = configuration.GetValue<bool>("AISettings:AutoGenerateEnabled", false);
        var aiConfigured = configuration.GetSection(AISettings.SectionName).Get<AISettings>()?.IsConfigured == true;
        var recurringJobs = app.ApplicationServices.GetRequiredService<IRecurringJobManager>();

        if (autoGenEnabled && !aiConfigured)
        {
            app.ApplicationServices.GetRequiredService<ILogger<ArticleGenerationJob>>()
                .LogWarning("AI 自动撰写未启动：请配置有效的 Key、Endpoint 和 ModelName；博客其他功能不受影响。");
        }

        if (autoGenEnabled && aiConfigured)
        {
            recurringJobs.AddOrUpdate<ArticleGenerationJob>(
                "article-generation-hourly",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Hourly(),
                new RecurringJobOptions());
        }
        else
        {
            recurringJobs.RemoveIfExists("article-generation-hourly");
        }

        return app;
    }
}
