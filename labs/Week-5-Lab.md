# Week 5 Lab: Observability with OpenTelemetry

Weeks 1-4 established a working PolicyService delivery process, identified artifacts, independent readiness validation, architecture views, and recorded architectural decisions.

Week 5 asks a different question:

> When policy issuance fails, can another engineer explain what happened using the telemetry your system already emits?

You will instrument the application, deliberately break its lab database, investigate the resulting evidence, and refine an **Observability Contract**. Follow the Week 4 pattern: reason first, build a small increment, inspect the evidence, revise it, and then reflect. This walkthrough supplies starter material and review criteria; your implementation choices and investigation answers remain your work.

**Workbook alignment:** Technical Leadership Study Workbook, Week 5, pages 16-18. Suggested time: **6-7 hours**. The final appendix records the coverage review of this walkthrough against those pages.

## Learning outcomes

By the end of this lab, you should be able to:

- Distinguish logs, metrics, traces, health checks, and alerts.
- Instrument ASP.NET Core using OpenTelemetry concepts.
- Correlate telemetry with deployment identity.
- Use telemetry to answer concrete operational questions.

## Before you begin

This lab assumes you completed Weeks 1-4 in your own `technical-leadership-lab` repository. Have these available:

- PolicyService, its test project, and the pinned SDK from `global.json`.
- Your locked package dependencies and build/test/package workflow.
- DEV and QA stages that consume the same immutable artifact.
- The health/readiness endpoints and database configuration from Week 3.
- Week 4 architecture documentation and ADR-001 through ADR-003.
- Your artifact manifest and deployment/readiness evidence format.
- A disposable lab database, its normal connection settings, and a way to restore it.

Work with your actual code and paths. Do not invent an existing policy issuance endpoint or database-backed issuance operation. Step 2 supplies a small extension path if those do not exist yet.

Use local development or a disposable DEV/QA runner for failure experiments. The original lab environments are ephemeral: collect evidence before their jobs and application processes disappear. You can complete the runtime investigation locally using an identified published artifact; a long-lived hosted environment is not required.

Week 4 selected R2 for future delivery evidence storage. **Implementing R2, a historical dashboard, or an alerting platform is not required this week.** Runtime telemetry and delivery evidence have different lifecycles. Explain how their shared identity permits correlation without treating the evidence bucket as an automatic telemetry backend.

## How to work through the week

| Work block | Suggested time | Result |
| --- | --- | --- |
| Reading/tutorials and initial contract | 105 minutes | Signal model and draft operational questions |
| Baseline workflow and OpenTelemetry setup | 60 minutes | Issuance path and visible telemetry |
| Logs, metrics, and tracing | 100 minutes | Instrumented critical workflow |
| Deployment identity and failure investigation | 75 minutes | Correlated failure evidence |
| Instrumentation revision and contract review | 35 minutes | Repeatable evidence-based diagnosis |
| Knowledge check, reflection, and closeout | 25 minutes | Completed portfolio record |

This is a planning guide, not a deadline. Extra database setup may take longer.

Checkpoints occur at larger milestones. At each checkpoint, save the evidence and resolve missing signals before adding more infrastructure.

## Required reading and tutorial selections

Use the three resources named by the workbook:

