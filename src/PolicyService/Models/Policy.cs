namespace PolicyService.Models;

public record Policy(
    int Id,
    string PolicyHolderName,
    string PolicyType,
    DateTimeOffset CreatedAt);
