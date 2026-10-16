namespace EmailServiceStub;

public record EmailPolicyRequest(
    string PolicyHolderName,
    string PolicyId,
    Guid EmailRequestId);