using ApiHost.Authorization;
using ApiHost.JsonConverters;
using ApiHost.Middleware;
using Content.Controllers;
using Content.Infrastructure;
using Dapper;
using Identity.Application.Common.Interfaces;
using Identity.Controllers;
using Identity.Infrastructure;
using Logging.Controllers;
using Logging.Infrastructure;
using Logging.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SharedKernel.Api;
using SharedKernel.Application;
using SharedKernel.Application.Behaviors;
using SharedKernel.Common;
using SharedKernel.Domain;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace ApiHost;

/// <summary>
/// 程序入口
/// </summary>
public class Program
{
    /// <summary>
    /// 主入口
    /// </summary>
    /// <param name="args">命令行参数</param>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 配置 Serilog 结构化日志，并通过 SerilogAuditSink 将 Warning / Error 级日志推入异步内存通道
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .WriteTo.Console()
                .WriteTo.Sink(new SerilogAuditSink(services.GetRequiredService<IAuditLogQueue>()));
        });

        // 添加控制器（JSON 序列化 + 模块 ApplicationPart）
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new BeijingDateTimeConverter());
                options.JsonSerializerOptions.Converters.Add(new BeijingNullableDateTimeConverter());
                options.JsonSerializerOptions.Converters.Add(new LongToStringJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new NullableLongToStringJsonConverter());
            })
            .AddApplicationPart(typeof(ArticlesController).Assembly)
            .AddApplicationPart(typeof(AuthController).Assembly)
            .AddApplicationPart(typeof(AdminLogsController).Assembly);

        // 全局路由小写标准化
        builder.Services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = false;
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
            {
                Title = "Chakra Blog WebAPI",
                Version = "v1.0.0",
                Description = "Chakra 现代化昼夜双生全栈个人技术博客系统后端服务 (.NET 10)"
            });
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Description = "JWT Bearer Token（格式：Bearer {token}）",
                Name = "Authorization",
                In = Microsoft.OpenApi.ParameterLocation.Header,
                Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            });
            c.AddSecurityRequirement(doc => new Microsoft.OpenApi.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                },
            });
        });

        // 全局初始化雪花ID（所有模块共用，必须在模块注册之前）
        var workerIdStr = builder.Configuration.GetSection("SnowflakeId:WorkerId").Value;
        var workerId = string.IsNullOrEmpty(workerIdStr) ? (ushort)1 : ushort.Parse(workerIdStr);
        SnowflakeIdGenerator.Initialize(workerId);

        // Dapper: 自动将 snake_case 列名映射到 PascalCase 属性（created_at → CreatedAt）
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        // HttpContextAccessor + ICurrentUser（JWT Claims 读取当前用户）
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();

        // 统一注册 MediatR 与 FluentValidation 全局验证管道（仅注册一次，扫描全部业务模块）
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(Content.Application.DependencyInjection).Assembly,
                typeof(Identity.Application.DependencyInjection).Assembly,
                typeof(Logging.Application.Logs.Queries.GetAuditLogsQuery).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuditLogBehavior<,>));
        });

        // JWT 身份认证配置
        var jwtSecret = builder.Configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("未配置 JwtSettings:Secret");
        if (ConfigurationSecrets.IsPlaceholder(jwtSecret))
        {
            Log.Warning("⚠️ 安全提示：当前正使用默认或示例 JWT 密钥。生产环境部署请务必在环境变量中配置强随机密钥！");
        }

        var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "chakra_api";
        var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "chakra_client";

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero,
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
                    var validator = context.HttpContext.RequestServices.GetRequiredService<IUserAccessValidator>();

                    if (!long.TryParse(userIdValue, out var userId) ||
                        string.IsNullOrWhiteSpace(role) ||
                        !await validator.IsAccessAllowedAsync(userId, role, context.HttpContext.RequestAborted))
                    {
                        context.Fail("用户已停用、删除或角色已变更");
                    }
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(context.Exception, "JWT 鉴权失败: {Message}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnChallenge = async context =>
                {
                    // 阻止默认空响应，统一输出 ApiResult 规范契约
                    context.HandleResponse();

                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT Challenge 401: Error={Error}, Description={Description}", context.Error, context.ErrorDescription);

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var result = ApiResult<object>.Fail("未授权，请先登录或令牌已失效", StatusCodes.Status401Unauthorized);
                    var jsonOptions = context.HttpContext.RequestServices
                        .GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()
                        .Value.JsonSerializerOptions;
                    var json = JsonSerializer.Serialize(result, jsonOptions);

                    await context.Response.WriteAsync(json);
                },
                OnForbidden = async context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT Forbidden 403: 用户无权访问该资源");

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var result = ApiResult<object>.Fail("权限不足，禁止访问该资源", StatusCodes.Status403Forbidden);
                    var jsonOptions = context.HttpContext.RequestServices
                        .GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()
                        .Value.JsonSerializerOptions;
                    var json = JsonSerializer.Serialize(result, jsonOptions);

                    await context.Response.WriteAsync(json);
                }
            };
        });

        // 添加 Content 模块服务（Application + Infrastructure + Hangfire + AI）
        builder.Services.AddContentModule(builder.Configuration);

        // 添加 Identity 模块服务（Application + Infrastructure + DbContext + Hasher）
        builder.Services.AddIdentityModule(builder.Configuration);

        // 添加 Logging 独立模块服务（DbContext + 异步通道队列 + 后台批量落盘 Worker）
        builder.Services.AddLoggingModule(builder.Configuration);

        // 注册基于权限代码的动态策略授权组件（RBAC 闭环）
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        builder.Services.AddCors(options =>
        {
            var allowedOriginsConfig = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            var originsSet = new HashSet<string>(allowedOriginsConfig, StringComparer.OrdinalIgnoreCase);

            // 支持逗号分隔的环境变量配置（如 CORS_ALLOWED_ORIGINS=https://a.com,https://b.com）
            var rawOriginsEnv = builder.Configuration["Cors:AllowedOriginsStr"] ?? builder.Configuration["CORS_ALLOWED_ORIGINS"];
            if (!string.IsNullOrWhiteSpace(rawOriginsEnv))
            {
                foreach (var origin in rawOriginsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    originsSet.Add(origin);
                }
            }

            // 自动将配置的展示端主域名加入跨域白名单
            var siteUrl = builder.Configuration["NEXT_PUBLIC_SITE_URL"] ?? builder.Configuration["SiteUrl"];
            if (!string.IsNullOrWhiteSpace(siteUrl) && Uri.TryCreate(siteUrl, UriKind.Absolute, out var siteUri))
            {
                originsSet.Add(siteUri.GetLeftPart(UriPartial.Authority));
            }

            if (originsSet.Count == 0)
            {
                // 开源友好默认白名单：容纳展示端 (3000) 与管理端 (3101) 的本地开发环境
                originsSet.Add("http://localhost:3000");
                originsSet.Add("http://localhost:3101");
                originsSet.Add("http://127.0.0.1:3000");
                originsSet.Add("http://127.0.0.1:3101");
            }

            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins([.. originsSet])
                      .AllowCredentials()
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("Authorization", "X-Trace-Id");
            });
        });

        // 统一接口速率限制中间件（防暴力破解与统计刷量）
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 登录防爆破策略：每个 IP 每分钟最多 10 次
            options.AddPolicy("auth-login", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            // 统计防刷量策略：每个 IP 每分钟最多 60 次
            options.AddPolicy("stats-visit", httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                    }));
        });
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 2;
            foreach (var address in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
            {
                options.KnownProxies.Add(IPAddress.Parse(address));
            }

            foreach (var network in builder.Configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        await using var app = builder.Build();

        // 自动执行增量数据库迁移（如果存在未应用的 SQL 升级脚本）
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var connectionString = app.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            await ApiHost.Infrastructure.DatabaseMigrator.MigrateAsync(connectionString, logger);
        }

        app.UseForwardedHeaders();
        app.UseCors();
        app.UseRateLimiter();

        // 挂载本地媒体文件静态目录（支持 Local 本地文件存储开箱即用）
        var uploadDir = builder.Configuration["Storage:Local:UploadDir"] ?? "uploads";
        var physicalUploadsPath = Path.IsPathRooted(uploadDir)
            ? uploadDir
            : Path.Combine(app.Environment.ContentRootPath, uploadDir);

        if (!Directory.Exists(physicalUploadsPath))
        {
            Directory.CreateDirectory(physicalUploadsPath);
        }

        var requestPath = builder.Configuration["Storage:Local:RequestPath"] ?? "/uploads";
        if (!requestPath.StartsWith('/')) requestPath = "/" + requestPath;

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(physicalUploadsPath),
            RequestPath = new PathString(requestPath),
        });

        // 全链路请求追踪与 API 慢请求监控中间件（注入 TraceId 并监测全站请求耗时）
        app.UseMiddleware<TraceContextMiddleware>();

        // 全局异常处理（紧随 TraceId 之后，确保捕获异常时可关联 TraceId 并输出统一契约）
        app.UseGlobalExceptionHandler();

        // 启用 Content 模块中间件（Hangfire 控制面板 + 定时任务）
        app.UseContentModule();

        // 配置 HTTP 请求管线（开发环境或显式开启 Swagger:Enabled 时启用）
        var swaggerEnabled = app.Environment.IsDevelopment() ||
            app.Configuration.GetValue<bool>("Swagger:Enabled", false);
        if (swaggerEnabled)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chakra WebAPI v1.0.0");
                c.DocumentTitle = "Chakra Blog WebAPI Documentation";
            });
        }

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        // 轻量健康检查探针（供 Docker / K8s / 反代探针使用）
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "Healthy",
            service = "Chakra.WebAPI",
            timestamp = DateTime.UtcNow,
        })).AllowAnonymous();

        // 打印 Chakra 极客启动横幅
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
   ______   __               __                  ____  __            
  / ____/  / /_  ____ _     / /__  _________ _  / __ )/ /___  ____ _ 
 / /      / __ \/ __ `/    / //_/ / ___/ __ `/ / __  / / __ \/ __ `/ 
/ /___   / / / / /_/ /    / ,<   / /  / /_/ / / /_/ / / /_/ / /_/ /  
\____/  /_/ /_/\__,_/____/_/|_| /_/   \__,_/ /_____/_/\____/\__, /   
                   /_____/                                 /____/    
 :: Chakra Blog WebAPI ::                     (v1.0.0 - .NET 10)
");
        Console.ResetColor();

        await app.RunAsync();
    }
}
