# Brick-by-Brick .NET 8 Web API Learning Guide
## A Phased Implementation Plan for Freshers

Welcome to the **CRN Products API** project! If you are a fresher or a complete beginner to web development, enterprise C#, or .NET 8, this guide is written specifically for you.

Instead of throwing a massive, finished codebase at you, we are going to break it down into **10 progressive phases (0 to 9)**. For each phase, we will explain:
1. **The Concepts:** What are we doing, and *why* are we doing it? (No jargon unexplained!)
2. **The Step-by-Step Commands:** The exact commands to run in your terminal.
3. **The Core Files to Create:** What the code does, explained line-by-line.
4. **The Definition of Done (Gate):** How to verify that your code works before moving to the next phase.

---

## The Architecture at a Glance (Clean Architecture)

Before writing any code, look at how this project is organized. We use **Clean Architecture**, which divides our application into 4 distinct layers:

```
        ┌────────────────────────────────────────────────────────┐
        │                        API Layer                       │
        │           (Controllers, Middleware, Swagger)           │
        └───────────────────────────┬────────────────────────────┘
                                    │ (depends on)
        ┌───────────────────────────▼────────────────────────────┐
        │                 Infrastructure Layer                   │
        │           (EF Core, SQL Server, Auth, JWT)             │
        └───────────────────────────┬────────────────────────────┘
                                    │ (depends on)
        ┌───────────────────────────▼────────────────────────────┐
        │                  Application Layer                     │
        │        (Services, Interfaces, DTOs, Validation)        │
        └───────────────────────────┬────────────────────────────┘
                                    │ (depends on)
        ┌───────────────────────────▼────────────────────────────┐
        │                     Domain Layer                       │
        │               (Entities, Exceptions)                   │
        └────────────────────────────────────────────────────────┘
```

### The Golden Rule: The Dependency Flow
**Dependencies only point inward.**
* The **Domain Layer** is the core. It depends on **nothing**. It has zero knowledge of database tools, web requests, or libraries.
* The **Application Layer** only depends on the **Domain Layer**.
* The **Infrastructure** and **API Layers** sit on the outside and depend on both **Application** and **Domain**. They never depend on each other, and the inner layers *never* depend on them.

**Why do we do this?** 
Imagine you want to switch your database from SQL Server to PostgreSQL, or replace your ASP.NET Core web framework with a console application. In Clean Architecture, you only have to change the outer layer (Infrastructure or API). The entire core of your business logic (Domain and Application) remains completely untouched!

---

## Phase 0: The Solution Foundation

### 1. Concepts
* **Solution (.sln):** A container file used by .NET to organize multiple related projects.
* **Project (.csproj):** A buildable unit of code (like a Class Library or Web API project).
* **Project Reference:** Tells .NET that Project A is allowed to use classes defined inside Project B.

### 2. Step-by-Step Commands
Run these commands in your terminal to scaffold the entire project structure from scratch:

