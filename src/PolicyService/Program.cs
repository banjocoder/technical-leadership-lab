using PolicyService.Configuration;
using PolicyService.Endpoints;
using PolicyService.Readiness;
using PolicyService.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ServiceEndpointsOptions>(
    builder.Configuration.GetSection(ServiceEndpointsOptions.SectionName));
builder.Services.Configure<PolicyRulesOptions>(
    builder.Configuration.GetSection(PolicyRulesOptions.SectionName));

builder.Services.AddSingleton<PolicyStore>();
builder.Services.AddScoped<ReadinessChecker>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<PolicyDatabase>();
builder.Services.AddScoped<PolicyIssuanceService>();
builder.Services.AddSingleton<IssuanceMetrics>();

var serviceEndpoints = builder.Configuration
    .GetSection(ServiceEndpointsOptions.SectionName)
    .Get<ServiceEndpointsOptions>() ?? new ServiceEndpointsOptions();

// Explicit local identifier for setup.
// Later, deployed runs will supply the real artifact version.
var version = builder.Configuration["Deployment:Version"] ?? "local-dev";
var sourceRevision =
    builder.Configuration["Deployment:SourceRevision"] ?? "local-unidentified";

var environment = builder.Environment.EnvironmentName;

Action<ResourceBuilder> configureResource = resource => resource
    .AddService("PolicyService", serviceVersion: version)
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment.name"] = environment,
        ["delivery.source_revision"] = sourceRevision
    });

builder.Logging.AddOpenTelemetry(options =>
{
    var resource = ResourceBuilder.CreateDefault();
    configureResource(resource);

    options.SetResourceBuilder(resource);
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    options.AddConsoleExporter();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(configureResource)
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddSource("PolicyService.Issuance")
        .SetSampler(new AlwaysOnSampler())
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("PolicyService.Issuance")
        .AddConsoleExporter());

builder.Services.AddHttpClient("RiskScore", client =>
{
    client.BaseAddress = new Uri(serviceEndpoints.RiskScoreUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient("EmailService", client =>
{
    client.BaseAddress = new Uri(serviceEndpoints.EmailServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.MapHealthEndpoints();
app.MapReadinessEndpoints();
app.MapPolicyEndpoints();

app.Run();
