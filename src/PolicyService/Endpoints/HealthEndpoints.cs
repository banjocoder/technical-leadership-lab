using System.Reflection;

namespace PolicyService.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", (IConfiguration configuration) =>
        {
            var version = Assembly
                .GetExecutingAssembly()
                .GetName()
                .Version?
                .ToString();

            return Results.Ok(new
            {
                status = "Healthy",
                environment = configuration["EnvironmentSettings:DisplayName"],
                version,
                timestamp = DateTimeOffset.UtcNow
            });
        });

        return app;
    }
}
