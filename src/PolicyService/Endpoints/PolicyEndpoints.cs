using System.Diagnostics;
using Microsoft.Data.SqlClient;
using PolicyService.Models;
using PolicyService.Services;

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
                    IssuanceMetrics issuanceMetrics) =>
            {
                issuanceMetrics.Attempts.Add(1); // Increment the attempts counter
                Stopwatch stopwatch = Stopwatch.StartNew();
                var logger = loggerFactory.CreateLogger("PolicyService.Issuance");

                using var issuance =
                    IssuanceTracing.Source.StartActivity("policy.issue");

                try
                {

                    logger.LogInformation(
                        new EventId(5100, "PolicyIssuanceRequested"),
                        "Policy issuance requested for operation {Operation}",
                        "policy.issue");

                    using (var validation =
                        IssuanceTracing.Source.StartActivity("policy.validate"))
                    {
                        // First, validate the request fields
                        if (string.IsNullOrWhiteSpace(request.PolicyHolderName)
                            || string.IsNullOrWhiteSpace(request.PolicyType))
                        {
                            validation?.SetTag("validation.outcome", "rejected");
                            issuance?.SetTag("issuance.outcome", "validation_rejected");

                            issuanceMetrics.ValidationRejections.Add(1);

                            issuanceMetrics.ValidationRejections.Add(1); // Increment the validation rejections counter
                            return Results.ValidationProblem(new Dictionary<string, string[]>
                            {
                                ["request"] = ["PolicyHolderName and PolicyType are required."]
                            });
                        }
                        
                        validation?.SetTag("validation.outcome", "accepted");
                    
                    }


                     // Validation has ended. This is another child of issuance.
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

                        return Results.Problem(
                            title: "Policy issuance unavailable",
                            detail: "The policy database is currently unavailable.",
                            statusCode: StatusCodes.Status503ServiceUnavailable);
                    }
                }

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
}
