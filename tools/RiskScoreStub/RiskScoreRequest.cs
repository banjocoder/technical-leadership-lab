namespace RiskScoreStub;

public record RiskScoreRequest(
    string PolicyHolderName);

// Entry-point marker so tests can host this stub via WebApplicationFactory.
public sealed class RiskScoreStubApp { }