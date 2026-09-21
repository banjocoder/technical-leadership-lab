namespace PolicyService.Models;

public record CreatePolicyRequest(
    string PolicyHolderName,
    string PolicyType);
