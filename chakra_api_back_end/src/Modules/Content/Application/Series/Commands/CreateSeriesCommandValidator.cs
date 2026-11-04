using FluentValidation;

namespace Content.Application.Series.Commands;

/// <summary>
/// 创建专栏系列命令参数验证器
/// </summary>
public sealed class CreateSeriesCommandValidator : AbstractValidator<CreateSeriesCommand>
{
    public CreateSeriesCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull().WithMessage("请求体不能为空");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("专栏名称不能为空")
            .MaximumLength(100).WithMessage("专栏名称长度不能超过100个字符");

        RuleFor(x => x.Request.Description)
            .MaximumLength(500).WithMessage("专栏描述长度不能超过500个字符");

        RuleFor(x => x.Request.Theme)
            .MaximumLength(50).WithMessage("主题色彩长度不能超过50个字符");
    }
}
