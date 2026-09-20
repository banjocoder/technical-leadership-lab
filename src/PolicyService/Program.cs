using PolicyService.Services;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PolicyService.Services.PolicyService>();
builder.Services.AddHttpClient();

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

app.MapGet("/readiness", async (
    IConfiguration configuration,
    PolicyService.Services.PolicyService policyService,
    IHttpClientFactory httpClientFactory) =>
{
    var displayName = configuration["EnvironmentSettings:DisplayName"];

    var checks = new List<ReadinessCheck>
    {
        new(
            Name: "required-configuration",
            Category: CheckCategory.Config,
            Enforcement: Enforcement.Blocking,
            Result: string.IsNullOrWhiteSpace(displayName)
                ? CheckResult.Fail
                : CheckResult.Pass,
            Detail: string.IsNullOrWhiteSpace(displayName)
                ? "EnvironmentSettings:DisplayName is not set"
                : $"EnvironmentSettings:DisplayName = '{displayName}'")
    };

    var requireDatabase = configuration.GetValue<bool>("Readiness:RequireDatabase");
    if (!requireDatabase)
    {
        checks.Add(new(
            Name: "database",
            Category: CheckCategory.Dependency,
            Enforcement: Enforcement.Conditional,
            Result: CheckResult.Skipped,
            Detail: "Database not required for this deployment scope"));
    }
    else
    {
        var (result, detail) = await CheckDatabaseAsync(
            configuration.GetConnectionString("PolicyDatabase"));
        checks.Add(new(
            Name: "database",
            Category: CheckCategory.Dependency,
            Enforcement: Enforcement.Blocking,
            Result: result,
            Detail: detail));
    }

    var requireSmokeTest = configuration.GetValue<bool>("Readiness:RequirePolicySmokeTest");
    if (!requireSmokeTest)
    {
        checks.Add(new(
            Name: "policy-smoke-test",
            Category: CheckCategory.Workflow,
            Enforcement: Enforcement.Conditional,
            Result: CheckResult.Skipped,
            Detail: "Policy smoke test not required for this deployment scope"));
    }
    else
    {
        var (result, detail) = RunPolicySmokeTest(policyService);
        checks.Add(new(
            Name: "policy-smoke-test",
            Category: CheckCategory.Workflow,
            Enforcement: Enforcement.Blocking,
            Result: result,
            Detail: detail));
    }

    var requireExternalDependency = configuration.GetValue<bool>("Readiness:RequireExternalDependency");

    if (!requireExternalDependency)
    {
        checks.Add(new(
            Name: "external-dependency",
            Category: CheckCategory.Dependency,
            Enforcement: Enforcement.Conditional,
            Result: CheckResult.Skipped,
            Detail: "External dependency not required for this deployment scope"));
    }
    else
    {
        var dependencyUrl = configuration["ExternalDependency:Url"];

        var (result, detail) = await CheckExternalDependencyAsync(
            httpClientFactory,
            dependencyUrl);

        checks.Add(new(
            Name: "external-dependency",
            Category: CheckCategory.Dependency,
            Enforcement: Enforcement.Blocking,
            Result: result,
            Detail: detail));
    }

    var blockingFailed = checks.Any(c =>
        c.Enforcement == Enforcement.Blocking && c.Result == CheckResult.Fail);

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

static async Task<(CheckResult Result, string Detail)> CheckDatabaseAsync(
    string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return (CheckResult.Fail, "ConnectionStrings:PolicyDatabase is not set");
    }

    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return (CheckResult.Pass, "Opened a connection to the policy database");
    }
    catch (Exception ex)
    {
        return (CheckResult.Fail, $"Database connection failed: {ex.Message}");
    }
}

static async Task<(CheckResult Result, string Detail)>
    CheckExternalDependencyAsync(
        IHttpClientFactory httpClientFactory,
        string? dependencyUrl)
{
    if (string.IsNullOrWhiteSpace(dependencyUrl))
    {
        return (
            CheckResult.Fail,
            "ExternalDependency:Url is not configured");
    }

    if (!Uri.TryCreate(
        dependencyUrl,
        UriKind.Absolute,
        out var uri))
    {
        return (
            CheckResult.Fail,
            $"ExternalDependency:Url is invalid: '{dependencyUrl}'");
    }

    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(2);

        using var response = await client.GetAsync(uri);

        if (response.IsSuccessStatusCode)
        {
            return (
                CheckResult.Pass,
                $"Dependency responded with HTTP {(int)response.StatusCode}");
        }

        return (
            CheckResult.Fail,
            $"Dependency returned HTTP {(int)response.StatusCode}");
    }
    catch (Exception ex)
    {
        return (
            CheckResult.Fail,
            $"Dependency unavailable: {ex.GetType().Name}: {ex.Message}");
    }
}

static (CheckResult Result, string Detail) RunPolicySmokeTest(
    PolicyService.Services.PolicyService service)
{
    try
    {
        var created = service.CreatePolicy(
            new CreatePolicyRequest("readiness-probe", "smoke-test"));
        var fetched = service.GetPolicy(created.Id);

        return fetched is not null
            ? (CheckResult.Pass, $"Created and retrieved policy {created.Id}")
            : (CheckResult.Fail, "Created policy could not be retrieved");
    }
    catch (Exception ex)
    {
        return (CheckResult.Fail, $"Policy smoke test threw: {ex.Message}");
    }
}

record ReadinessCheck(
    string Name,
    CheckCategory Category,
    Enforcement Enforcement,
    CheckResult Result,
    string Detail);

[JsonConverter(typeof(JsonStringEnumConverter))]
enum CheckCategory
{
    Config,
    Dependency,
    Workflow
}

[JsonConverter(typeof(JsonStringEnumConverter))]
enum Enforcement
{
    Blocking,
    Conditional
}

[JsonConverter(typeof(JsonStringEnumConverter))]
enum CheckResult
{
    Pass,
    Fail,
    Skipped
}