using Application.DTOs;
using Application.Validators;

namespace Application.Tests;

/// <summary>
/// Direct tests of the FluentValidation rules. [Theory] + [InlineData] runs the same
/// test body for several inputs — handy for checking a rule's boundaries.
/// </summary>
public class ValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    [Theory]
    [InlineData("")]                 // empty -> NotEmpty fails
    [InlineData("   ")]              // whitespace -> NotEmpty fails
    public void CreateProduct_IsInvalid_WhenNameMissing(string name)
    {
        var result = _validator.Validate(new CreateProductDto { ProductName = name });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateProduct_IsInvalid_WhenNameExceeds255()
    {
        var result = _validator.Validate(new CreateProductDto { ProductName = new string('x', 256) });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateProduct_IsValid_ForReasonableName()
    {
        var result = _validator.Validate(new CreateProductDto { ProductName = "Widget" });
        Assert.True(result.IsValid);
    }
}
