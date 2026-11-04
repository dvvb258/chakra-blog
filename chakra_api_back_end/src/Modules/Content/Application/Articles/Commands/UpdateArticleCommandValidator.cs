using FluentValidation;

namespace Content.Application.Articles.Commands;

/// <summary>
/// 更新文章命令参数验证器
/// </summary>
public sealed class UpdateArticleCommandValidator : AbstractValidator<UpdateArticleCommand>
{
    /// <summary>
    /// 初始化更新文章验证规则
    /// </summary>
    public UpdateArticleCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("文章ID非法");

        RuleFor(x => x.Request)
            .NotNull().WithMessage("请求体不能为空");

        RuleFor(x => x.Request.Title)
            .NotEmpty().WithMessage("文章标题不能为空")
            .MaximumLength(200).WithMessage("文章标题长度不能超过200个字符");

        RuleFor(x => x.Request.Content)
            .NotEmpty().WithMessage("文章正文不能为空");

        RuleFor(x => x.Request.CategoryId)
            .GreaterThan(0).WithMessage("必须指定有效的文章分类");
    }
}
