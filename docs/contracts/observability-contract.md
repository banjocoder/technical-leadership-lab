# Observability Contract

This contract defines the telemetry provided by PolicyService for the `POST /policies` issuance workflow. It describes deployment identity, request correlation, error context, metrics, traces, and the current lab collection policy.

The contract covers handler execution, required-field validation, the PolicyDatabase dependency check, and in-memory policy creation. The database check opens a connection and executes `SELECT 1`; it does not persist the policy.

## Deployment Attributes

The same resource attributes must be attached to logs, metrics, and traces.

| Attribute | Exact telemetry key | Source |
| --- | --- | --- |
| Service name | `service.name` | The shared resource configuration assigns `PolicyService` through `AddService`. |
| Artifact version | `service.version` | The extracted artifact's `artifact-manifest.json` supplies `version`. The launch script sets `Deployment__Version`, and the application reads `Deployment:Version`. |
| Source revision | `delivery.source_revision` | The extracted artifact's manifest supplies `sourceRevision`. The launch script sets `Deployment__SourceRevision`, and the application reads `Deployment:SourceRevision`. |
| Environment | `deployment.environment.name` | The application reads `builder.Environment.EnvironmentName` from the runtime hosting environment. The lab launch script sets `ASPNETCORE_ENVIRONMENT=Development`. |

`delivery.source_revision` is a custom resource attribute. Version and revision identify the packaged code; the environment describes where that code is running. Promoting the same artifact should preserve its version and revision while allowing its environment to change.

Local development fallbacks are `local-dev` for version and `local-unidentified` for revision. These values do not establish a specific packaged artifact's identity and must not be used as evidence of an identified deployment.

When conducting a deployment experiment, run the extracted executable and read its own manifest. Assigning an older artifact's version to a newer source-code process would produce misleading telemetry.

## Request Correlation

A trace ID connects participating spans and logs for one request. A span ID identifies a specific operation within that trace. Parent span IDs establish the relationship between operations. Across services, trace continuity requires context propagation and participating instrumentation.

A log captures an event at a particular time. Exported logs include the active trace and span IDs when emitted inside an activity. Logs emitted in different operation scopes can share a trace ID while having different span IDs.

For an investigation that starts with a 503 response:

1. Locate the captured HTTP span or `PolicyDatabaseFailure` log using request time, method, route, and failure event.
2. Read its trace ID.
3. Find the remaining spans and logs with that trace ID.
4. Use span IDs to connect a failure log to its operation, and parent IDs to reconstruct the hierarchy.
5. Read the resource attributes to identify the artifact and environment.

The current implementation has not established that the HTTP response exposes a trace ID. Console records support correlation after a responder locates the initial record. Metrics describe aggregated activity and do not identify an individual request through a trace-ID dimension.

## Error Context

Failure records must provide enough context to identify the failed operation and its deployment without exposing connection strings or request contents.

| Field | Telemetry key or location | Definition | Required when |
| --- | --- | --- | --- |
| Failure event | Log event `PolicyDatabaseFailure`, ID `5102` | Identifies the handled SQL dependency failure. | A `SqlException` is handled by the issuance SQL catch. |
| Operation | Log attribute `Operation` | Business operation, currently `policy.issue`. | A database failure is logged. |
| Component | Log attribute `Component` | Failed dependency, currently `PolicyDatabase`. | A database failure is logged. |
| Dependency | Database span tag `dependency.name` | Dependency involved in the database boundary, currently `PolicyDatabase`. | The database span is created. |
| Error type | Span tag `error.type`; log attribute `ErrorType` | Exception type. The span uses the full type name; the log uses its short name. | A SQL failure is handled; the outer issuance span and unexpected-failure log also identify unexpected exceptions. |
| SQL error number | Log attribute `SqlErrorNumber` | The value reported by `SqlException.Number`. It may describe a client or connection failure before a query reaches SQL Server. | A database failure is logged. |
| Operation stage | Database span tag `database.operation.stage` | Most recent database stage reached: `open_connection` or `execute_query`. | The corresponding database operation is reached and an activity is available. |
| Span status | Activity status `Error` | Marks the database and issuance spans as failed for a handled SQL failure. The outer issuance span is also marked for an unexpected exception. | Those internal failures occur. |
| Business outcome | Issuance span tag `issuance.outcome` | Bounded outcome: `succeeded`, `validation_rejected`, `database_failed`, `cancelled`, or `unexpected_error`. | The corresponding outcome branch is reached. |
| HTTP response status | HTTP span tag `http.response.status_code` | Outcome at the HTTP boundary, such as `201`, `400`, or `503`. | HTTP instrumentation records the response. |
| Correlation | Log trace/span IDs and activity trace/span/parent IDs | Connects the event to the request and operation. | The record is emitted with an active recorded trace. |

