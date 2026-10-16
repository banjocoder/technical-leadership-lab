namespace EmailServiceStub;

public record EmailPolicyRequest(
    string PolicyHolderName,
    string PolicyId,
    Guid EmailRequestId);

// Entry-point marker so tests can host this stub via WebApplicationFactory.
public sealed class EmailServiceStubApp { }