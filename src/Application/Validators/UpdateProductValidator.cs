using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Declarative validation rules for the PUT body (UpdateProductDto).
/// Same constraints as create — ProductName is the only mutable business field,
/// and it must satisfy the nvarchar(255) NOT NULL column. Kept as a separate
/// validator (rather than reusing Create's) so the two DTOs can diverge later
/// without coupling their rules.
/// </summary>
public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("ProductName is required.")
            .MaximumLength(255).WithMessage("ProductName must not exceed 255 characters.");
    }
}