Span status, business outcome, and HTTP status describe different boundaries. A required-field rejection returns 400 and records `validation_rejected`; it does not need to mark the business span as an internal error.

The database stage tag records the latest stage reached, including successful execution. It does not separately time connection opening and query execution.

In the failure experiment, `open_connection`, the SQL exception, and the correlated error log establish that failure occurred during connection opening before `SELECT 1` executed. They do not independently establish the underlying infrastructure cause. The deliberately unavailable endpoint is known from the experiment setup, rather than proven by these telemetry fields alone.

## Structured Log Events

| Event ID | Event name | Level | Purpose and fields |
| --- | --- | --- | --- |
| `5100` | `PolicyIssuanceRequested` | Information | Records entry into the workflow with `Operation=policy.issue`. |
| `5101` | `PolicyIssuanceSucceeded` | Information | Records successful creation with `PolicyId`. |
| `5102` | `PolicyDatabaseFailure` | Error | Records `Operation`, `Component`, `ErrorType`, and `SqlErrorNumber`. |
| `5103` | `PolicyIssuanceUnexpectedFailure` | Error | Records `ErrorType` for an unexpected exception that is rethrown. |

The current custom workflow does not define separate rejection or cancellation log events. Those outcomes are represented by counters and issuance span tags.

## Metrics

Custom instruments belong to meter `PolicyService.Issuance`, which must be subscribed to by the metrics provider. The singleton `IssuanceMetrics` service creates the instruments through `IMeterFactory`.

| Instrument | Type | Unit | Definition |
| --- | --- | --- | --- |
| `policy.issue.attempts` | Counter | `{attempt}` | Requests entering the POST handler. |
| `policy.issue.successes` | Counter | `{success}` | Policies successfully created by the handler. |
| `policy.issue.validation_rejections` | Counter | `{rejection}` | Requests rejected by the handler's required-field validation. |
| `policy.issue.failures` | Counter | `{failure}` | Internal issuance failures, including handled SQL failures and unexpected exceptions; excludes validation rejection and recognized caller cancellation. |
| `policy.issue.cancellations` | Counter | `{cancellation}` | `OperationCanceledException` outcomes when the request cancellation token is signaled. |
| `policy.issue.duration` | Histogram | `s` | Handler execution duration for all outcomes, recorded by the outer `finally`. |

Duration includes work and logging inside the measured handler boundary. HTTP response serialization occurs after the handler returns and is outside that duration. Use the HTTP server span to measure the full instrumented request boundary.

For completed executions through the instrumented outcome paths, the intended accounting relationship is:

```text
Attempts = Successes + ValidationRejections + Failures + Cancellations
```

During active requests or process interruption, exported totals may not yet satisfy this relationship. Success means policy creation completed; it does not guarantee the client received the serialized response. Counting must remain once per outcome if later code changes introduce fallible work after an outcome counter increments.

Requests rejected by ASP.NET Core before handler entry, such as malformed JSON, are outside the custom counters. HTTP instrumentation covers those requests.

Counters accumulate within a process. Compare deltas for an experiment, or use a fresh process with a controlled request set. A histogram's observation count is the number of recorded handler durations; its measurements are durations in seconds. An instrument with no measurements may be absent from an export rather than explicitly showing zero.

### Expected Request Accounting

| Request outcome | Attempts | Successes | Rejections | Failures | Cancellations | Duration observations |
| --- | --- | --- | --- | --- | --- | --- |
| Successful creation | +1 | +1 | 0 | 0 | 0 | +1 |
| Required-field rejection | +1 | 0 | +1 | 0 | 0 | +1 |
| Handled SQL failure | +1 | 0 | 0 | +1 | 0 | +1 |
| Unexpected internal exception | +1 | 0 | 0 | +1 | 0 | +1 |
| Recognized caller cancellation | +1 | 0 | 0 | 0 | +1 | +1 |

The verified batch of two valid requests and one validation rejection produced three attempts, two successes, one rejection, zero failures, and three duration observations.

## Trace Structure

Custom spans belong to activity source `PolicyService.Issuance`, which must be subscribed to by the tracing provider.

