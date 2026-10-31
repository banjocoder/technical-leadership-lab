using System.Diagnostics;

namespace PolicyService.Services;

public static class IssuanceTracing
{
    public static readonly ActivitySource Source =
        new("PolicyService.Issuance");
}