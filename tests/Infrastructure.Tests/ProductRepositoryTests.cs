using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests;

/// <summary>
/// Tests ProductRepository against EF Core's in-memory provider — fast, no SQL Server.
/// Each test gets a uniquely-named database so they don't bleed into each other.
/// Verifies the queries the service relies on: paging math and eager-loading items.
/// </summary>
public class ProductRepositoryTests
{
    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task GetPagedAsync_ReturnsRequestedSlice_AndTotalCount()
    {
        using var context = NewContext();
        for (var i = 1; i <= 25; i++)
            context.Products.Add(new Product { ProductName = $"P{i}", CreatedBy = "seed" });
        await context.SaveChangesAsync();

        var repo = new ProductRepository(context);
        var (items, total) = await repo.GetPagedAsync(page: 2, pageSize: 10);

        Assert.Equal(25, total);       // total ignores paging
        Assert.Equal(10, items.Count); // second page is full
    }

    [Fact]
    public async Task GetByIdWithItemsAsync_EagerLoadsItems()
    {
        using var context = NewContext();
        var product = new Product { ProductName = "WithItems", CreatedBy = "seed" };
        product.Items.Add(new Item { Quantity = 5 });
        product.Items.Add(new Item { Quantity = 9 });
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var repo = new ProductRepository(context);
        var loaded = await repo.GetByIdWithItemsAsync(product.Id);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Items.Count);
    }

    [Fact]
    public async Task GetItemsForProductAsync_ReturnsOnlyThatProductsItems()
    {
        using var context = NewContext();
        var a = new Product { ProductName = "A", CreatedBy = "seed" };
        a.Items.Add(new Item { Quantity = 1 });
        var b = new Product { ProductName = "B", CreatedBy = "seed" };
        b.Items.Add(new Item { Quantity = 2 });
        b.Items.Add(new Item { Quantity = 3 });
        context.Products.AddRange(a, b);
        await context.SaveChangesAsync();

        var repo = new ProductRepository(context);
        var items = await repo.GetItemsForProductAsync(b.Id);

        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Equal(b.Id, i.ProductId));
    }
}
