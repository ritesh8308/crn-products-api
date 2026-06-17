namespace API.Middleware;

/// <summary>
/// Adds a small set of defensive response headers to every response. Set before the
/// response body is written (headers are buffered until the first write), so they
/// apply uniformly across all endpoints. CSP is intentionally omitted: this is a
/// JSON API and a strict policy would break the Swagger UI's inline scripts/styles.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";   // no MIME sniffing
        headers["X-Frame-Options"] = "DENY";             // no framing -> anti-clickjacking
        headers["Referrer-Policy"] = "no-referrer";      // don't leak our URLs onward
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        await _next(context);
    }
}
