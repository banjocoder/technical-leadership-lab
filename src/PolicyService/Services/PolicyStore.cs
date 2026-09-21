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

        var policy = new Policy(
            nextId,
            request.PolicyHolderName,
            request.PolicyType,
            DateTimeOffset.UtcNow);

        _policies[nextId] = policy;

        return policy;
    }

    public Policy? GetPolicy(int id)
    {
        return _policies.GetValueOrDefault(id);
    }
}