```bash
# 1. Create a parent folder and initialize the solution file
mkdir crn-products-api && cd crn-products-api
dotnet new sln -n CrnProducts

# 2. Create the source projects (src/)
mkdir src
dotnet new classlib -o src/Domain       # Domain: Pure C# class library
dotnet new classlib -o src/Application  # Application: Pure C# class library
dotnet new classlib -o src/Infrastructure # Infrastructure: Class library for external services
dotnet new webapi -o src/API            # API: ASP.NET Core Web API template

# 3. Create the test projects (tests/)
mkdir tests
dotnet new xunit -o tests/Infrastructure.Tests
dotnet new xunit -o tests/Application.Tests
dotnet new xunit -o tests/API.Tests

# 4. Add all projects to the solution container
dotnet sln add src/Domain/Domain.csproj
dotnet sln add src/Application/Application.csproj
dotnet sln add src/Infrastructure/Infrastructure.csproj
dotnet sln add src/API/API.csproj
dotnet sln add tests/Infrastructure.Tests/Infrastructure.Tests.csproj
dotnet sln add tests/Application.Tests/Application.Tests.csproj
dotnet sln add tests/API.Tests/API.Tests.csproj

# 5. Wire up the project dependencies (Following the Inward Dependency Rule)
# Application depends only on Domain
dotnet add src/Application/Application.csproj reference src/Domain/Domain.csproj

# Infrastructure depends on Application (and transitively Domain)
dotnet add src/Infrastructure/Infrastructure.csproj reference src/Application/Application.csproj

# API depends on Infrastructure and Application
dotnet add src/API/API.csproj reference src/Application/Application.csproj
dotnet add src/API/API.csproj reference src/Infrastructure/Infrastructure.csproj

# Wire up the Test projects to the code they are testing
dotnet add tests/Domain.Tests/Domain.Tests.csproj reference src/Domain/Domain.csproj 2>/dev/null || true # Optional
dotnet add tests/Infrastructure.Tests/Infrastructure.Tests.csproj reference src/Infrastructure/Infrastructure.csproj
dotnet add tests/Application.Tests/Application.Tests.csproj reference src/Application/Application.csproj
dotnet add tests/API.Tests/API.Tests.csproj reference src/API/API.csproj
```

### 3. Verification Gate
In the root folder, run:
```bash
dotnet build
```
**Definition of Done:** The build completes successfully with `0 Warning(s)` and `0 Error(s)`.

---

## Phase 1: The Core (Domain Layer)

### 1. Concepts
* **Entities:** Core models representing data stored in our database. They represent real-world concepts (like a "Product" or an "Item").
* **Custom Exceptions:** Domain-specific error classes representing business violations (e.g., trying to fetch something that does not exist).
* **Anemic vs. Rich Domain Models:** In this project, we write lightweight entities with getter/setter properties to map directly to our schema, keeping code easy for beginners to read.

### 2. Files to Create
Create these files inside the `src/Domain` project:

#### A. The Product Entity
*File: `src/Domain/Entities/Product.cs`*
```csharp
namespace Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    
    // Audit fields: tracking who created/modified this product and when.
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }

    // Navigation property: One product has many items.
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
```

#### B. The Item Entity
*File: `src/Domain/Entities/Item.cs`*
```csharp
namespace Domain.Entities;

public class Item
{
    public int Id { get; set; }
    public int ProductId { get; set; } // Foreign Key back to Product
    public int Quantity { get; set; }

    // Navigation property back to the owning product.
    public Product? Product { get; set; }
}
```

#### C. Custom Exceptions
*File: `src/Domain/Exceptions/NotFoundException.cs`*
```csharp
namespace Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' ({key}) was not found.")
    {
    }
}
```

*File: `src/Domain/Exceptions/ValidationException.cs`*
```csharp
namespace Domain.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
```

### 3. Verification Gate
Run:
```bash
dotnet build src/Domain
```
**Definition of Done:** The Domain project compiles by itself with no errors or package dependencies.

---

## Phase 2: Contracts & DTOs (Application Layer)

### 1. Concepts
* **DTO (Data Transfer Object):** A simple class containing data sent to or received from our API endpoints. **Never expose database entities directly to the client!** If you do, a user could modify hidden fields like `CreatedBy` (called an Over-posting attack), or changes to your database schema would immediately break your API client.
* **Interfaces:** Code contracts defining *what* actions are supported (e.g., `SaveProduct()`), leaving *how* it's done (e.g., SQL Server or In-Memory) to the Infrastructure layer. This is called **Dependency Inversion**.
* **Mapping:** The act of copying data from a DTO into an Entity or vice-versa. We write these mappings manually as C# extension methods instead of using libraries like AutoMapper. It is fast, explicit, and easy to debug.

### 2. Files to Create
Create these files inside the `src/Application` project:

