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
            (CreatePolicyRequest request, PolicyStore store) =>
            {
                if (string.IsNullOrWhiteSpace(request.PolicyHolderName)
                    || string.IsNullOrWhiteSpace(request.PolicyType))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["request"] = ["PolicyHolderName and PolicyType are required."]
                    });
                }

                var policy = store.CreatePolicy(request);

                return Results.Created($"/policies/{policy.Id}", policy);
            });

        return app;
    }
}
