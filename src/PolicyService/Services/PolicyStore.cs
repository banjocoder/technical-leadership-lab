using System.Collections.Concurrent;
using PolicyService.Models;

namespace PolicyService.Services;

public class PolicyStore
{
    private readonly ConcurrentDictionary<int, Policy> _policies = new();
    private int _lastId;

    public Policy CreatePolicy(CreatePolicyRequest request)
    {
        var nextId = Interlocked.Increment(ref _lastId);

        var expirationDate = request.EffectiveDate.AddYears(1).AddDays(-1);
        var policyNumber = $"POL-{nextId:D6}";

        var policy = new Policy(
            nextId,
            request.PolicyHolderName,
            request.LineOfBusiness,
            DateTimeOffset.UtcNow,
            policyNumber,
            1,
            request.EffectiveDate,
            expirationDate);

        _policies[nextId] = policy;

        return policy;
    }

    public Policy? GetPolicy(int id)
    {
        return _policies.GetValueOrDefault(id);
    }
}