| Span | Kind | Parent | Work inside its scope |
| --- | --- | --- | --- |
| HTTP `POST /policies` | Server | Incoming trace context, if present | Instrumented HTTP request boundary. |
| `policy.issue` | Internal | HTTP server span | Issuance workflow and its error handling. |
| `policy.validate` | Internal | `policy.issue` | Required-field validation; tag `validation.outcome` is `accepted` or `rejected`. |
| `policy.database.query` | Client | `policy.issue` | Connection opening, `SELECT 1`, and handled SQL error logging. |
| `policy.create` | Internal | `policy.issue` | In-memory `store.CreatePolicy(request)`. |

Validation, database access, and creation are sequential sibling spans. Each scope ends before the next starts. A healthy recorded request has the HTTP span and four custom spans.

On validation rejection, the database and creation boundaries are not reached. On a handled database failure, creation is not reached and no success event should be emitted for that request. The failure log is emitted inside the database activity, so its span ID matches that activity.

Activities can be unavailable because of listener or sampling configuration. Instrumentation uses null-conditional tag and status calls so application behavior does not depend on a span being created. Console export order reflects completion and export behavior; use timestamps and parent IDs to interpret execution.

## Collection Policy

### Current Lab Configuration

Logs, metrics, and traces are exported to the console for local inspection. Evidence is collected from the identified extracted artifact running in Development. This contract does not assert a particular export interval or sampling configuration because those settings were not supplied in the reviewed evidence.

For each controlled experiment:

1. Record the artifact version, revision, and environment.
2. Capture the relevant HTTP and custom spans, correlated workflow logs, and metric export.
3. Allow the next metric export to occur before evaluating counter and histogram changes.
4. Retain the request trace ID and each timing's scope.
5. Restore the temporary configuration and confirm a valid POST succeeds.

Console output has no guaranteed durable retention. Save selected evidence with the lab documentation, removing sensitive content before committing it. A centralized collector, dashboards, alerts, retention period, and access policy have not been established by this lab.

### Data and Cardinality Rules

- Keep custom outcome and stage values bounded to the values defined in this contract.
- Do not add policy IDs, trace IDs, names, or request-specific values as metric dimensions.
- Keep connection strings, credentials, policyholder names, and request bodies out of custom logs and span tags.
- Use the policy ID in the existing success log for investigation, subject to the deployment's access and retention policy.
- Review automatically collected HTTP attributes before using the configuration outside the local lab.

Production collection decisions remain to be specified: exporter destination, sampling, metric export interval and histogram buckets, retention, access, redaction, and ownership. These are future configuration decisions rather than verified capabilities of this implementation.

## Experiment Evidence

### Original Database Failure

| Item | Observed value |
| --- | --- |
| Artifact | `0.2.37` |
| Source revision | `b3193d2179ee74843ac0d4388fd6c6b8ace70330` |
| Environment | `Development` |
| Trace ID | `a810d1d03f31607ccf21b01cadc8efd0` |
| HTTP span ID | `5952fc89caefbe2f` |
| HTTP response | `503`, span status `Error` |
| HTTP duration | `3.0316537 s` |
| Database span ID | `77510882b53348a1` |
| Database duration | `3.0188658 s` |
| Dependency and error | `PolicyDatabase`; `Microsoft.Data.SqlClient.SqlException`; SQL error number `258` |
| Correlation | The failure log and database span share both trace and span IDs. |

This evidence identified the failed dependency but did not distinguish connection opening from query execution.

### Revised Instrumentation

| Item | Observed value |
| --- | --- |
| Artifact | `0.2.38` |
| Source revision | `df8228786fa94941e5b1e4ac17e5f8c0a5a04be5` |
| Environment | `Development` |
| Trace ID | `3eb929f104ffef17e6d98dd92b834759` |
| Database span ID | `c53e208ce2ae24e4` |
| Parent issuance span ID | `fb92cede55cbb36f` |
| Database duration | `3.3604806 s` |
| Stage | `database.operation.stage=open_connection` |
| Dependency and error | `PolicyDatabase`; `Microsoft.Data.SqlClient.SqlException`; SQL error number `258`; span status `Error` |
| Correlation | `PolicyDatabaseFailure` matches the database span's trace and span IDs. |

The added stage attribute establishes failure during connection opening before query execution. The revised trace's full HTTP duration was not supplied; the original request's HTTP timing must not be reused for this trace.

Recovery was reported after restoring the working configuration. Explicit confirmation of the failed request's metric delta and absence of `policy.create` remains outstanding.

## Maintaining the Contract

Update this document when instrument names, outcome definitions, log fields, span boundaries, deployment identity, or collection settings change. Preserve the distinction between verified evidence, expected behavior, and work still awaiting confirmation.

A responder should be able to use the contract to answer: which request failed, which operation and dependency failed, what the records establish about the failure, which deployment was running, and how long the relevant boundaries took.
