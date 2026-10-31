namespace PolicyService.Models;

public record CreatePolicyResponse(
    Policy Policy,
    bool IsNew
);