namespace PolicyService.Services;
using PolicyService.Models;
public interface IPolicyRepository
{
    Task<Policy?> GetPolicyAsync(int policyId, CancellationToken cancellationToken);
    Task<CreatePolicyResponse> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken cancellationToken);
}