using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using FluentValidation;
// FluentValidation also defines a ValidationException; alias the DOMAIN one so the
// service throws our own exception type (the API layer maps it to HTTP 400 in Phase 5).
using ValidationException = Domain.Exceptions.ValidationException;
using NotFoundException = Domain.Exceptions.NotFoundException;

namespace Application.Services;

/// <summary>
/// Orchestrates the Product use cases. Depends ONLY on Application interfaces
/// (repository, unit of work, validators) and the Domain — never on EF Core,
/// SQL Server, or ASP.NET Core. That is what keeps it unit-testable: in tests
/// every dependency below is a mock.
/// </summary>
public class ProductService : IProductService
{
    // Pagination guard rails: a sane default page size, and a hard upper bound so a
    // client can't request a single enormous page and exhaust server memory.
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateProductDto> _createValidator;
    private readonly IValidator<UpdateProductDto> _updateValidator;

    public ProductService(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateProductDto> createValidator,
        IValidator<UpdateProductDto> updateValidator)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>GET all — one page of products plus pagination metadata.</summary>
    public async Task<PagedResult<ProductDto>> GetProductsAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Defensive normalization so a caller can't ask for page 0 / negative size,
        // and can't request an unbounded page.
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var (items, totalCount) = await _productRepository.GetPagedAsync(page, pageSize, cancellationToken);

        return new PagedResult<ProductDto>
        {
            // List endpoint stays lean — products mapped without their items.
            Items = items.Select(p => p.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>GET by id — includes the product's items.</summary>
    public async Task<ProductDto> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        return product.ToDto(includeItems: true);
    }

    /// <summary>POST — validate, stamp create-audit fields, persist, return the created product.</summary>
    public async Task<ProductDto> CreateProductAsync(
        CreateProductDto dto, string createdBy, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, dto, cancellationToken);

        var product = dto.ToEntity();
        // Audit stamp. createdBy is supplied by the caller (the API layer passes the
        // authenticated user once Phase 6 lands). UtcNow is used directly for now —
        // see the note I left you about an optional IClock abstraction.
        product.CreatedBy = createdBy;
        product.CreatedOn = DateTime.UtcNow;

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // After SaveChanges the identity Id is populated on the tracked entity.
        return product.ToDto();
    }

    /// <summary>PUT — validate, load, apply changes, stamp modify-audit fields, persist.</summary>
    public async Task UpdateProductAsync(
        int id, UpdateProductDto dto, string modifiedBy, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, dto, cancellationToken);

        var product = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        dto.ApplyTo(product);
        product.ModifiedBy = modifiedBy;
        product.ModifiedOn = DateTime.UtcNow;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>DELETE — 404 if missing, otherwise remove and persist.</summary>
    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        _productRepository.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>GET related items for a product.</summary>
    public async Task<IReadOnlyList<ItemDto>> GetItemsForProductAsync(
        int productId, CancellationToken cancellationToken = default)
    {
        // Confirm the product exists first, so a missing product yields a 404 rather
        // than an indistinguishable empty list (a product with zero items also returns []).
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), productId);

        var items = await _productRepository.GetItemsForProductAsync(productId, cancellationToken);
        return items.Select(i => i.ToDto()).ToList();
    }

    /// <summary>
    /// Runs a FluentValidation validator and translates any failures into the
    /// domain's ValidationException, joining all messages. Centralized here so
    /// every write path validates identically and throws one exception type.
    /// </summary>
    private static async Task ValidateAsync<T>(
        IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            var message = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException(message);
        }
    }
}