#### A. Product and Item DTOs
*File: `src/Application/DTOs/ProductDto.cs`*
```csharp
namespace Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public List<ItemDto> Items { get; set; } = new();
}
```

*File: `src/Application/DTOs/CreateProductDto.cs`*
```csharp
namespace Application.DTOs;

public class CreateProductDto
{
    public string ProductName { get; set; } = string.Empty;
}
```

#### B. The Repository Interface
A Repository abstracts away database-specific operations.
*File: `src/Application/Interfaces/IRepository.cs`*
```csharp
namespace Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
```

*File: `src/Application/Interfaces/IProductRepository.cs`*
```csharp
using Domain.Entities;

namespace Application.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Item>> GetItemsForProductAsync(
        int productId, CancellationToken cancellationToken = default);
}
```

#### C. Manual Mapper Extensions
*File: `src/Application/Mapping/ProductMappingExtensions.cs`*
```csharp
using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class ProductMappingExtensions
{
    public static ItemDto ToDto(this Item item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        Quantity = item.Quantity
    };

    public static ProductDto ToDto(this Product product, bool includeItems = false) => new()
    {
        Id = product.Id,
        ProductName = product.ProductName,
        CreatedBy = product.CreatedBy,
        CreatedOn = product.CreatedOn,
        ModifiedBy = product.ModifiedBy,
        ModifiedOn = product.ModifiedOn,
        Items = includeItems 
            ? product.Items.Select(i => i.ToDto()).ToList() 
            : new List<ItemDto>()
    };

    public static Product ToEntity(this CreateProductDto dto) => new()
    {
        ProductName = dto.ProductName
    };

    public static void ApplyTo(this UpdateProductDto dto, Product product)
    {
        product.ProductName = dto.ProductName;
    }
}
```

### 3. Verification Gate
Run:
```bash
dotnet build src/Application
```
**Definition of Done:** The Application layer compiles successfully, referencing only the Domain layer.

---

## Phase 3: Persistence (Infrastructure Layer)

### 1. Concepts
* **ORM (Object-Relational Mapper):** A tool that acts as a bridge between C# code and database tables. **Entity Framework Core (EF Core)** is the official ORM for .NET.
* **DbContext:** The primary class in EF Core representing a session with the database, used to query and save instances of your entities.
* **Fluent API:** A way of configuring EF Core models using method chaining in code, preferred over Data Annotations because it keeps database configurations completely separate from your Domain entities.
* **Migrations:** Code files generated by EF Core that describe how to build and update database tables to match your C# models.
* **Unit of Work (UoW):** A design pattern that group one or more operations (like inserting a Product and updating an Item) into a single transaction so that either all succeed, or all fail (maintaining data integrity).

### 2. Step-by-Step Commands
Install the EF Core tool and packages in your terminal:

```bash
# Install EF Core tool globally if you haven't already
dotnet tool install --global dotnet-ef || dotnet tool update --global dotnet-ef

# Add NuGet packages to Infrastructure project
cd src/Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
cd ../..
```

### 3. Files to Create
Create these files inside the `src/Infrastructure` project:

#### A. The DbContext
*File: `src/Infrastructure/Data/ApplicationDbContext.cs`*
```csharp
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Item> Items => Set<Item>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Tells EF Core to search for and apply configurations we define below
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

#### B. Fluent API Configurations
*File: `src/Infrastructure/Data/Configurations/ProductConfiguration.cs`*
```csharp
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        // One-to-Many configuration (A product owns many items, deleting a product deletes its items)
        builder.HasMany(p => p.Items)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### C. Generic and Product Repositories
*File: `src/Infrastructure/Data/Repositories/Repository.cs`*
```csharp
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // AsNoTracking() tells EF Core to skip tracking changes on these entities.
        // It makes read-only operations significantly faster.
        return await DbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(T entity) => DbSet.Update(entity);
    public void Remove(T entity) => DbSet.Remove(entity);
}
```

