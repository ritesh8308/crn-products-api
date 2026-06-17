using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Application.DTOs.Auth;
using Infrastructure.Data;

namespace API.Tests;

/// <summary>
/// End-to-end tests through the real HTTP pipeline (WebApplicationFactory + HttpClient).
/// These exercise auth, role-based authorization, validation and CRUD exactly as a
/// client would. IClassFixture shares one factory (and its seeded admin) across the class.
/// </summary>
public class ProductsApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductsApiIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetProducts_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_AsSeededAdmin_ReturnsTokens()
    {
        var client = _factory.CreateClient();

        var auth = await LoginAsync(client, IdentitySeeder.DefaultAdminUsername, IdentitySeeder.DefaultAdminPassword);

        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.Equal("Admin", auth.Role);
    }

    [Fact]
    public async Task Admin_CanCreateAndFetchProduct()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);

        var create = await client.PostAsJsonAsync("/api/v1/products", new CreateProductDto { ProductName = "IntegrationWidget" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(created);
        Assert.Equal("IntegrationWidget", created!.ProductName);
        Assert.Equal("admin", created.CreatedBy); // audit stamped from the JWT identity

        var get = await client.GetAsync($"/api/v1/products/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task RegisteredUser_CannotCreateProduct_Returns403()
    {
        var client = _factory.CreateClient();
        // Register a fresh plain user and authenticate as them.
        var auth = await RegisterAsync(client, $"user_{Guid.NewGuid():N}", "Passw0rd");
        Assert.Equal("User", auth.Role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/products", new CreateProductDto { ProductName = "Nope" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithEmptyName_Returns400()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/products", new CreateProductDto { ProductName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndOldTokenStopsWorking()
    {
        var client = _factory.CreateClient();
        var auth = await RegisterAsync(client, $"user_{Guid.NewGuid():N}", "Passw0rd");

        // Rotate once: the old refresh token should now be dead.
        var refreshed = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest { RefreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);

        var replay = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest { RefreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    // ---- helpers ----

    private static async Task<AuthResponse> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Username = username, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest { Username = username, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private static async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var auth = await LoginAsync(client, IdentitySeeder.DefaultAdminUsername, IdentitySeeder.DefaultAdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }
}
