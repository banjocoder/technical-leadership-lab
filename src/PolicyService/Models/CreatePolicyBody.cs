namespace PolicyService.Models;

public record CreatePolicyBody(
    string PolicyHolderName,
    string LineOfBusiness,
    DateOnly EffectiveDate);
