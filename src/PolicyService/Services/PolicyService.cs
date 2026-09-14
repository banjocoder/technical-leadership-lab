namespace PolicyService.Services;

public class PolicyService
{
    private readonly Dictionary<int, Policy> _policies = new();

    public Policy CreatePolicy(CreatePolicyRequest request)
    {
        var nextId = _policies.Count == 0
            ? 1
            : _policies.Keys.Max() + 1;

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

public record CreatePolicyRequest(
    string PolicyHolderName,
    string PolicyType);

public record Policy(
    int Id,
    string PolicyHolderName,
    string PolicyType,
    DateTimeOffset CreatedAt);