using PolicyService.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PolicyService.Services.PolicyService>();

var app = builder.Build();

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

app.MapGet("/readiness", (IConfiguration configuration) =>
{
    var displayName = configuration["EnvironmentSettings:DisplayName"];

    var checks = new List<ReadinessCheck>
    {
        // Blocking: the environment must know its own identity to be usable.
        new(
            Name: "required-configuration",
            Category: "config",
            Enforcement: "blocking",
            Result: string.IsNullOrWhiteSpace(displayName) ? "Fail" : "Pass",
            Detail: string.IsNullOrWhiteSpace(displayName)
                ? "EnvironmentSettings:DisplayName is not set"
                : $"EnvironmentSettings:DisplayName = '{displayName}'")
    };

    var blockingFailed = checks.Any(c =>
        c.Enforcement == "blocking" && c.Result == "Fail");

    var response = new
    {
        status = blockingFailed ? "NotReady" : "Ready",
        environment = displayName,
        timestamp = DateTimeOffset.UtcNow,
        checks
    };

    return Results.Json(
        response,
        statusCode: blockingFailed
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK);
});

app.MapGet(
    "/policies/{id:int}",
    (int id, PolicyService.Services.PolicyService service) =>
    {
        var policy = service.GetPolicy(id);

        return policy is null
            ? Results.NotFound()
            : Results.Ok(policy);
    });

app.MapPost(
    "/policies",
    (
        CreatePolicyRequest request,
        PolicyService.Services.PolicyService service) =>
    {
        var policy = service.CreatePolicy(request);

        return Results.Created(
            $"/policies/{policy.Id}",
            policy);
    });

app.Run();

record ReadinessCheck(
    string Name,
    string Category,
    string Enforcement,
    string Result,
    string Detail);