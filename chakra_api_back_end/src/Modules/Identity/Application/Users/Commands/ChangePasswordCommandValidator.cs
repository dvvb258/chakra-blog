using FluentValidation;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 修改密码命令参数验证器
/// </summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("用户ID非法");

        RuleFor(x => x.Request)
            .NotNull().WithMessage("请求体不能为空");

        RuleFor(x => x.Request.NewPassword)
            .NotEmpty().WithMessage("新密码不能为空")
            .MinimumLength(6).WithMessage("新密码长度不能少于6位")
            .MaximumLength(100).WithMessage("新密码长度不能超过100位");
    }
}
