namespace PolicyService.Configuration;

public sealed class ServiceEndpointsOptions
{
    public const string SectionName = "ServiceEndpoints";

    public string RiskScoreUrl { get; set; } = "http://localhost:5099";

    public string EmailServiceUrl { get; set; } = "http://localhost:5098";
}