*File: `src/Infrastructure/Data/Repositories/ProductRepository.cs`*
```csharp
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = Context.Products.AsNoTracking().OrderBy(p => p.Id);
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Product?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Products
            .AsNoTracking()
            .Include(p => p.Items) // Eager loading related items
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Item>> GetItemsForProductAsync(
        int productId, CancellationToken cancellationToken = default)
    {
        return await Context.Items
            .AsNoTracking()
            .Where(i => i.ProductId == productId)
            .ToListAsync(cancellationToken);
    }
}
```

#### D. The Unit of Work implementation
*File: `src/Infrastructure/Data/UnitOfWork.cs`*
```csharp
using Application.Interfaces;

namespace Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Executes all pending updates/inserts as a single database transaction.
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### 4. Creating and Applying Migrations
Run these commands in the **root folder** of the project:
```bash
# Add a migration called InitialCreate
dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/API
```

### 5. Verification Gate
Ensure you have Docker running locally or a working SQL Server connection.
Run:
```bash
# Apply migrations to your database
dotnet ef database update --project src/Infrastructure --startup-project src/API
```
**Definition of Done:** The migration compiles, generates files inside `src/Infrastructure/Migrations/`, and successfully updates the database schema.

---

## Phase 4: Business Logic & Validation (Application Layer)

### 1. Concepts
* **Application Services:** The "brain" of our app. This is where we write the actual business rules, coordinate mappings, validate input, and save data.
* **FluentValidation:** A library used to define validation rules cleanly outside of our models using a fluent interface.

### 2. Step-by-Step Commands
Install FluentValidation inside the Application project:
```bash
cd src/Application
dotnet add package FluentValidation
cd ../..
```

### 3. Files to Create
Create these files inside the `src/Application` project:

#### A. Validators
*File: `src/Application/Validators/CreateProductValidator.cs`*
```csharp
using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product Name is required.")
            .MaximumLength(150).WithMessage("Product Name must not exceed 150 characters.");
    }
}
```

#### B. The Product Service
*File: `src/Application/Services/ProductService.cs`*
```csharp
using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using FluentValidation;
using ValidationException = Domain.Exceptions.ValidationException;
using NotFoundException = Domain.Exceptions.NotFoundException;

namespace Application.Services;

public class ProductService : IProductService
{
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

    public async Task<PagedResult<ProductDto>> GetProductsAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var (items, totalCount) = await _productRepository.GetPagedAsync(page, pageSize, cancellationToken);

        return new PagedResult<ProductDto>
        {
            Items = items.Select(p => p.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductDto> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        return product.ToDto(includeItems: true);
    }

    public async Task<ProductDto> CreateProductAsync(
        CreateProductDto dto, string createdBy, CancellationToken cancellationToken = default)
    {
        // Run validation rules
        await ValidateAsync(_createValidator, dto, cancellationToken);

        var product = dto.ToEntity();
        product.CreatedBy = createdBy;
        product.CreatedOn = DateTime.UtcNow;

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }

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

    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        _productRepository.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ItemDto>> GetItemsForProductAsync(
        int productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), productId);

        var items = await _productRepository.GetItemsForProductAsync(productId, cancellationToken);
        return items.Select(i => i.ToDto()).ToList();
    }

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
```

### 4. Verification Gate
Run:
```bash
dotnet build
```
**Definition of Done:** The code compiles cleanly. There are no logic leaks: API endpoints are not yet written, but business logic compiles successfully.

---

## Phase 5: The Interface (API Layer & CRUD)

### 1. Concepts
* **Controllers:** Classes that handle incoming HTTP requests (like GET, POST, DELETE) and return HTTP responses.
* **Middleware:** A block of code in the ASP.NET Core request pipeline that executes on every request/response cycle (e.g., catching exceptions globally so they return clean JSON, or adding custom headers).
* **Swagger/OpenAPI:** A tool that automatically documents your API endpoints and provides an interactive webpage to test them.

### 2. Files to Create
Create these files inside the `src/API` project:

#### A. Global Error Handling Middleware
*File: `src/API/Middleware/ExceptionHandlingMiddleware.cs`*
```csharp
using System.Text.Json;
using Domain.Exceptions;

