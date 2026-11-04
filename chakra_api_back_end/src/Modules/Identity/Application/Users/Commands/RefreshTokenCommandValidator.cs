using FluentValidation;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 刷新令牌命令参数验证器
/// </summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>
    /// 初始化验证规则
    /// </summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("刷新令牌不能为空");
    }
}
