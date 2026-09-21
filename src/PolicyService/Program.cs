using PolicyService.Endpoints;
using PolicyService.Readiness;
using PolicyService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PolicyStore>();
builder.Services.AddScoped<ReadinessChecker>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.MapHealthEndpoints();
app.MapReadinessEndpoints();
app.MapPolicyEndpoints();

app.Run();