namespace API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = StatusCodes.Status500InternalServerError;
        var result = exception.Message;

        // Map domain-specific exceptions to standard HTTP response codes!
        switch (exception)
        {
            case NotFoundException:
                code = StatusCodes.Status404NotFound;
                break;
            case ValidationException:
                code = StatusCodes.Status400BadRequest;
                break;
            case UnauthorizedAccessException:
                code = StatusCodes.Status401Unauthorized;
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = code;

        var responsePayload = JsonSerializer.Serialize(new { error = result });
        return context.Response.WriteAsync(responsePayload);
    }
}
```

#### B. The Products Controller
*File: `src/API/Controllers/ProductsController.cs`*
```csharp
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _productService.GetProductsAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProductById(int id)
    {
        var result = await _productService.GetProductByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto dto)
    {
        var result = await _productService.CreateProductAsync(dto, "SystemUser");
        return CreatedAtAction(nameof(GetProductById), new { id = result.Id }, result);
    }
}
```

### 3. Wiring it all up
Configure DI (Dependency Injection) in `src/API/Program.cs`:
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register application dependencies
builder.Services.AddScoped<IProductService, ProductService>();
// (Add database context, repositories, and unit of work here)

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
```

### 4. Verification Gate
Start the API locally:
```bash
dotnet run --project src/API
```
**Definition of Done:** Navigate to `http://localhost:<port>/swagger/index.html` in your browser and execute a GET or POST request. It should return a successful JSON response from the database!

---

## Phase 6: Authentication & Security (JWT and Rotation)

