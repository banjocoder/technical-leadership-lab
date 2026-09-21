using PolicyService.Readiness;

namespace PolicyService.Endpoints;

public static class ReadinessEndpoints
{
    public static IEndpointRouteBuilder MapReadinessEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/readiness", async (
            IConfiguration configuration,
            ReadinessChecker readinessChecker) =>
        {
            var checks = await readinessChecker.RunAsync();

            var blockingFailed = checks.Any(c =>
                c.Enforcement == Enforcement.Blocking && c.Result == CheckResult.Fail);

            var response = new
            {
                status = blockingFailed ? "NotReady" : "Ready",
                environment = configuration["EnvironmentSettings:DisplayName"],
                timestamp = DateTimeOffset.UtcNow,
                checks
            };

            return Results.Json(
                response,
                statusCode: blockingFailed
                    ? StatusCodes.Status503ServiceUnavailable
                    : StatusCodes.Status200OK);
        });

        return app;
    }
}
