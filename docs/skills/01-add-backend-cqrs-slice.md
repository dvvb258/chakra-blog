# Skill: 后端垂直切片 (Vertical Slice CQRS) 新增功能指南

本技能指导 AI 编码 Agent 或研发人员在 `chakra_api_back_end` 中遵循**垂直切片架构 (Vertical Slice Architecture)** 与 **CQRS 读写分离规范** 扩展后端接口。

---

## 1. 架构核心守则 (Non-Negotiables)

1. **写操作 (Command)**：
   - 依赖 EF Core `DbContext` 操作领域实体；
   - 实体主键强制使用 `SnowflakeIdGenerator.NextId()` 生成 64 位雪花 ID；
   - 实体更新支持并发版本锁（`Version`），版本冲突抛出 `ConcurrencyException`（映射 HTTP 409）；
   - 敏感写操作添加 `[AuditLog(AuditModules.xxx, "操作名称")]` 无侵入审计；系统内部自动调用的无须审计操作标记 `[SkipAuditLog]`。
2. **读操作 (Query)**：
   - `Content` 模块：注入 `IContentDbConnectionFactory`，使用 **Dapper** 编写原生 SQL 直出 DTO，避免 EF Core 跟踪开销；
   - `Identity` 模块：注入 `IdentityDbContext`，使用 **EF Core `AsNoTracking()`** 查询只读 DTO。
3. **入参验证 (Validation)**：
   - 必须为每一个 Command 编写对应的 FluentValidation 校验器 `AbstractValidator<TCommand>`；
   - 由 MediatR 全局管道 `ValidationBehavior` 自动统一拦截并返回 HTTP 400 规范报文。
4. **统一响应格式**：
   - 控制器 Action 统一返回 `Success(data, message)`，输出 `ApiResult<T>`；分页数据使用 `PagedResult<T>`。

---

## 2. 标准落地步骤 (SOP)

### 步骤 1：声明请求 DTO
路径：`src/Modules/{Module}/Application/{SubModule}/DTOs/`
```csharp
namespace Content.Application.Articles.DTOs;

public sealed record CreateFeedbackRequest
{
    public string Content { get; init; } = string.Empty;
}
```

### 步骤 2：创建 Command / Query 与 Validator
路径：`src/Modules/{Module}/Application/{SubModule}/Commands/`
```csharp
using FluentValidation;
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;

namespace Content.Application.Articles.Commands;

[AuditLog(AuditModules.Article, "提交文章反馈")]
public sealed record CreateFeedbackCommand(
    long ArticleId,
    long UserId,
    string Content) : ICommand;

public sealed class CreateFeedbackCommandValidator : AbstractValidator<CreateFeedbackCommand>
{
    public CreateFeedbackCommandValidator()
    {
        RuleFor(x => x.ArticleId).GreaterThan(0).WithMessage("文章 ID 无效");
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("用户未登录");
        RuleFor(x => x.Content).NotEmpty().MaximumLength(500).WithMessage("反馈内容不能超过 500 字");
    }
}
```

### 步骤 3：编写 Handler 业务逻辑
路径：`src/Modules/{Module}/Application/{SubModule}/Commands/{Name}CommandHandler.cs`
```csharp
using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Articles.Commands;

public sealed class CreateFeedbackCommandHandler : ICommandHandler<CreateFeedbackCommand>
{
    private readonly BlogDbContext _dbContext;

    public CreateFeedbackCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(CreateFeedbackCommand command, CancellationToken cancellationToken)
    {
        var articleExists = await _dbContext.Articles
            .AnyAsync(a => a.Id == command.ArticleId && !a.IsDeleted, cancellationToken);

        if (!articleExists)
        {
            throw new NotFoundException("Article", command.ArticleId);
        }

        // 领域实体操作与持久化
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

### 步骤 4：在 Controller 暴露路由并标记权限
路径：`src/Modules/{Module}/Controllers/{Name}Controller.cs`
```csharp
[HttpPost("{id:long}/feedback")]
[ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
public async Task<IActionResult> CreateFeedbackAsync(long id, [FromBody] CreateFeedbackRequest request, CancellationToken cancellationToken)
{
    var command = new CreateFeedbackCommand(id, _currentUser.UserId, request.Content);
    await _mediator.Send(command, cancellationToken);
    return Success("反馈已成功提交");
}
```

### 步骤 5：补充单元测试
路径：`tests/Chakra.Tests.Unit/{Feature}Tests.cs`
- 运行 `dotnet test chakra_api_back_end/Chakra.slnx` 确保全部通过；
- 确保 `dotnet build chakra_api_back_end/Chakra.slnx` 保持 0 警告 0 错误（开启了 `TreatWarningsAsErrors`）。
