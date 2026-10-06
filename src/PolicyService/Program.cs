using PolicyService.Endpoints;
using PolicyService.Readiness;
using PolicyService.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PolicyStore>();
builder.Services.AddScoped<ReadinessChecker>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<PolicyDatabase>();
builder.Services.AddSingleton<IssuanceMetrics>();

// Explicit local identifier for setup.
// Later, deployed runs will supply the real artifact version.
var version = builder.Configuration["Deployment:Version"] ?? "local-dev";
var environment = builder.Environment.EnvironmentName;

Action<ResourceBuilder> configureResource = resource => resource
    .AddService("PolicyService", serviceVersion: version)
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment.name"] = environment
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

var app = builder.Build();

app.MapHealthEndpoints();
app.MapReadinessEndpoints();
app.MapPolicyEndpoints();

app.Run();