### 1. Concepts
* **JWT (JSON Web Token):** A secure, signed string representing credentials. It is stateless (the server doesn't store active tokens in a session table).
* **Refresh Token:** A long-lived, stored string used to request a new short-lived JWT without forcing the user to log in again.
* **Refresh Token Rotation:** An industry best practice where *each time* a refresh token is used to get a new JWT, that refresh token is immediately invalidated and a brand new refresh token is issued.
* **Replay Attack Protection:** If a thief steals a used refresh token and tries to replay it, the system detects that the token was already rotated. To protect the victim, the system **revokes all active sessions** for that user immediately, forcing a complete re-login.

```
USER                                                     SERVER
 │                                                         │
 ├─────────── POST /api/v1/auth/login ────────────────────►│ (Verifies credentials)
 ◄─────────── Returns [AccessToken + RefreshToken A] ──────┤
 │                                                         │
 ─── (Time passes: AccessToken expires) ───────────────────
 │                                                         │
 ├─────────── POST /api/v1/auth/refresh ──────────────────►│ (Rotates Token A)
 │            (Presents RefreshToken A)                    │ Validates Token A
 │                                                         │ Invalidates Token A
 ◄─────────── Returns [AccessToken + RefreshToken B] ──────┤ Generates Token B
 │                                                         │
 ─── (ATTACK CASE: Thief presents Token A again) ─────────
 │                                                         │
 ├─────────── POST /api/v1/auth/refresh ──────────────────►│ ALERT: Token A already used!
 │            (Presents stolen RefreshToken A)             │ Revokes ALL user tokens!
 ◄─────────── Returns 401 Unauthorized ────────────────────┤
```

### 2. Implementation Overview
1. Create a `User` entity inside `Domain.Entities`.
2. Create a `RefreshToken` entity (holding properties like `UserId`, `ExpiresOn`, `RevokedOn`, `ReplacedByToken`).
3. Add specialized endpoints to `AuthController` (`/register`, `/login`, `/refresh`, `/revoke`).
4. Secure critical endpoints in `ProductsController` by adding the `[Authorize(Roles = "Admin")]` attribute.

---

## Phase 7: Polish & Performance

### 1. Concepts
* **Structured Logging (Serilog):** Logging fields as data objects (e.g. `{ProductId}`) rather than formatting them into plain text strings. This lets you run SQL-like queries on your logs later.
* **CORS (Cross-Origin Resource Sharing):** Security feature restricting which web pages can request resources from your API.
* **Security Headers:** Adding HTTP headers like `X-Frame-Options` and `X-Content-Type-Options` to prevent clickjacking and content sniffing.
* **Response Compression:** Compressing HTTP responses (like Brotli or Gzip) to minimize network bandwidth.

### 2. Setup in Program.cs
Ensure these middlewares are registered:
```csharp
app.UseSerilogRequestLogging();
app.UseCors("DefaultCors");
app.UseResponseCompression();
```

---

## Phase 8: Testing

### 1. Concepts
* **Unit Testing:** Testing a single method in isolation (usually in a Service class) by mocking its external dependencies.
* **Mocking (Moq):** Creating fake versions of dependencies (like repositories) that return preset values, allowing you to test business logic without writing to a real database.
* **Integration Testing:** Testing the API endpoints end-to-end (sending real HTTP requests to an in-memory test server and verifying the JSON response).
* **WebApplicationFactory:** A .NET utility that boots up your entire API in-memory during test runs.

### 2. Files to Create
Create this unit test inside the `tests/Application.Tests` project:

*File: `tests/Application.Tests/ProductServiceTests.cs`*
```csharp
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using FluentValidation;
using Moq;
using Xunit;

namespace Application.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task CreateProductAsync_WithValidData_ReturnsCreatedProduct()
    {
        // 1. Arrange: Setup mock repositories and validator rules
        var mockProductRepo = new Mock<IProductRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCreateValidator = new Mock<IValidator<CreateProductDto>>();
        var mockUpdateValidator = new Mock<IValidator<UpdateProductDto>>();

        mockCreateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateProductDto>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var service = new ProductService(
            mockProductRepo.Object, 
            mockUow.Object, 
            mockCreateValidator.Object, 
            mockUpdateValidator.Object);

        var dto = new CreateProductDto { ProductName = "Test Product" };

        // 2. Act: Call the service method
        var result = await service.CreateProductAsync(dto, "AdminTester");

        // 3. Assert: Verify the results
        Assert.NotNull(result);
        Assert.Equal("Test Product", result.ProductName);
        mockProductRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), default), Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
```

### 3. Verification Gate
Run in the root folder:
```bash
dotnet test
```
**Definition of Done:** All tests compile, execute, and display in green (`Passed! - Failed: 0`).

---

## Phase 9: Deploy & Run (Docker & Docker Compose)

### 1. Concepts
* **Docker:** A tool to package an app and all its dependencies (runtime, databases) into a container that runs identically on any computer.
* **Multi-Stage Build:** A docker construction method where you use one heavy image to build and compile the code, then copy the compiled binaries into a lightweight runtime image, making the final docker image extremely small.
* **Docker Compose:** A tool for defining and running multi-container applications (e.g., spinning up your SQL Server container and Web API container simultaneously).

### 2. Verification Gate
Run:
```bash
docker compose up --build
```
**Definition of Done:** The database starts, the API runs migrations and starts up, and you can access Swagger at `http://localhost:8080/swagger` without installing SQL Server locally!

---

## 💡 Summary of Key Developer Commands for Quick Reference

Here are the commands you will use daily while working on this project:

| Action | Command | Where to run |
|:---|:---|:---|
| Build the entire solution | `dotnet build` | Solution root |
| Run the API locally | `dotnet run --project src/API` | Solution root |
| Add a DB Migration | `dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/API` | Solution root |
| Update the Database | `dotnet ef database update --project src/Infrastructure --startup-project src/API` | Solution root |
| Run all Tests | `dotnet test` | Solution root |
| Run Docker Compose | `docker compose up --build` | Solution root |
