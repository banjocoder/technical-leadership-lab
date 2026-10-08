## Reflection prompts:

R1. Which telemetry signal gave you the fastest answer?

Metrics give the fastest and simplest answers. It simply states "Number of attempts, failures, etc." but they are less meaningful.

R2. What information would you regret not capturing during an incident?

Actual exceptions that were thrown by the system and stack traces.

R3. How would you prevent high-cardinality telemetry from becoming noisy or expensive?

For metrics, I would use bounded dimensions such as operation, outcome, and dependency, and avoid request IDs, policy IDs, or customer names as labels. For logs and traces, I would control volume through filtering, sampling where appropriate, and retention limits. I would alert on actionable conditions rather than every error log.

## Knowledge check:

1. What is a log?

Logs capture individual events and context. 

2. What is a metric?

Metrics are numerical measurements aggregated over time—counts, durations, and rates.

3. What is a trace?

Traces connect timed operations within a request.

4. How is an event different from an alert?

A health check evaluates a defined aspect of an application’s condition, such as whether it is running or ready to serve requests. Passing establishes only what those checks cover at that point in time.

5. What is a health check?

A health check is a series of checks to verify that the app is running and able to receive requests. 

6. Why is "we have logs" not equivalent to "the system is observable"?

Having logs does not guarantee that we can understand the system’s behavior or investigate failures. Observability depends on useful, contextual, correlated telemetry that lets us answer questions about what happened and why.
