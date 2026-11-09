using FluentValidation;

namespace Content.Application.Categories.Commands;

/// <summary>
/// 创建文章分类命令参数验证器
/// </summary>
public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull().WithMessage("请求体不能为空");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("分类名称不能为空")
            .MaximumLength(50).WithMessage("分类名称长度不能超过50个字符");

        RuleFor(x => x.Request.Description)
            .MaximumLength(200).WithMessage("分类描述长度不能超过200个字符");

        RuleFor(x => x.Request.Icon)
            .MaximumLength(50).WithMessage("分类图标标识长度不能超过50个字符");
    }
}
