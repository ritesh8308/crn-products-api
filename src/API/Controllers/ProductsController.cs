using Application.DTOs;
using Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// HTTP entry point for the Product feature. Deliberately THIN: it binds the request,
/// calls a single IProductService method, and shapes the HTTP result. It holds no
/// business logic and never touches the database or domain entities directly.
/// Validation and not-found handling live in the service (which throws domain
/// exceptions); the exception-handling middleware turns those into status codes,
/// so these actions don't need try/catch.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService) => _productService = productService;

    /// <summary>
    /// The audit "user" stamped on writes. Until JWT auth lands in Phase 6,
    /// User.Identity.Name is null, so we fall back to "system". Once authentication
    /// is wired, the authenticated user's name flows in here with no controller change.
    /// </summary>
    private string CurrentUser => User.Identity?.Name ?? "system";

    /// <summary>GET all products, paginated.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetProductsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET a single product (including its items) by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductById(int id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);
        return Ok(product);
    }

    /// <summary>GET the items belonging to a product.</summary>
    [HttpGet("{id:int}/items")]
    [ProducesResponseType(typeof(IReadOnlyList<ItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ItemDto>>> GetItemsForProduct(int id, CancellationToken cancellationToken)
    {
        var items = await _productService.GetItemsForProductAsync(id, cancellationToken);
        return Ok(items);
    }

    /// <summary>POST a new product. Returns 201 with a Location header pointing at the new resource.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] CreateProductDto dto, CancellationToken cancellationToken)
    {
        var created = await _productService.CreateProductAsync(dto, CurrentUser, cancellationToken);

        // 201 Created with the resource as the body and a Location header pointing at it.
        // The link generator emits the version in its canonical form, /api/v1.0/products/{id};
        // that URL resolves fine — the apiVersion route constraint treats v1 and v1.0 as equal.
        return CreatedAtAction(nameof(GetProductById), new { id = created.Id, version = "1.0" }, created);
    }

    /// <summary>PUT (update) an existing product. Returns 204 on success.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(
        int id, [FromBody] UpdateProductDto dto, CancellationToken cancellationToken)
    {
        await _productService.UpdateProductAsync(id, dto, CurrentUser, cancellationToken);
        return NoContent();
    }

    /// <summary>DELETE a product. Returns 204 on success.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        await _productService.DeleteProductAsync(id, cancellationToken);
        return NoContent();
    }
}