1. [OpenTelemetry .NET reference](https://opentelemetry.io/docs/languages/dotnet/) - use it to find SDK configuration, instrumentation, and export guidance.
2. [OpenTelemetry .NET Getting Started](https://opentelemetry.io/docs/languages/dotnet/getting-started/) - spend 60-90 minutes working through the ASP.NET Core tutorial in a scratch project or adapting it carefully to PolicyService.
3. [.NET observability with OpenTelemetry](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel) - allow 45 minutes for Microsoft's guidance and notes.

Record what you completed in `docs/week-05/README.md`. If you use a scratch tutorial, retain a short observation about each exported signal; the tutorial does not replace the PolicyService exercises.

Additional implementation references:

- [Log correlation](https://opentelemetry.io/docs/languages/dotnet/logs/correlation/)
- [.NET tracing API](https://opentelemetry.io/docs/languages/dotnet/traces/)
- [.NET metrics API](https://opentelemetry.io/docs/languages/dotnet/metrics/)

## Portfolio artifacts

Follow your repository's existing conventions; these paths are suggested:

| Path | Purpose |
| --- | --- |
| `docs/week-05/README.md` | Setup, reading record, runtime workflow, run instructions, and artifact index |
| `docs/week-05/observability-contract.md` | **Required deliverable:** deployment attributes, correlation, workflow metrics, and error context |
| `docs/week-05/experiments.md` | Healthy baseline, database failure, telemetry revision, and recovery evidence |
| `docs/week-05/evidence/` | Sanitized exported telemetry and request results with trace IDs and timestamps |
| `docs/week-05/retrospective.md` | Knowledge-check answers, reflection prompts, and weekly retrospective |
| `src/PolicyService/` | Runtime instrumentation and any minimal issuance-path extension |
| Existing workflow/configuration paths | Deployment identity injection and telemetry capture, where applicable |

Screenshots can supplement evidence. Retain searchable text or exported records so a reviewer can follow the request without reading pictures alone.

---

## Step 1: Start with questions and a draft contract

Create the Week 5 documentation directory and an initial contract before writing instrumentation code.

Your contract describes the evidence a responder can depend on. It is more specific than “we use OpenTelemetry,” but does not need to list every framework-generated field.

### A. Define the investigation questions

Write the workbook's five questions at the top of `experiments.md`, leaving the answers blank:

1. **Which request failed?**
2. **Why?**
3. **Which component failed?**
4. **Which deployment was running?**
5. **How long did the request take?**

For each, propose a signal and the fields you expect to use. Revisit those guesses after the experiment.

### B. Distinguish the signals

Complete this table in your own words. Use a concrete PolicyService example for each row.

| Concept | What question does it help answer? | Proposed PolicyService example | What does it not establish by itself? |
| --- | --- | --- | --- |
| Log | To explain | To propose | To explain |
| Metric | To explain | To propose | To explain |
| Trace | To explain | To propose | To explain |
| Health/readiness check | To explain | To propose | To explain |
| Alert | To explain | To propose | To explain |

Prompts:

- What is different about an individual failed request and a rising failure rate?
- What does a readiness probe test that a business request may not?
- Who would act on an alert, and what evidence would they need next?

Do not implement alerts just to fill the table. Propose one meaningful alert condition and explain its intended responder, evaluation window, and action. Distinguishing an event from an alert is part of the learning outcome.

### C. Starter Observability Contract

Save this scaffold as `docs/week-05/observability-contract.md`. Fill it incrementally using observed records, not assumed output.

```markdown
# PolicyService Observability Contract

- Status: Draft
- Date: YYYY-MM-DD
- Runtime/workflow covered: [define the boundary]
- Instrumentation owner: [name or role]

## Operational Questions

[The five questions and expected evidence.]

## Deployment Attributes

| Attribute | Meaning | Authoritative source | Signals carrying it | Missing-value behavior |
| --- | --- | --- | --- | --- |
| [name] | [meaning] | [source] | [signals] | [behavior] |

## Request Correlation

[Trace ID, span ID, any request ID, propagation, and how to find records.]

## Critical Workflow Events

| Event | When emitted | Severity | Required fields | Forbidden fields |
| --- | --- | --- | --- | --- |
| [event] | [boundary] | [level] | [fields] | [fields] |

## Critical Workflow Metrics

| Metric | Definition | Instrument | Unit | Allowed dimensions | Recording boundary |
| --- | --- | --- | --- | --- | --- |
| [name] | [meaning] | [type] | [unit] | [dimensions] | [boundary] |

## Trace Coverage

[Span boundaries, parent-child relationships, status, and timing scope.]

## Error Context

[Error classification, failed component, exception policy, and HTTP outcome.]

## Collection and Operating Policy

[Export route, collection delay, sampling, retention, access, cost, owner.]

## Verification

[Links to healthy, failure, revision, and recovery evidence.]

## Tradeoffs and Limitations

[Implemented policy, accepted tradeoff, and explicitly deferred work.]
```

Choose required fields deliberately. A useful contract says what happens when identity or correlation is missing and how a reviewer verifies compliance.

## Step 2: Establish an actual policy issuance workflow

Inspect the existing endpoint handlers and database access. Draw or describe the normal execution path in `README.md`.

The workflow for this week must perform:

**HTTP request -> validation -> database query -> policy creation**

These are observable runtime boundaries. They are not four labels added around a method that never uses the database.

### If you already have this workflow

Reuse it. Identify its route, a valid synthetic request, its validation rule, its database operation, its creation success boundary, and its error response. Keep the existing business behavior intact while adding telemetry.

### If the Week 1 service is still minimal

Build a small teaching workflow rather than a full insurance application:

1. Add a `POST /policies/issue` endpoint (or equivalent route using your conventions).
2. Define a request with a synthetic reference and one input you can validate, such as a non-empty product code. Use fictitious data.
3. Reject invalid input before database access, with a deliberate client-error response.
4. For valid input, open the lab database using your existing provider and configuration, and execute a real query such as `SELECT 1`. Await it and set a bounded timeout appropriate to the lab.
5. Only after the query succeeds, create a synthetic policy result, return a generated identifier, and respond with your chosen success status.
6. Map database failures to a deliberate server-error response without returning raw connection details or exception internals to the caller.

**Scope declaration:** With `SELECT 1`, the database confirms connectivity; it does not store a policy. Document that creation is synthetic/in-memory and not durable. If your existing workflow persists policies, instrument the actual persistence operation instead. Do not claim the toy version proves transaction correctness, idempotency, or production issuance.

Starter shape, intentionally left for you to implement:

```text
Receive issuance request
  Validate input
  If invalid: record rejected outcome and return client error
  Execute actual database query
  If database fails: record dependency failure and return server error
  Create policy result
  Record successful issuance and return result
Always record the attempted workflow's duration and final outcome
```

Keep dependency exceptions distinguishable from validation rejection and unexpected application errors. Do not count a readiness database probe as an issuance request.

Record baseline behavior before instrumentation:

| Case | Input or setup | HTTP outcome | Was the database queried? | Was a policy result created? |
| --- | --- | --- | --- | --- |
| Valid request | To record | To observe | To observe | To observe |
| Invalid request | To record | To observe | To observe | To observe |

Keep the valid body in a reusable local file or a documented PowerShell variable. Include the exact route, port, command, expected status, and prerequisites in your run instructions.

## Step 3: Configure OpenTelemetry and export a first request

Use the official Getting Started tutorial to add the SDK integration, ASP.NET Core instrumentation, and console exporter to the existing service.

### Preserve the Week 2 dependency discipline

Select explicit compatible package versions from the official package documentation for your pinned framework. Record your selections; update and commit the project file and lock file together. Do not copy an old tutorial's version numbers without checking compatibility.

For your existing .NET 9 CLI, the command pattern is:

```powershell
# Set each variable to the compatible version you selected first.
dotnet add src/PolicyService/PolicyService.csproj package OpenTelemetry.Extensions.Hosting --version $otelSdkVersion
dotnet add src/PolicyService/PolicyService.csproj package OpenTelemetry.Exporter.Console --version $otelSdkVersion
dotnet add src/PolicyService/PolicyService.csproj package OpenTelemetry.Instrumentation.AspNetCore --version $otelAspNetVersion

dotnet restore TechnicalLeadershipLab.sln
dotnet restore TechnicalLeadershipLab.sln --locked-mode
dotnet build TechnicalLeadershipLab.sln --configuration Release --no-restore
dotnet test TechnicalLeadershipLab.sln --configuration Release --no-build
```

These version variables are intentional inputs. Set them before running the commands. If your project paths or package compatibility differ, adapt them and record the reason. Adding packages is an intentional lock update; ordinary CI should continue restoring in locked mode.

### Start with one export route

Console export is sufficient for this walkthrough when you retain the records and can correlate them. You may use an existing telemetry backend instead. Record how logs, metrics, and traces reach it, how to retrieve records, and where resource metadata appears.

The following fragments go in `Program.cs` before `builder.Build()`. Merge them with your current setup; do not replace existing services, health checks, routes, or logging configuration wholesale.

```csharp
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// builder is your existing WebApplicationBuilder.
// Step 7 supplies these values from deployment configuration/manifest.
var artifactVersion = builder.Configuration["Deployment:Version"]
    ?? throw new InvalidOperationException("Deployment version is required for this lab.");
var deploymentEnvironment = builder.Environment.EnvironmentName;

Action<ResourceBuilder> configureResource = resource => resource
    .AddService("PolicyService", serviceVersion: artifactVersion)
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment.name"] = deploymentEnvironment
    });

builder.Logging.AddOpenTelemetry(options =>
{
    var resource = ResourceBuilder.CreateDefault();
    configureResource(resource);
    options.SetResourceBuilder(resource);
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    options.AddConsoleExporter();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(configureResource)
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddSource("PolicyService.Issuance")
        .SetSampler(new AlwaysOnSampler()) // Small lab only; review later.
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("PolicyService.Issuance")
        .AddConsoleExporter());
```

Check these APIs against the package versions you selected. Resource metadata can appear in a resource block shared by records rather than as repeated fields in each log line. Verify the actual output for all three signals.

**Local bootstrap:** Before starting the process, supply `Deployment__Version` and `ASPNETCORE_ENVIRONMENT` as described in Step 7. The starter deliberately rejects a missing version, so do not mistake that startup failure for the database experiment.

Run the service in one terminal and send requests from another. Save application output, for example with `Tee-Object`, to an experiment-specific evidence file. Exported metrics can arrive periodically; allow an export interval to pass, then shut down gracefully so pending data can flush. Abrupt process termination can lose evidence.

**Checkpoint: baseline workflow and export setup.**

- A valid issuance request really queries the lab database and creates the declared result.
- Invalid input is rejected before the query.
- An HTTP server span and exported log records are visible.
- SDK configuration includes your custom source and meter names exactly.
- You know where version/environment resource attributes appear.
- The normal build and test checks pass with locked restore.

## Step 4: Add structured workflow logs

Add the three events required by the workbook:

| Required event | Placement to implement | Review question |
| --- | --- | --- |
| Policy issuance requested | At the accepted workflow entry, before validation completes | Can you identify the operation and its request context? |
| Policy issuance succeeded | After the declared creation boundary succeeds | Does this prove the operation reached success rather than merely started? |
| Database failure | At the failed database-operation boundary | Can you identify the dependency and safe error category? |

You may add a validation-rejection or overall-failure event if it resolves a real gap. Choose stable event names/IDs and severities in the contract.

Use structured message templates. This syntax example establishes the pattern, not the completed event catalog:

```csharp
logger.LogInformation(
    new EventId(5100, "PolicyIssuanceRequested"),
    "Policy issuance requested for operation {Operation}",
    "policy.issue");

// TODO: Add the success and database-failure events at their real boundaries.
// TODO: Define the fields and severity for each in your contract.
```

Avoid interpolation that hides meaningful values inside a single message string. Inspect exported properties, not just the formatted message.

For the database failure, capture the exception type or safe provider error code, failed component, operation, outcome, and correlation context. Decide whether sanitized exception detail is needed. Raw exception messages can include sensitive connection or query information; review actual output before committing evidence.

OpenTelemetry log records can obtain trace/span context from the active .NET `Activity`. Verify the exported `TraceId` and `SpanId`; plain console-provider output may look different. See the [correlation reference](https://opentelemetry.io/docs/languages/dotnet/logs/correlation/).

Decide whether an extra request ID is useful. If present, explain its relationship to the trace ID instead of assuming the two are interchangeable. Keep correlation identifiers out of metric dimensions.

**Mini-review:** Run one successful request. Find its requested and succeeded logs, follow their trace ID, and confirm no database-failure event exists for that request. Write down missing fields before proceeding.

## Step 5: Add critical workflow metrics

The workbook requires metrics for **total policy issues, failures, and duration**. Define “total” carefully: successful issues and attempted requests are different quantities.

Implement at least:

- A counter for successfully issued policy results.
- A counter for failed issuance attempts, with a documented inclusion/exclusion policy.
- A duration histogram that includes both successful and failed attempts.

An attempts counter is a useful addition for denominators and count reconciliation. Include it in this lab so you can verify that failures do not vanish from the totals.

### Starter instruments

Create a reusable telemetry class, adjusting its namespace to your project:

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;

internal static class IssuanceTelemetry
{
    internal const string Name = "PolicyService.Issuance";
    internal static readonly ActivitySource Activities = new(Name);
    internal static readonly Meter Meter = new(Name);

    internal static readonly Counter<long> Attempts =
        Meter.CreateCounter<long>("policy.issue.attempts", unit: "{attempt}");
    internal static readonly Counter<long> Issued =
        Meter.CreateCounter<long>("policy.issue.successes", unit: "{policy}");
    internal static readonly Counter<long> Failures =
        Meter.CreateCounter<long>("policy.issue.failures", unit: "{attempt}");
    internal static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("policy.issue.duration", unit: "s");
}
```

These are lab-specific names, not a claim that they are standard semantic-convention instruments. Static source/meter instances avoid constructing instruments for every request. Keep their names aligned with `AddSource` and `AddMeter`.

### Decide where measurements belong

1. Increment attempts exactly once at workflow entry.
2. Increment successful issues only after the creation boundary succeeds.
3. Increment failed attempts exactly once for each outcome your failure definition includes.
4. Start a monotonic timer at workflow entry; record elapsed seconds in a `finally` block so early returns and exceptions do not erase duration.
5. Use a small, bounded outcome set, such as `succeeded`, `validation_rejected`, `database_failed`, and `unexpected_error`, adapting names to your code.

If validation rejection is excluded from failures, track or explain it separately. Write a reconciliation equation appropriate to your policy. Do not count the same database exception as two failed attempts because it crosses two handlers.

Questions for your contract:

- Does duration measure the business handler, the full HTTP request, or only the database call?
- Which unit is emitted, and where is conversion performed?
- Which dimensions are bounded and operationally useful?
- Are a trace ID, policy ID, customer ID, raw URL, exception message, or timestamp creating a series per request?
- What additional series can deployment-version resource labels create over time in your chosen backend?

Keep unique identifiers in appropriately protected logs/traces, not metric labels. Resource labels can also affect exported series cardinality; filtering and retention policies still matter.

### Inspect a small deterministic batch

In a quiet process, record the starting metrics, send a known number of valid and invalid requests, and inspect the next export. Compare **deltas** if export is cumulative; do not assume one printed interval equals one request batch.

| Measurement | Expected delta from your definitions | Observed delta | Explanation of any difference |
| --- | --- | --- | --- |
| Attempts | To calculate | To observe | To explain |
| Successful issues | To calculate | To observe | To explain |
| Failures | To calculate | To observe | To explain |
| Duration observation count | To calculate | To observe | To explain |

Built-in HTTP metrics complement the workflow metrics. A healthy `/health` request should not increment policy issuance counters.

## Step 6: Trace the critical workflow

Use ASP.NET Core instrumentation for the HTTP server span. Add custom activities for the business-operation boundaries.

Describe the trace tree you expect before coding. A straightforward shape is an HTTP parent span containing an issuance span whose validation, database query, and creation spans execute sequentially. Those three spans are normally siblings under issuance; do not nest database work inside an already completed validation span merely to create a chain.

### Starter activity pattern

Place this fragment around your real validation work, inside the active HTTP request:

```csharp
using (var validation = IssuanceTelemetry.Activities.StartActivity("policy.validate"))
{
    // TODO: Run existing input validation here.
    // TODO: Record its bounded outcome and relevant safe context.
}
```

Apply the pattern at the other real boundaries, choosing stable span names. Ensure the activity scopes dispose promptly, so each duration measures the intended work. Await database operations inside their activity scope. `StartActivity` can return null; use null-safe tagging and status calls.

For the database boundary:

- Use a client span kind for an actual call to the external database.
- Identify the dependency safely; avoid full connection strings or parameter values.
- Mark the failed database span with `ActivityStatusCode.Error` and a safe error classification.
- Ensure the enclosing issuance span also reflects the failed business outcome.
- Inspect the HTTP status and server span outcome separately.
- Do not emit a creation span or successful-issuance event when the database operation prevented creation.

You may use supported database auto-instrumentation, or a manual client span around the real query. Manual tracing is sufficient for this small lab. If using both, avoid duplicate spans for the same operation or explain their different boundaries.

Trace review prompts:

1. Do all spans belong to the same trace ID?
2. Can you follow each parent span ID back to the incoming HTTP request?
3. Can you distinguish validation time, database time, and creation time?
4. Does a validation rejection skip database work?
5. Do logs share the expected trace and active span context?

**Checkpoint: complete healthy-path instrumentation.**

Save one healthy trace, its requested/succeeded logs, and the batch metric observations. Confirm the database work is real, the success boundary is honest, counters reconcile, and resource attributes are present. Explain one tradeoff before continuing: for example, manual DB spans versus provider instrumentation, or console export versus a searchable backend.

## Step 7: Attach and verify deployment identity

Runtime telemetry must identify the artifact and environment actually running, not merely the repository branch or current pipeline number.

Complete this mapping using your manifest/deployment evidence conventions:

| Attribute or field | Suggested meaning | Source to verify |
| --- | --- | --- |
| `service.name` | Stable logical runtime name | Application configuration |
| `service.version` | Immutable artifact version | Original build manifest |
| `deployment.environment.name` | Runtime target, such as DEV or QA | Deployment configuration |
| Source revision, optional custom attribute | Commit that produced the artifact | Build manifest |
| Workflow run/attempt, optional custom attribute | Delivery execution identity | Deployment-stage inputs |
| Artifact SHA-256, optional custom attribute | Identity of promoted package | Existing verified artifact metadata |

At minimum, version and environment must be present in **logs, metrics, and traces** through their resource metadata or a documented equivalent. Use the same authoritative values for all three providers. Mark custom keys explicitly rather than presenting them as standard fields.

### Local run pattern

Launch an identified published artifact, supplying its real manifest version and local environment through external configuration:

```powershell
# Set $manifestVersion to the actual version read from your artifact manifest.
$env:Deployment__Version = $manifestVersion
$env:ASPNETCORE_ENVIRONMENT = "DEV"

# Start your existing published executable using its actual path.
# Keep the database and other required settings from the successful baseline.
```

Document the real manifest path/property and launch command in your README. A development run may use an explicit local identifier during setup; the deployment-correlation evidence should use a manifest-backed artifact identity. Never label an unpublished local change as a previously built artifact version.

For the existing workflow, set identity inputs before starting PolicyService in each deployment job. DEV and QA should report the same artifact version but different environments. Retrieve version from the downloaded package's manifest, rather than recalculating it during promotion. If you run both environments, save an example from each.

Do not rebuild an artifact just to stamp QA identity into it. Environment belongs in deployment/runtime configuration. Reuse ADR-001's immutability rules.

Record missing-identity behavior: the starter fails startup when version is missing; if you choose a different policy, justify it and make unidentified telemetry visible. Avoid quietly defaulting to `unknown` and claiming the deployment question is answered.

**Mini-review:** Join one runtime trace to the matching build/deployment evidence using your contract's identity fields. Show the actual match, not only a table of intended field names.

## Step 8: Break the database and investigate from telemetry

Create an experiment entry with date/time, artifact identity, environment, normal configuration, chosen failure, restoration steps, and capture location. Save private normal configuration locally; do not commit credentials.

### A. Capture a healthy baseline

Start the identified service, issue a valid synthetic request, and record its response, trace ID, relevant logs/spans, and metric deltas. Confirm that the query and creation succeed before changing the database condition.

### B. Inject one database fault

Use one reversible option suited to your disposable environment:

- Stop the dedicated lab database process/service while PolicyService remains running.
- Change only the issuance database connection to a deliberately unavailable local endpoint, with bounded connection/command timeouts, then restart the application.

With the second option, preserve the same artifact version and record the configuration change and restart. Keep enough startup dependencies working that the HTTP request reaches database access. Confirm required configuration is present: a missing connection-string setting is a configuration test, not evidence of a failed database query.

If readiness blocks an automated smoke test after the fault, invoke the lab issuance endpoint directly for this experiment. Record readiness separately; do not relax the accepted promotion policy. No unhealthy artifact needs to be promoted to QA to complete this exercise.

A deliberate exception inserted before a database call is not a substitute for the workbook's database failure. Verify that an actual database connection/query was attempted.

### C. Send a known valid request

Reuse the valid baseline input. Capture HTTP status, time, and any safe correlation identifier exposed to the caller. Avoid invalid input that exits before the database stage.

If useful, provide a response correlation header containing the active trace ID, or record a synthetic experiment marker in logs. Document the lookup method. Do not add that marker as a metric label.

Inspect the next metric export, then stop the application gracefully after retaining the evidence. Copy runner evidence before job teardown if working in CI.

### D. Answer all five questions from emitted evidence

Use exported telemetry to complete this table. The fault you injected is experimental setup, not the evidence for the answer.

| Workbook question | Your answer | Signal and exact record/field reference | What remains uncertain? |
| --- | --- | --- | --- |
| Which request failed? | To investigate | To cite | To assess |
| Why? | To investigate | To cite | To assess |
| Which component failed? | To investigate | To cite | To assess |
| Which deployment was running? | To investigate | To cite | To assess |
| How long did the request take? | To investigate | To cite | To assess |

Reference saved filenames plus trace/span IDs, event names, resource attributes, timestamps, or line references. Distinguish the observed failure category from a deeper cause you cannot establish. For example, a connection failure may identify an unreachable dependency without proving why its process stopped.

Use the HTTP server span for full request timing. Your workflow histogram may measure a narrower business boundary; explain the difference rather than substituting database-span duration for request duration. Aggregate metrics alone do not identify the exact failed request.

Also inspect:

- Whether a database-failure log exists with useful, safe context.
- Whether the database and issuance spans carry failure status.
- Whether the success metric/log stayed unchanged for the failed request.
- Whether failure and duration observations increased according to your contract.
- Whether readiness and liveness changed, and what each result actually establishes.

**Checkpoint: first incident investigation.**

Another engineer should be able to follow your records to each answer without being told which fault you injected. If an answer depends on memory, reading source code, or connecting to the database server, record that as an instrumentation gap.

## Step 9: Improve instrumentation, repeat, and restore

The workbook explicitly requires revision until **all five questions can be answered from telemetry**.

Create a gap table before changing code:

| Missing answer or ambiguity | Evidence gap | Change you will make | Expected verification |
| --- | --- | --- | --- |
| To identify | To explain | To implement | To specify |

Examples of gaps to look for, not predetermined conclusions:

- Correlation is missing from dependency-failure logs.
- Version appears on traces but not log/metric resources.
- An error is visible only as HTTP 500, with no failed component.
- Timing covers the database call but not the HTTP request.
- Metrics omit failed requests because the histogram is recorded only on success.

Implement the smallest useful revision and rerun the healthy/failure cases. If application code changes, rebuild and publish a **new identified artifact**; update the recorded version. Do not keep the old artifact identity while changing its contents.

If the first run already answers all five questions, perform an explicit review and make one evidence-backed refinement to clarity, consistency, or noise, then verify it. Explain the rationale; do not add arbitrary telemetry to meet a count.

Add a before/after comparison with references to the observed records. Complete the five-question table again for the revised failed request. Preserve both experiments so the improvement is assessable.

Restore the database/settings and repeat a healthy request. Confirm creation resumes, no dependency-failure event belongs to the recovered request, and metrics reflect the new success. Remove temporary fault settings and record the restoration result.

**Checkpoint: revision and recovery.**

All five answers now have telemetry evidence, the database fault has been removed, and the same published application operates normally under restored configuration. Build/test checks remain green after any code revisions.

## Step 10: Finalize and review the Observability Contract

Turn the draft contract into a document another engineer can use without this lab. Replace placeholders with decisions and observed examples.

### Review in three passes

**Pass 1 - Identity and correlation**

- Are required version/environment values authoritative and present on all signals?
- Can a request lead to its logs and spans through stable trace context?
- Can runtime identity lead back to artifact and deployment evidence?
- Are missing values and propagation limitations explicit?

**Pass 2 - Workflow and failure evidence**

- Are requested, succeeded, and database-failure events defined by real boundaries?
- Are counts, units, outcomes, dimensions, and duration scope unambiguous?
- Do traces cover HTTP, validation, query, and creation on success, with honest omissions on failure?
- Is error classification useful without disclosing secrets or synthetic input unnecessarily?
- Does the contract link to actual healthy, failed, revised, and recovered records?

**Pass 3 - Operational cost and ownership**

- Who maintains instrumentation, investigates missing telemetry, and handles collection failures?
- Where is telemetry collected, who can access it, and how long is it retained?
- What does console export fail to provide compared with a production backend?
- What sampling policy is used, and can failed requests disappear under that policy?
- Which dimensions must never become per-request metric labels?
- What collection delay, volume, retention, and resource-cardinality costs have you accepted?

The starter uses always-on trace sampling for a small deterministic experiment. Do not claim that policy scales automatically to production. Propose a production tradeoff and explain its diagnostic consequence; implementing a sampling platform is optional.

Keep a short “implemented versus deferred” table. Distinguish this week's console/backend export from future centralized collection, dashboards, alert delivery, and the Week 4 R2 decision.

If instrumentation introduces a new architectural dependency in your version of the system, reconcile the relevant Week 4 view. Do not create an ADR merely because you added a package; preserve a decision when its consequences justify one.

**Contract acceptance:** Have a reviewer, coach, or independent self-review select one failed trace and answer the five questions using the contract and linked records. Record the result and set the contract status to Reviewed when those checks pass.

---

## Step 11: Knowledge check

Answer the workbook's six questions without consulting its answer key. Save your answers in `retrospective.md` before checking them.

1. What is a log?
2. What is a metric?
3. What is a trace?
4. How is an event different from an alert?
5. What is a health check?
6. Why is “we have logs” not equivalent to “the system is observable”?

After answering, compare with the workbook's Week 5 answer key. Record any correction in your own words and connect it to one observed record from this lab. Do not replace your explanation with a copied definition.

## Step 12: Reflection prompts

Use all three workbook prompts:

**R1. Which telemetry signal gave you the fastest answer?**

Describe which operational question you were trying to answer, the record you found, and how another signal confirmed or qualified the result.

**R2. What information would you regret not capturing during an incident?**

Name a specific missing field or relationship. Explain why a later investigation could not reliably reconstruct it and where your contract now requires it.

**R3. How would you prevent high-cardinality telemetry from becoming noisy or expensive?**

Use your own metrics and resource attributes as examples. Discuss allowed dimensions, unique identifiers, sampling, retention, and what diagnostic capability you would preserve when reducing volume.

## Step 13: Weekly retrospective

Answer the same four questions used in the earlier weeks:

1. **What became clearer this week?**
2. **What was harder than expected?**
3. **What would I do differently in a production system?**
4. **What artifact from this week best demonstrates growth?**

Include an observation from the failed request or instrumentation revision. Keep the retrospective separate from the contract so the contract remains useful to someone who was not part of your learning process.

## Step 14: Repository closeout

Update the root README's weekly roadmap and documentation index with links to the Week 5 overview and required contract. Verify relative links from their actual repository locations.

### Completion checklist

- [ ] I completed the required reading/tutorial selections and recorded them.
- [ ] I can distinguish logs, metrics, traces, health checks, and alerts.
- [ ] The issuance workflow validates input, queries a real lab database, and creates the declared result.
- [ ] Structured requested, succeeded, and database-failure events are exported.
- [ ] Successful issues, failures, and duration are measured, with attempts and count reconciliation documented.
- [ ] Successful traces cover HTTP -> validation -> database query -> policy creation.
- [ ] Failed traces identify the actual failed dependency and do not imply creation succeeded.
- [ ] Deployment version and environment appear on logs, metrics, and traces.
- [ ] Runtime identity matches the artifact/deployment evidence.
- [ ] A real database fault was injected and all five investigation questions were answered from telemetry.
- [ ] Instrumentation was refined, evidence was compared, and all five answers were verified again.
- [ ] Database settings were restored and a recovered success was observed.
- [ ] The lab runs end-to-end, and I can explain the result and at least one tradeoff.
- [ ] The Observability Contract defines deployment attributes, correlation, workflow metrics, and error context.
- [ ] Existing locked restore, build, and test checks pass after changes.
- [ ] Evidence contains no credentials, raw connection strings, or real customer data.
- [ ] I completed the knowledge check without consulting the answer key first.
- [ ] I answered all three reflection prompts and wrote a brief weekly retrospective.
- [ ] Documentation links work, and the required deliverable is committed to the repository.

Review changes before staging:

```powershell
git status
git diff --check
git diff
```

Stage your actual files deliberately: Week 5 documents, instrumentation changes, package/lock changes, README/index updates, and any deployment configuration changes. Review `git diff --cached` before committing. Do not stage private configuration or unrelated work.

```powershell
# Run after staging the intended files and reviewing the staged diff.
git commit -m "Complete week 5 observability lab"
git push
```

If you have maintained weekly milestone tags, add `week-05` after confirming it does not already exist. A tag is optional; the committed Observability Contract is required.

**Week 5 is complete when another engineer can use your contract and emitted telemetry to explain a failed issuance request, identify its deployment, and follow the evidence to its timing and failed component.**

---

## Appendix: Workbook coverage review

This walkthrough was checked against the supplied workbook's **Week 5 pages 16-18**, including the learning outcomes, all six hands-on requirements, required deliverable, completion checklist, knowledge check, reflections, and retrospective. The review below checks the walkthrough's coverage; it does not certify that a reader has performed the exercises.

| Workbook requirement | Walkthrough coverage | Evidence the reader must produce | Review result |
| --- | --- | --- | --- |
| Distinguish logs, metrics, traces, health checks, alerts | Steps 1, 11 | Completed signal table and own-word answers | Covered |
| Instrument ASP.NET Core using OpenTelemetry concepts | Steps 3-6 | Export setup and real workflow signals | Covered |
| Correlate telemetry with deployment identity | Steps 3, 7, 10 | Resource identity matching manifest/deployment evidence | Covered |
| Answer concrete operational questions from telemetry | Steps 8-10 | Five-question tables and independent review | Covered |
| Reference: OpenTelemetry .NET | Required reading section | Reading record | Covered |
| Getting Started tutorial, 60-90 min | Required reading section | Tutorial completion and observations | Covered |
| Microsoft .NET observability guidance, 45 min | Required reading section | Reading notes | Covered |
| Lab 1: structured requested, succeeded, database-failure logs | Step 4 | Exported events at real execution boundaries | Covered |
| Lab 2: total policy issues, failures, duration metrics | Step 5 | Successful-issue/failure counters, duration histogram, definitions and deltas | Covered |
| Lab 3: HTTP -> validation -> DB query -> creation traces | Steps 2, 6 | Healthy trace tree with real database operation | Covered |
| Lab 4: deployment version and environment on telemetry | Steps 3, 7 | Identity on all three signal resources | Covered |
| Lab 5: break DB; answer request, reason, component, deployment, duration | Step 8 | Actual DB failure and five evidence-backed answers | Covered |
| Lab 6: improve until all five questions are answerable | Step 9 | Gap/refinement record and repeated five-question investigation | Covered |
| Required Observability Contract: deployment attributes | Steps 1, 7, 10 | Completed authoritative attribute definitions | Covered |
| Required Observability Contract: request correlation | Steps 1, 4, 6, 10 | Lookup and propagation policy with example | Covered |
| Required Observability Contract: critical workflow metrics | Steps 1, 5, 10 | Definitions, units, dimensions, recording boundaries | Covered |
| Required Observability Contract: error context | Steps 1, 4, 6, 8, 10 | Safe classification, component, status, correlation | Covered |
| Completion: readings/tutorials complete | Steps 3, 14 and reading section | Reading record and checklist | Covered |
| Completion: end-to-end run and explanation | Steps 8-10, 14 | Baseline, fault, revision, recovery evidence | Covered |
| Completion: required deliverable committed | Step 14 | Repository commit containing contract | Covered |
| Completion: explain at least one tradeoff | Steps 6, 10, 14 | Recorded choice and diagnostic/operational consequence | Covered |
| Completion: knowledge check before answer key | Step 11 | Six original answers followed by corrections if needed | Covered |
| Completion: brief weekly retrospective | Step 13 | Four retrospective responses | Covered |
| Reflection: fastest signal | Step 12, R1 | Signal and investigation example | Covered |
| Reflection: information regretted during incident | Step 12, R2 | Missing-context analysis | Covered |
| Reflection: high cardinality, noise, expense | Step 12, R3 | Specific policy tied to own telemetry | Covered |
| Knowledge questions 1-6 | Step 11 | All six prompts preserved without supplied answers | Covered |
| Retrospective: clearer, harder, production changes, growth artifact | Step 13 | All four prompts preserved | Covered |

**Review outcome:** No Week 5 requirement from pages 16-18 is omitted. The minimal issuance workflow, attempts counter, recovery check, and operating-policy prompts support the required exercises. Centralized telemetry hosting, R2 implementation, production dashboards, and alert delivery remain optional extensions rather than prerequisites.
