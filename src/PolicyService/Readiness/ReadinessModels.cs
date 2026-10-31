using System.Text.Json.Serialization;

namespace PolicyService.Readiness;

public record ReadinessCheck(
    string Name,
    CheckCategory Category,
    Enforcement Enforcement,
    CheckResult Result,
    string Detail);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CheckCategory
{
    Config,
    Dependency,
    Workflow
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Enforcement
{
    Blocking,
    Conditional
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CheckResult
{
    Pass,
    Fail,
    Skipped
}
