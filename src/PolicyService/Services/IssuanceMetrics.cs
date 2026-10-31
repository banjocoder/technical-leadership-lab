using System.Diagnostics.Metrics;

namespace PolicyService.Services;

public sealed class IssuanceMetrics
{
    public Counter<long> Attempts { get; }
    public Histogram<double> Duration { get; }

    // TODO: Declare Successes, ValidationRejections, and Failures.
    public Counter<long> Successes { get; }
    public Counter<long> ValidationRejections { get; }
    public Counter<long> Failures { get; }
    public Counter<long> Cancellations { get; }
    public IssuanceMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("PolicyService.Issuance");

        Attempts = meter.CreateCounter<long>(
            "policy.issue.attempts", unit: "{attempt}");

        Duration = meter.CreateHistogram<double>(
            "policy.issue.duration", unit: "s");

        // TODO: Create the remaining three counters.
        Successes = meter.CreateCounter<long>(
            "policy.issue.successes", unit: "{success}");

        ValidationRejections = meter.CreateCounter<long>(
            "policy.issue.validation_rejections", unit: "{rejection}");

        Failures = meter.CreateCounter<long>(
            "policy.issue.failures", unit: "{failure}");

        Cancellations = meter.CreateCounter<long>(
            "policy.issue.cancellations", unit: "{cancellation}");
    }
}