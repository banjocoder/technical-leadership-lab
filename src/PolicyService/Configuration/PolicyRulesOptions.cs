namespace PolicyService.Configuration;

public sealed class PolicyRulesOptions
{
    public const string SectionName = "PolicyRules";

    public string RequiredLineOfBusiness { get; set; } = "Fire";
}
