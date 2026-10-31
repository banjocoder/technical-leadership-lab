namespace PolicyService.Models;

public record Policy(
    int Id,
    string PolicyHolderName,
    string LineOfBusiness,
    DateTimeOffset CreatedAt,
    string PolicyNumber,
    int TermNumber,
    DateOnly EffectiveDate,
    DateOnly ExpirationDate);
