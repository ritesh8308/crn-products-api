using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Declarative validation rules for the POST body (CreateProductDto).
/// Rules mirror the database schema exactly: ProductName is nvarchar(255) NOT NULL,
/// so it must be present and at most 255 characters. Catching this here means a
/// bad request never reaches EF Core / SQL Server to fail there.
/// </summary>
public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("ProductName is required.")
            .MaximumLength(255).WithMessage("ProductName must not exceed 255 characters.");
    }
}
