using FluentValidation;

namespace Identity.Application.Users.Commands;

/// <summary>
/// 更新个人资料命令参数验证器
/// </summary>
public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("用户ID非法");

        RuleFor(x => x.Request)
            .NotNull().WithMessage("请求体不能为空");

        RuleFor(x => x.Request.Nickname)
            .NotEmpty().WithMessage("昵称不能为空")
            .MaximumLength(50).WithMessage("昵称长度不能超过50个字符");

        RuleFor(x => x.Request.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Request.Email))
            .WithMessage("电子邮箱格式不正确")
            .MaximumLength(100).WithMessage("邮箱长度不能超过100个字符");

        RuleFor(x => x.Request.AvatarUrl)
            .MaximumLength(500).WithMessage("头像链接长度不能超过500个字符");
    }
}
