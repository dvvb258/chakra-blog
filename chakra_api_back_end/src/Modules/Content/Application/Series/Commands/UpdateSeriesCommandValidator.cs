using FluentValidation;

namespace Content.Application.Series.Commands;

/// <summary>
/// 更新专栏系列命令参数验证器
/// </summary>
public sealed class UpdateSeriesCommandValidator : AbstractValidator<UpdateSeriesCommand>
{
    public UpdateSeriesCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("专栏ID非法");

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
