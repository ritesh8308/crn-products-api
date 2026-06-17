using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Application.Validators;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Tests;

/// <summary>
/// Unit tests for ProductService. The repository and unit of work are mocked (Moq),
/// so these test the SERVICE's logic — mapping, audit stamping, not-found handling,
/// pagination clamping — with no database involved. The real validators are used
/// (they're pure, dependency-free) to exercise the actual validation path.
/// </summary>
public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly ProductService _sut; // system under test

    public ProductServiceTests()
    {
        _sut = new ProductService(
            _repo.Object,
            _uow.Object,
            new CreateProductValidator(),
            new UpdateProductValidator());
    }

    [Fact]
    public async Task GetProductByIdAsync_ReturnsDto_WhenFound()
    {
        var product = new Product { Id = 7, ProductName = "Widget", CreatedBy = "admin" };
        _repo.Setup(r => r.GetByIdWithItemsAsync(7, It.IsAny<CancellationToken>()))
             .ReturnsAsync(product);

        var result = await _sut.GetProductByIdAsync(7);

        Assert.Equal(7, result.Id);
        Assert.Equal("Widget", result.ProductName);
    }

    [Fact]
    public async Task GetProductByIdAsync_Throws_WhenMissing()
    {
        _repo.Setup(r => r.GetByIdWithItemsAsync(99, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetProductByIdAsync(99));
    }

    [Fact]
    public async Task GetProductsAsync_ClampsExcessivePageSize_To100()
    {
        _repo.Setup(r => r.GetPagedAsync(1, 100, It.IsAny<CancellationToken>()))
             .ReturnsAsync((new List<Product>(), 0));

        var result = await _sut.GetProductsAsync(page: 1, pageSize: 9999);

        Assert.Equal(100, result.PageSize);
        // Verify the service actually asked the repo for the clamped size, not 9999.
        _repo.Verify(r => r.GetPagedAsync(1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_StampsAuditFields_AndPersists()
    {
        Product? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
             .Callback<Product, CancellationToken>((p, _) => captured = p)
             .Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        var dto = new CreateProductDto { ProductName = "New" };

        var result = await _sut.CreateProductAsync(dto, createdBy: "alice");

        Assert.NotNull(captured);
        Assert.Equal("alice", captured!.CreatedBy);
        Assert.InRange(captured.CreatedOn, before, DateTime.UtcNow);
        Assert.Equal("New", result.ProductName);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_Throws_WhenNameEmpty()
    {
        var dto = new CreateProductDto { ProductName = "" };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateProductAsync(dto, "alice"));
        // Nothing should have been persisted on a validation failure.
        _repo.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_Throws_WhenMissing()
    {
        _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateProductAsync(5, new UpdateProductDto { ProductName = "X" }, "bob"));
    }

    [Fact]
    public async Task UpdateProductAsync_StampsModified_AndPersists()
    {
        var product = new Product { Id = 5, ProductName = "Old", CreatedBy = "admin" };
        _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await _sut.UpdateProductAsync(5, new UpdateProductDto { ProductName = "Updated" }, "bob");

        Assert.Equal("Updated", product.ProductName);
        Assert.Equal("bob", product.ModifiedBy);
        Assert.NotNull(product.ModifiedOn);
        _repo.Verify(r => r.Update(product), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_Throws_WhenMissing()
    {
        _repo.Setup(r => r.GetByIdAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteProductAsync(8));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProductAsync_Removes_AndPersists()
    {
        var product = new Product { Id = 8 };
        _repo.Setup(r => r.GetByIdAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await _sut.DeleteProductAsync(8);

        _repo.Verify(r => r.Remove(product), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetItemsForProductAsync_Throws_WhenProductMissing()
    {
        _repo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetItemsForProductAsync(3));
    }

    [Fact]
    public async Task GetItemsForProductAsync_ReturnsMappedItems()
    {
        _repo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(new Product { Id = 3 });
        _repo.Setup(r => r.GetItemsForProductAsync(3, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<Item> { new() { Id = 1, ProductId = 3, Quantity = 10 } });

        var items = await _sut.GetItemsForProductAsync(3);

        Assert.Single(items);
        Assert.Equal(10, items[0].Quantity);
    }
}
