using System.Diagnostics;
using Microsoft.Data.SqlClient;
using PolicyService.Models;
using PolicyService.Services;

using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PolicyService.Endpoints;

public static class PolicyEndpoints
{
    public static IEndpointRouteBuilder MapPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/policies/{id:int}",
            (int id, PolicyStore store) =>
            {
                var policy = store.GetPolicy(id);

                return policy is null
                    ? Results.NotFound()
                    : Results.Ok(policy);
            });

        app.MapPost(
            "/policies",
            async (CreatePolicyRequest request, 
                    PolicyStore store, 
                    PolicyDatabase database, 
                    CancellationToken cancellationToken,
                    ILoggerFactory loggerFactory,
                    IssuanceMetrics issuanceMetrics,
                    IHttpClientFactory httpClientFactory) =>
            {
                #region InitialLogging
                issuanceMetrics.Attempts.Add(1); // Increment the attempts counter
                Stopwatch stopwatch = Stopwatch.StartNew();
                var logger = loggerFactory.CreateLogger("PolicyService.Issuance");

                using var issuance =
                    IssuanceTracing.Source.StartActivity("policy.issue");
                #endregion InitialLogging

                try
                {

                    logger.LogInformation(
                        new EventId(5100, "PolicyIssuanceRequested"),
                        "Policy issuance requested for operation {Operation}",
                        "policy.issue");

                    // Validate Request
                    var validationResult = ValidateRequest(request, issuanceMetrics, issuance);
                    if (validationResult is not null)
                    {
                        return validationResult;
                    }

                    // Validate Database
                    var dbValidationResult = await ValidateDB(database, issuanceMetrics, logger, issuance, cancellationToken);
                    if (dbValidationResult is not null)
                    {
                        return dbValidationResult;
                    }

                    // Validate Risk Score
                    var riskScoreValidationResult = await ValidateRiskScore(request, httpClientFactory, issuanceMetrics, issuance, cancellationToken);
                    if (riskScoreValidationResult is not null)
                    {
                        return riskScoreValidationResult;
                    }

                    // Issue Policy
                    Policy policy = IssuePolicy(request, store, issuanceMetrics, logger, issuance);

                    // Send Marketing Email
                    await SendMarketingEmail(request, httpClientFactory, logger, policy, cancellationToken);

                    return Results.Created($"/policies/{policy.Id}", policy);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    issuanceMetrics.Cancellations.Add(1); // Increment the cancellations counter
                    issuance?.SetTag("issuance.outcome", "cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    issuanceMetrics.Failures.Add(1);

                    issuance?.SetStatus(ActivityStatusCode.Error);
                    issuance?.SetTag("issuance.outcome", "unexpected_error");
                    issuance?.SetTag("error.type", ex.GetType().FullName);

                    logger.LogError(
                        new EventId(5103, "PolicyIssuanceUnexpectedFailure"),
                        "Unexpected policy issuance failure. ErrorType: {ErrorType}",
                        ex.GetType().Name);

                    throw;
                }
                finally
                {
                    issuanceMetrics.Duration.Record(
                        stopwatch.Elapsed.TotalSeconds); // Record handler duration for every outcome
                }
            });

        return app;
    }

    private static async Task<IResult?> ValidateRiskScore(CreatePolicyRequest request, IHttpClientFactory httpClientFactory, IssuanceMetrics issuanceMetrics, Activity? issuance, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient("RiskScore");
        try
        {
            using var riskScore = await client.PostAsJsonAsync(
                "/risk-score",
                new { request.PolicyHolderName },
                cancellationToken);
            riskScore.EnsureSuccessStatusCode();
            var riskScoreValue = await riskScore.Content.ReadAsStringAsync();

            issuance?.SetTag("risk.score", riskScoreValue);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {

            issuanceMetrics.Failures.Add(1);
            issuance?.SetStatus(ActivityStatusCode.Error);
            issuance?.SetTag("issuance.outcome", "risk_score_failed");
            
            return Results.Problem(
                title: "Policy issuance unavailable",
                detail: "The risk score service timed out.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (HttpRequestException)
        {
            issuanceMetrics.Failures.Add(1);
            issuance?.SetStatus(ActivityStatusCode.Error);
            issuance?.SetTag("issuance.outcome", "risk_score_failed");
            return Results.Problem(
                title: "Policy issuance unavailable",
                detail: "The risk score service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return null;
    }

    private static async Task<IResult?> ValidateDB(PolicyDatabase database, IssuanceMetrics issuanceMetrics, ILogger logger, Activity? issuance, CancellationToken cancellationToken)
    {
        using (var databaseActivity =
                                IssuanceTracing.Source.StartActivity(
                                    "policy.database.query",
                                    ActivityKind.Client))
        {
            databaseActivity?.SetTag(
                "dependency.name", "PolicyDatabase");

            try
            {
                await database.VerifyIssuanceDependencyAsync(
                    cancellationToken);

                await database.ValidateLineOfBusinessAsync(
                    cancellationToken);
            }
            catch (SqlException ex)
            {
                databaseActivity?.SetStatus(ActivityStatusCode.Error);
                databaseActivity?.SetTag(
                    "error.type", ex.GetType().FullName);

                issuance?.SetStatus(ActivityStatusCode.Error);
                issuance?.SetTag("issuance.outcome", "database_failed");

                issuanceMetrics.Failures.Add(1);

                logger.LogError(
                    new EventId(5102, "PolicyDatabaseFailure"),
                    "Policy issuance dependency failed. " +
                    "Operation: {Operation}; Component: {Component}; " +
                    "ErrorType: {ErrorType}; SqlErrorNumber: {SqlErrorNumber}",
                    "policy.issue",
                    "PolicyDatabase",
                    ex.GetType().Name,
                    ex.Number);

                var firstLine = ex.Message
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault() ?? "Unknown database error.";

                return Results.Problem(
                    title: "Policy issuance unavailable",
                    detail: "The policy database has an error: " + firstLine,
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }

        return null;
    }

    private static IResult? ValidateRequest(CreatePolicyRequest request, IssuanceMetrics issuanceMetrics, Activity? issuance)
    {
        using (var validation =
            IssuanceTracing.Source.StartActivity("policy.validate"))
        {
            // First, validate the request fields
            if (string.IsNullOrWhiteSpace(request.PolicyHolderName)
                || string.IsNullOrWhiteSpace(request.PolicyType))
            {
                validation?.SetTag("validation.outcome", "rejected");
                issuance?.SetTag("issuance.outcome", "validation_rejected");

                issuanceMetrics.ValidationRejections.Add(1); // Increment the validation rejections counter
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["PolicyHolderName and PolicyType are required."]
                });
            }

            validation?.SetTag("validation.outcome", "accepted");

        }

        return null;
    }

    private static Policy IssuePolicy(CreatePolicyRequest request, PolicyStore store, IssuanceMetrics issuanceMetrics, ILogger logger, Activity? issuance)
    {
        // Declare outside the block so the result remains accessible.
        Policy policy;

        using (var creation =
            IssuanceTracing.Source.StartActivity("policy.create"))
        {
            policy = store.CreatePolicy(request);
        }

        issuanceMetrics.Successes.Add(1);
        issuance?.SetTag("issuance.outcome", "succeeded");

        logger.LogInformation(
            new EventId(5101, "PolicyIssuanceSucceeded"),
            "Policy issuance succeeded with policy {PolicyId}",
            policy.Id);
        return policy;
    }

    private static async Task SendMarketingEmail(CreatePolicyRequest request, IHttpClientFactory httpClientFactory, ILogger logger, Policy policy, CancellationToken cancellationToken)
    {
        var emailRequest = new
        {
            request.PolicyHolderName,
            PolicyId = policy.Id.ToString(),
            EmailRequestId = Guid.NewGuid()
        };

        try
        {
            using var emailClient = httpClientFactory.CreateClient("EmailService");

            using var response = await emailClient.PostAsJsonAsync(
                "/email-service",
                emailRequest,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Marketing email outcome: {outcome}, PolicyId: {PolicyId}, EmailRequestId: {EmailRequestId}",
                    response.StatusCode,
                    policy.Id,
                    emailRequest.EmailRequestId);
            }
            else
            {
                logger.LogWarning(
                "Marketing email outcome: {Outcome}; PolicyId: {PolicyId}; EmailRequestId: {EmailRequestId}; HTTP status: {StatusCode}",
                "Failed",
                policy.Id,
                emailRequest.EmailRequestId,
                (int)response.StatusCode);
            }

        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(
                "Marketing email outcome: {Outcome}; PolicyId: {PolicyId}; EmailRequestId: {EmailRequestId}; Reason: {Reason}",
                "Request Exception",
                policy.Id,
                emailRequest.EmailRequestId,
                "Request timed out");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(
                "Marketing email outcome: {Outcome}; PolicyId: {PolicyId}; EmailRequestId: {EmailRequestId}; Reason: {Reason}",
                "Unknown",
                policy.Id,
                emailRequest.EmailRequestId,
                ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Marketing email outcome: {Outcome}; PolicyId: {PolicyId}; EmailRequestId: {EmailRequestId}; Reason: {Reason}",
                "Unknown",
                policy.Id,
                emailRequest.EmailRequestId,
                ex.Message);
        }
    }
}
