using PolicyService.Models;
using PolicyService.Services;
using Microsoft.Data.SqlClient;

namespace PolicyService.Readiness;

public sealed class ReadinessChecker(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    PolicyStore policyStore)
{
    public async Task<IReadOnlyList<ReadinessCheck>> RunAsync()
    {
        var checks = new List<ReadinessCheck>
        {
            CheckRequiredConfiguration()
        };

        checks.Add(await CheckDatabaseAsync());
        checks.Add(RunPolicySmokeTest());
        checks.Add(await CheckExternalDependencyAsync());

        return checks;
    }

    private ReadinessCheck CheckRequiredConfiguration()
    {
        var displayName = configuration["EnvironmentSettings:DisplayName"];
        var missing = string.IsNullOrWhiteSpace(displayName);

        return new ReadinessCheck(
            Name: "required-configuration",
            Category: CheckCategory.Config,
            Enforcement: Enforcement.Blocking,
            Result: missing ? CheckResult.Fail : CheckResult.Pass,
            Detail: missing
                ? "EnvironmentSettings:DisplayName is not set"
                : $"EnvironmentSettings:DisplayName = '{displayName}'");
    }

    private async Task<ReadinessCheck> CheckDatabaseAsync()
    {
        if (!configuration.GetValue<bool>("Readiness:RequireDatabase"))
        {
            return Skipped(
                "database",
                CheckCategory.Dependency,
                "Database not required for this deployment scope");
        }

        var connectionString = configuration.GetConnectionString("PolicyDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Blocking(
                "database",
                CheckCategory.Dependency,
                CheckResult.Fail,
                "ConnectionStrings:PolicyDatabase is not set");
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            return Blocking(
                "database",
                CheckCategory.Dependency,
                CheckResult.Pass,
                "Opened a connection to the policy database");
        }
        catch (Exception ex)
        {
            return Blocking(
                "database",
                CheckCategory.Dependency,
                CheckResult.Fail,
                $"Database connection failed: {ex.Message}");
        }
    }

    private ReadinessCheck RunPolicySmokeTest()
    {
        if (!configuration.GetValue<bool>("Readiness:RequirePolicySmokeTest"))
        {
            return Skipped(
                "policy-smoke-test",
                CheckCategory.Workflow,
                "Policy smoke test not required for this deployment scope");
        }

        try
        {
            var created = policyStore.CreatePolicy(
                new CreatePolicyRequest("readiness-probe", "smoke-test"));
            var fetched = policyStore.GetPolicy(created.Id);

            return Blocking(
                "policy-smoke-test",
                CheckCategory.Workflow,
                fetched is not null ? CheckResult.Pass : CheckResult.Fail,
                fetched is not null
                    ? $"Created and retrieved policy {created.Id}"
                    : "Created policy could not be retrieved");
        }
        catch (Exception ex)
        {
            return Blocking(
                "policy-smoke-test",
                CheckCategory.Workflow,
                CheckResult.Fail,
                $"Policy smoke test threw: {ex.Message}");
        }
    }

    private async Task<ReadinessCheck> CheckExternalDependencyAsync()
    {
        if (!configuration.GetValue<bool>("Readiness:RequireExternalDependency"))
        {
            return Skipped(
                "external-dependency",
                CheckCategory.Dependency,
                "External dependency not required for this deployment scope");
        }

        var dependencyUrl = configuration["ExternalDependency:Url"];

        if (string.IsNullOrWhiteSpace(dependencyUrl))
        {
            return Blocking(
                "external-dependency",
                CheckCategory.Dependency,
                CheckResult.Fail,
                "ExternalDependency:Url is not configured");
        }

        if (!Uri.TryCreate(dependencyUrl, UriKind.Absolute, out var uri))
        {
            return Blocking(
                "external-dependency",
                CheckCategory.Dependency,
                CheckResult.Fail,
                $"ExternalDependency:Url is invalid: '{dependencyUrl}'");
        }

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(2);

            using var response = await client.GetAsync(uri);

            return Blocking(
                "external-dependency",
                CheckCategory.Dependency,
                response.IsSuccessStatusCode ? CheckResult.Pass : CheckResult.Fail,
                response.IsSuccessStatusCode
                    ? $"Dependency responded with HTTP {(int)response.StatusCode}"
                    : $"Dependency returned HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return Blocking(
                "external-dependency",
                CheckCategory.Dependency,
                CheckResult.Fail,
                $"Dependency unavailable: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static ReadinessCheck Skipped(
        string name,
        CheckCategory category,
        string detail) =>
        new(name, category, Enforcement.Conditional, CheckResult.Skipped, detail);

    private static ReadinessCheck Blocking(
        string name,
        CheckCategory category,
        CheckResult result,
        string detail) =>
        new(name, category, Enforcement.Blocking, result, detail);
}
