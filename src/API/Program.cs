using API.Middleware;
using API.Swagger;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Application.Validators;
using Asp.Versioning;
using FluentValidation;
using Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ---- MVC / controllers ----
builder.Services.AddControllers();

// ---- Application services (composition root) ----
// Bound here, in the API, so the Application project stays free of any DI framework.
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IValidator<CreateProductDto>, CreateProductValidator>();
builder.Services.AddScoped<IValidator<UpdateProductDto>, UpdateProductValidator>();

// ---- Infrastructure (DbContext, repositories, UnitOfWork) ----
builder.Services.AddInfrastructure(builder.Configuration);

// ---- API versioning (kept light: a single URL-segment version, v1) ----
builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true; // unversioned callers get v1
        options.ReportApiVersions = true;                   // advertise versions in response headers
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";       // groups are named v1, v2, ...
        options.SubstituteApiVersionInUrl = true; // replaces {version} token in Swagger URLs
    });

// ---- Swagger / OpenAPI ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>(); // one Swagger doc per version
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---- HTTP request pipeline ----
// Exception handling goes first so it wraps everything downstream.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Register an endpoint for each discovered API version.
        foreach (var description in app.DescribeApiVersions())
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposes the implicit Program class to the integration-test project (Phase 8,
// WebApplicationFactory<Program>). Harmless now; saves a change later.
public partial class Program { }
