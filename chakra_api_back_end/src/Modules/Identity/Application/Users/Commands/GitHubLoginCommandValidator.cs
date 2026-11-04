using FluentValidation;

namespace Identity.Application.Users.Commands;

/// <summary>
/// GitHub 登录命令参数验证器
/// </summary>
public sealed class GitHubLoginCommandValidator : AbstractValidator<GitHubLoginCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubLoginCommandValidator"/> class.
    /// </summary>
    public GitHubLoginCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("GitHub 授权码（Code）不能为空")
            .MaximumLength(200).WithMessage("GitHub 授权码格式不合法");
    }
}
