using FluentValidation;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 登录命令参数验证器
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// 初始化验证规则
    /// </summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.Account)
            .NotEmpty().WithMessage("登录账号不能为空")
            .MaximumLength(100).WithMessage("登录账号长度不能超过100个字符");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MaximumLength(100).WithMessage("密码长度不能超过100个字符");
    }
}
