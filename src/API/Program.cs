using System.Text;
using API.Middleware;
using API.Swagger;
using Application.DTOs;
using Application.DTOs.Auth;
using Application.Interfaces;
using Application.Services;
using Application.Validators;
using Asp.Versioning;
using FluentValidation;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---- MVC / controllers ----
builder.Services.AddControllers();

// ---- Application services (composition root) ----
// Bound here, in the API, so the Application project stays free of any DI framework.
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IValidator<CreateProductDto>, CreateProductValidator>();
builder.Services.AddScoped<IValidator<UpdateProductDto>, UpdateProductValidator>();
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

// ---- Infrastructure (DbContext, repositories, UnitOfWork, token service, hasher) ----
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Authentication (JWT bearer) ----
// Validates incoming "Authorization: Bearer <jwt>" tokens using the SAME JwtSettings
// the token service signs with. A failed/missing token yields 401 automatically.
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // no grace period — expired means expired
        };
    });

builder.Services.AddAuthorization();

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

// Order matters: authenticate (who are you?) before authorize (are you allowed?).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed a default Admin account on startup so the Admin-only endpoints are reachable
// for the demo (self-registration only ever creates plain Users).
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await IdentitySeeder.SeedAdminAsync(context, hasher);
}

app.Run();

// Exposes the implicit Program class to the integration-test project (Phase 8,
// WebApplicationFactory<Program>). Harmless now; saves a change later.
public partial class Program { }
