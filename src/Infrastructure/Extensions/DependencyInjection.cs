using Application.Interfaces;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

/// <summary>
/// One place to register everything Infrastructure provides. API's Program.cs calls
/// services.AddInfrastructure(configuration) and stays ignorant of EF Core details.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext registered as Scoped (one per HTTP request) by default.
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Auth infrastructure. Bind JwtSettings here so both the token service (signing)
        // and the API's JwtBearer middleware (validation) read the same configuration.
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();  // stateless, safe as singleton
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
