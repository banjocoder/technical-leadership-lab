using RiskScoreStub;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/risk-score", async (RiskScoreRequest request, HttpContext context) =>
{
    var delayMs = app.Configuration.GetValue<int>("RiskStub:DelayMs");

    app.Logger.LogInformation(
        "Risk-score request received at {Timestamp}; delay {DelayMs}ms",
        DateTimeOffset.UtcNow,
        delayMs);

    await Task.Delay(delayMs, context.RequestAborted);

    return Results.Text("Low");
});

app.Run();