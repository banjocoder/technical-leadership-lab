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
            async (
                CreatePolicyRequest request,
                PolicyIssuanceService issuanceService,
                CancellationToken cancellationToken) =>
            {
                var outcome = await issuanceService.IssueAsync(request, cancellationToken);

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
