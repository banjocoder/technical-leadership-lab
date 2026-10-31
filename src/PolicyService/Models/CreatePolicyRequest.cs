namespace PolicyService.Models;

public record CreatePolicyRequest(
    string PolicyHolderName,
    string LineOfBusiness,
    Guid RequestId,
    DateOnly EffectiveDate);
