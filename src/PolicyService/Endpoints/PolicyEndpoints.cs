using Microsoft.AspNetCore.Mvc;
using PolicyService.Models;
using PolicyService.Services;

namespace PolicyService.Endpoints;

public static class PolicyEndpoints
{
    public static IEndpointRouteBuilder MapPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/policies/{id:int}",
            async (int id, IPolicyRepository repository,
            CancellationToken cancellationToken) =>
            {
                var policy = await repository.GetPolicyAsync(id, cancellationToken);

                return policy is null
                    ? Results.NotFound()
                    : Results.Ok(policy);
            });

        app.MapPost(
            "/policies",
            async (
                CreatePolicyBody body,
                [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
                PolicyIssuanceService issuanceService,
                CancellationToken cancellationToken) =>
            {
                if (idempotencyKey is null || idempotencyKey == Guid.Empty)
                {   
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["Idempotency-Key"] =
                                new[] { "A non-empty GUID header is required." }
                        });
                }

                var request = new CreatePolicyRequest(
                    body.PolicyHolderName,
                    body.LineOfBusiness,
                    idempotencyKey.Value,
                    body.EffectiveDate);

                var outcome = await issuanceService.BeginPolicyIssuanceProcedureAsync(request, cancellationToken);

                return outcome.Status switch
                {
                    IssuanceStatus.Created => Results.Created(
                        $"/policies/{outcome.Policy!.Id}",
                        outcome.Policy),
                    IssuanceStatus.ValidationRejected => Results.ValidationProblem(
                        outcome.ValidationErrors!),
                    _ => Results.Problem(
                        title: "Policy issuance unavailable",
                        detail: outcome.Detail,
                        statusCode: StatusCodes.Status503ServiceUnavailable)
                };
            });

        return app;
    }
}
