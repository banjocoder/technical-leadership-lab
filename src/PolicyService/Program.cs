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