using EmailServiceStub;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/email-service", async (EmailPolicyRequest request, HttpContext context) =>
{
    var configuredResult = app.Configuration.GetValue<string>("EmailService:Result");

    app.Logger.LogInformation("Email service request received at {TimeStamp}", DateTimeOffset.UtcNow);

    IResult result;

    switch (configuredResult)
    {
        case "success":
            result = Results.Ok(
                new { message = "Email service succeeded." }
            );
            app.Logger.LogInformation("Email service succeeded at {TimeStamp}, PolicyId: {PolicyId}, EmailRequestId: {EmailRequestId}", DateTimeOffset.UtcNow, request.PolicyId, request.EmailRequestId);
            break;

        case "failure":
            result = Results.Problem(
                title: "Email service failure",
                detail: "The email service is configured to fail.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
            app.Logger.LogInformation("Email service configured to fail at {TimeStamp}, PolicyId: {PolicyId}, EmailRequestId: {EmailRequestId}", DateTimeOffset.UtcNow, request.PolicyId, request.EmailRequestId);
            break;

        // Simulate a timeout scenario for 5 seconds
        case "timeout":
            await Task.Delay(
                TimeSpan.FromSeconds(10),
                context.RequestAborted);

            result = Results.Ok(
                new { message = "Email service succeeded." }
            );

            app.Logger.LogInformation("Email service timed out at {TimeStamp}, PolicyId: {PolicyId}, EmailRequestId: {EmailRequestId}", DateTimeOffset.UtcNow, request.PolicyId, request.EmailRequestId);
            break;

        default:
            result = Results.Problem(
                title: "Email service failure",
                detail: "The email service configuration is invalid.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
            app.Logger.LogInformation("Email service encountered an invalid configuration at {TimeStamp}, PolicyId: {PolicyId}, EmailRequestId: {EmailRequestId}", DateTimeOffset.UtcNow, request.PolicyId, request.EmailRequestId);
            break;
    }

    return result;
});

app.Run();
