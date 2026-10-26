using System.Diagnostics;
using Microsoft.Data.SqlClient;
using PolicyService.Models;

namespace PolicyService.Services;

public enum IssuanceStatus
{
    Created,
    ValidationRejected,
    DatabaseUnavailable,
    RiskScoreUnavailable
}

public sealed record IssuanceOutcome(
    IssuanceStatus Status,
    Policy? Policy = null,
    string? Detail = null,
    IDictionary<string, string[]>? ValidationErrors = null);

public sealed class PolicyIssuanceService(
    IPolicyRepository repository,
    PolicyDatabase database,
    IHttpClientFactory httpClientFactory,
    IssuanceMetrics metrics,
    ILogger<PolicyIssuanceService> logger)
{
    public async Task<IssuanceOutcome> BeginPolicyIssuanceProcedureAsync(
        CreatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        metrics.Attempts.Add(1);
        var stopwatch = Stopwatch.StartNew();

        using var issuance =
            IssuanceTracing.Source.StartActivity("policy.issue");

        try
        {
            logger.LogInformation(
                new EventId(5100, "PolicyIssuanceRequested"),
                "Policy issuance requested for operation {Operation}",
                "policy.issue");

            var validationError = ValidateRequest(request, issuance);
            if (validationError is not null)
            {
                return validationError;
            }

            var databaseError =
                await ValidateDatabaseAsync(issuance, cancellationToken);
            if (databaseError is not null)
            {
                return databaseError;
            }

            var riskScoreError =
                await ValidateRiskScoreAsync(request, issuance, cancellationToken);
            if (riskScoreError is not null)
            {
                return riskScoreError;
            }

            var response = await IssuePolicy(request, issuance, cancellationToken);

            if (response.IsNew)
            {
                await SendMarketingEmailAsync(request, response.Policy, cancellationToken);
            }

            return new IssuanceOutcome(IssuanceStatus.Created, Policy: response.Policy);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            metrics.Cancellations.Add(1);
            issuance?.SetTag("issuance.outcome", "cancelled");
            throw;
        }
        catch (Exception ex)
        {
            metrics.Failures.Add(1);

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
            metrics.Duration.Record(stopwatch.Elapsed.TotalSeconds);
        }
    }

    private IssuanceOutcome? ValidateRequest(
        CreatePolicyRequest request,
        Activity? issuance)
    {
        using var validation =
            IssuanceTracing.Source.StartActivity("policy.validate");

        if (string.IsNullOrWhiteSpace(request.PolicyHolderName)
            || string.IsNullOrWhiteSpace(request.LineOfBusiness))
        {
            validation?.SetTag("validation.outcome", "rejected");
            issuance?.SetTag("issuance.outcome", "validation_rejected");

            metrics.ValidationRejections.Add(1);

            return new IssuanceOutcome(
                IssuanceStatus.ValidationRejected,
                ValidationErrors: new Dictionary<string, string[]>
                {
                    ["request"] = ["PolicyHolderName and LineOfBusiness are required."]
                });
        }

        validation?.SetTag("validation.outcome", "accepted");
        return null;
    }

    private async Task<IssuanceOutcome?> ValidateDatabaseAsync(
        Activity? issuance,
        CancellationToken cancellationToken)
    {
        using var databaseActivity = IssuanceTracing.Source.StartActivity(
            "policy.database.query",
            ActivityKind.Client);

        databaseActivity?.SetTag("dependency.name", "PolicyDatabase");

        try
        {
            await database.VerifyIssuanceDependencyAsync(cancellationToken);
            await database.ValidateLineOfBusinessAsync(cancellationToken);
        }
        catch (SqlException ex)
        {
            databaseActivity?.SetStatus(ActivityStatusCode.Error);
            databaseActivity?.SetTag("error.type", ex.GetType().FullName);

            issuance?.SetStatus(ActivityStatusCode.Error);
            issuance?.SetTag("issuance.outcome", "database_failed");

            metrics.Failures.Add(1);

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

            return new IssuanceOutcome(
                IssuanceStatus.DatabaseUnavailable,
                Detail: "The policy database has an error: " + firstLine);
        }

        return null;
    }

    private async Task<IssuanceOutcome?> ValidateRiskScoreAsync(
        CreatePolicyRequest request,
        Activity? issuance,
        CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient("RiskScore");

        try
        {
            using var riskScore = await client.PostAsJsonAsync(
                "/risk-score",
                new { request.PolicyHolderName },
                cancellationToken);
            riskScore.EnsureSuccessStatusCode();
            var riskScoreValue = await riskScore.Content.ReadAsStringAsync(cancellationToken);

            issuance?.SetTag("risk.score", riskScoreValue);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            metrics.Failures.Add(1);
            issuance?.SetStatus(ActivityStatusCode.Error);
            issuance?.SetTag("issuance.outcome", "risk_score_failed");

            return new IssuanceOutcome(
                IssuanceStatus.RiskScoreUnavailable,
                Detail: "The risk score service timed out.");
        }
        catch (HttpRequestException)
        {
            metrics.Failures.Add(1);
            issuance?.SetStatus(ActivityStatusCode.Error);
            issuance?.SetTag("issuance.outcome", "risk_score_failed");

            return new IssuanceOutcome(
                IssuanceStatus.RiskScoreUnavailable,
                Detail: "The risk score service is unavailable.");
        }

        return null;
    }

    private async Task<CreatePolicyResponse> IssuePolicy(CreatePolicyRequest request, Activity? issuance, CancellationToken cancellationToken)
    {
        CreatePolicyResponse response;

        using (var creation =
            IssuanceTracing.Source.StartActivity("policy.create"))
        {
            response = await repository.CreatePolicyAsync(request, cancellationToken);
        }

        metrics.Successes.Add(1);
        issuance?.SetTag("issuance.outcome", "succeeded");

        logger.LogInformation(
            new EventId(5101, "PolicyIssuanceSucceeded"),
            "Policy issuance succeeded with policy {PolicyId}",
            response.Policy.Id);

        return response;
    }

    private async Task SendMarketingEmailAsync(
        CreatePolicyRequest request,
        Policy policy,
        CancellationToken cancellationToken)
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
