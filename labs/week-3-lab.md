## Week 3 Lab

According to the workbook, **Week 3: Continuous Deployment and Artifact Promotion** is about promoting **one immutable artifact** through DEV and QA while separating **deployment execution** from **environment readiness**. The target is 6–7 hours, with a final portfolio artifact consisting of a **“Deployment Success vs. Environment Readiness” diagram** plus a small **deployment metadata schema**.

The lab sequence is:

1. Extend the workflow to **Build → Artifact → Deploy DEV → DEV health check → Approval → Deploy QA → QA health check**.
2. Build the application **only once**.
3. Inject environment-specific configuration during deployment rather than baking it into the artifact.
4. Capture provenance: artifact ID, commit, branch, build time, deployment time, environment, and validation result.
5. Intentionally test four bad deployments: wrong connection string, missing database, invalid configuration, and unavailable dependency.
6. For each failure, separately record **“Did deployment execution succeed?”** and **“Is the environment ready?”**.

That last distinction is really the core lesson of the week. It also connects directly to the delivery-system distinction you worked through in Week 1: a pipeline can execute successfully without producing a usable system.

### How I suggest we work through Week 3

We'll use the same approach as the previous weeks: **you do the reasoning first, then we'll implement it, deliberately break it, examine what happened, and finish with the portfolio artifact and knowledge check.**

Before we touch the pipeline, I want to establish your current mental model.

Imagine your Week 2 workflow currently produces:

```
Commit
  ↓
Restore
  ↓
Compile
  ↓
Unit Test
  ↓
Package
  ↓
PolicyService-0.2.123.zip
```

Now we want this:

```
                         ┌───────────────┐
                         │ Build once    │
                         │               │
Source ──► CI ──────────►│ Artifact      │
                         │ 0.2.123       │
                         └───────┬───────┘
                                 │
                                 ▼
                         Deploy to DEV
                                 │
                                 ▼
                         Validate DEV
                                 │
                                 ▼
                             Approval
                                 │
                                 ▼
                          Same artifact
                                 │
                                 ▼
                          Deploy to QA
                                 │
                                 ▼
                          Validate QA
```

There should **not** be another compile/package operation between DEV and QA. The bytes entering QA should be the same bytes that were validated in DEV.

## First exercise: define the boundaries

Before we write YAML, answer these from your current understanding. Don't look at the Week 3 answer key.

**1.** Suppose the pipeline successfully copies `PolicyService-0.2.123.zip` onto the DEV machine, replaces the previous application, and starts the process. The application then returns HTTP 500 because its database connection string is wrong.

Would you describe:

* the **deployment execution** as successful or failed?
* the **environment readiness** as successful or failed?

Explain why.

**2.** Suppose DEV and QA need different database connection strings. Where do you think those values should live?

Would you put them:

* inside the artifact during build,
* in separate environment configuration supplied during deployment,
* somewhere else?

Explain your reasoning.

**3.** We built commit:

```
a8f31c72
```

and produced:

```
PolicyService-0.2.123.zip
```

DEV passed validation.

When we're ready for QA, why would this be weaker?

```
git checkout a8f31c72
dotnet publish
deploy newly-created output to QA
```

than simply promoting:

```
PolicyService-0.2.123.zip
```

to QA?

**4.** Finally, imagine the deployment tool says:

```
Deployment completed successfully.
```

What evidence would you personally require before being willing to say:

> "DEV is ready for use."

Don't worry yet about what the workbook thinks the answer is. I want your current definition of **READY**.

Answer those four, and we'll use your answers to design the Week 3 deployment workflow before we change any code.

* * *

**#1**
Deployment Execution was a success if the only issue is a connection string. The application deployed and started but the the Environment readiness was a failure because we are not able to actually use the application in the way that it was intended

```
Artifact already exists
        ↓
Deployment copies/installs artifact
        ↓
Application process starts
        ↓
Deployment execution: SUCCESS
        ↓
Post-deployment validation
        ↓
Database connection fails
        ↓
Environment readiness: FAIL
```

**#2** 
The connection string values could live in a few places, but probably in an environment specific configuration file

**#3** 
Ideally, we want the exact bit shaped package to move from one environment to the next. Outside influences could change the build result that happened after the dev package was deployed

**#4** 
For our PolicyService, I'd separate validation into something like:

```
Deployment Execution
    Artifact installed
    Application started
             │
             ▼
Environment Validation
    1. Application responds
    2. Configuration is valid
    3. Required database is reachable
    4. Required dependencies are reachable
    5. Critical policy workflow succeeds
             │
             ▼
          READY
```

That becomes especially useful because the Week 3 lab explicitly wants us to inject **a wrong connection string, missing database, invalid configuration, and unavailable dependency**, then independently record whether deployment succeeded and whether the environment was ready.

## Next step: design our Week 3 pipeline

Given what you built in Week 2, I think our target should be:

```
CI
─────────────────────────────────
Checkout
Restore
Compile
Unit Tests
Package
Publish Artifact
        │
        │ PolicyService-x.x.x
        ▼

DEV Deployment
─────────────────────────────────
Download SAME artifact
Supply DEV configuration
Deploy
Start service
DEV validation
        │
        ▼

Promotion Gate
─────────────────────────────────
Approval
        │
        ▼

QA Deployment
─────────────────────────────────
Download SAME artifact
Supply QA configuration
Deploy
Start service
QA validation
```

There should be **no restore, compile, test, or package step below the artifact boundary**.

Since you're doing this in a public personal repository and we already discovered you can't create local VMs, we don't need real DEV and QA infrastructure to learn the architecture. We can build a controlled simulation that still demonstrates the important properties: separate environments, separate configuration, one artifact, promotion, validation, and failure injection.

Before we implement it, I want you to make one design decision.

Suppose our validation has these four checks:

| Check | What it proves |
| --- | --- |
| `/health` responds | Process/application is running |
| Configuration check | Required configuration was supplied |
| Database check | Application can actually reach its data dependency |
| `POST /policies` smoke test | Critical business workflow functions |

**Which of those would you make blocking for promotion from DEV → QA, and why?**

You don't have to make all four blocking. Think about the difference between **“something is imperfect”** and **“we have evidence that promoting this environment would be unsafe or meaningless.”**

* * *

Health Check should be required. If we can't interact with the application then there is not much point to promoting it.

Configuration check is also important, this says "required configuration", so this implies configuration that is required to run the application. There may be edge cases here where we can promote without all configuration, but this would have to be on a case by case basis. 

Database check is also a case by case basis. There may be parts of the application that we want to test in QA that do not require full DB access. Like if we wanted to test a 3rd party integration or login process that did not depend on persisted state. 

Post /policies smoke test would also be on a case by case basis. This would mainly depend on the standards of the QA team for validating a workable environment. We can test some features of the applicaiton in QA without needing all core business workflows available. 

We would probably not promote a build with any of these failures to production in this state, but in some situations we may want to test changes even if the build is not fully functional.

For our Week 3 design, I’d capture your reasoning like this:

| Validation | Default behavior | Why |
| --- | --- | --- |
| Application health | **Blocking** | If the application cannot be interacted with at all, promotion provides little useful value. |
| Required configuration | **Blocking when required for intended testing** | Missing mandatory configuration can make the environment invalid, while optional feature configuration may not matter for a particular QA cycle. |
| Database connectivity | **Conditional** | Required for database-dependent workflows, but some targeted testing may legitimately not require it. |
| `POST /policies` smoke test | **Conditional** | Strong evidence that a core workflow works, but not every QA purpose necessarily requires policy issuance. |

That leads to a better model than simply saying:

```
Every check passed = READY
Anything failed = NOT READY
```

Instead:

```
Deployment completed
        │
        ▼
Run validation checks
        │
        ├── Required baseline checks
        │       └── failure => NOT READY
        │
        └── Scenario-specific checks
                └── failure => degraded / unsuitable
                    for that test scope
```

This is also why your production distinction matters. **QA and production can have different readiness policies.** QA exists partly to investigate incomplete or questionable states, so intentionally accepting some degradation can be legitimate. Production usually needs a much stricter gate.

The important architectural requirement is that this exception is **explicit**, rather than someone simply ignoring a red pipeline. For example:

```
Target: QA

Required:
  application-health: PASS
  required-configuration: PASS

Requested test capabilities:
  database: NOT REQUIRED
  policy-issuance: NOT REQUIRED

Result:
  READY for integration-only testing
```

Versus:

```
Target: QA

Requested test capabilities:
  policy-issuance: REQUIRED

Results:
  application-health: PASS
  database: FAIL
  policy-issuance: FAIL

Result:
  NOT READY
```

Same environment state, different testing purpose, different meaningful readiness result.

That kind of reasoning will become particularly useful later in the workbook when you get to the Environment Certification Service in Week 8. Week 3 is laying the conceptual groundwork for it.

## Let's build the Week 3 implementation

Because we don't have persistent DEV and QA machines, I suggest we simulate deployment using the GitHub-hosted runner while preserving the architecture we're actually trying to learn.

Our workflow will become:

```
CI / BUILD
──────────────────────────────
Restore
Compile
Unit Test
Package
Publish immutable artifact
              │
              ▼
DEV
──────────────────────────────
Download artifact
Inject DEV configuration
"Deploy" artifact
Start PolicyService
Run health/readiness validation
Record deployment metadata
              │
              ▼
PROMOTION
──────────────────────────────
Approval / promotion decision
              │
              ▼
QA
──────────────────────────────
Download SAME artifact
Inject QA configuration
"Deploy" artifact
Start PolicyService
Run health/readiness validation
Record deployment metadata
```

The important thing is that DEV and QA jobs will **download the exact artifact produced by the build job**. Neither is allowed to call `dotnet publish`.

### First technical change: make the application deployable

Before changing the workflow, we need to check how `PolicyService` currently gets its configuration.

For Week 3, I want something roughly like:

```JSON
{
  "EnvironmentName": "LOCAL",
  "ConnectionStrings": {
    "PolicyDatabase": ""
  }
}
```

with environment values supplied at deployment/runtime rather than baked into the package.

ASP.NET Core already gives us a nice hierarchy for this, so our eventual deployment can inject values such as:

```PowerShell
$env:EnvironmentName = "DEV"
$env:ConnectionStrings__PolicyDatabase = "..."
```

and later:

```PowerShell
$env:EnvironmentName = "QA"
$env:ConnectionStrings__PolicyDatabase = "..."
```

without changing the artifact.

So let's start with the application rather than the YAML.

`Program.cs` already reads an environment-specific value through `IConfiguration`, and your `/health` endpoint exposes both the environment name and assembly version. That gives us two things we need for deployment validation: **environment identity** and **artifact identity**.

Your `appsettings.json` currently supplies a default environment display name.

There is one typo worth fixing now before we build on it:

For Week 3, I would make the smallest possible change rather than introducing a real database immediately.

### Step 1: make configuration explicit

Update `appsettings.json` to this:

```JSON
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  "EnvironmentSettings": {
    "DisplayName": "Default"
  },

  "ConnectionStrings": {
    "PolicyDatabase": ""
  }
}
```

Then update the health endpoint:

```C#
app.MapGet("/health", (IConfiguration configuration) =>
{
    var version = Assembly
        .GetExecutingAssembly()
        .GetName()
        .Version?
        .ToString();

    return Results.Ok(new
    {
        status = "Healthy",
        environment = configuration["EnvironmentSettings:DisplayName"],
        version,
        timestamp = DateTimeOffset.UtcNow
    });
});
```

At this stage, the connection string exists as a **configuration contract**, but we're deliberately not putting a real DEV or QA value into the artifact.

That matters because our artifact can contain:

```JSON
"PolicyDatabase": ""
```

while deployment supplies:

```
DEV → DEV-specific connection string
QA  → QA-specific connection string
```

The package stays unchanged.

### Step 2: verify environment-variable overriding locally

ASP.NET Core maps double underscores in environment variables to configuration hierarchy, so:

```PowerShell
$env:EnvironmentSettings__DisplayName = "DEV"
dotnet run --project .\src\PolicyService
```

should override:

```JSON
"DisplayName": "Default"
```

Then hitting `/health` should return something conceptually like:

```JSON
{
  "status": "Healthy",
  "environment": "DEV",
  "version": "0.2.123.0",
  "timestamp": "..."
}
```

You can then switch it without rebuilding:

```PowerShell
$env:EnvironmentSettings__DisplayName = "QA"
dotnet run --project .\src\PolicyService
```

and the same compiled application should now identify itself as QA.

That little experiment is actually an important Week 3 demonstration:

```
Same artifact
    +
Different runtime configuration
    =
Different environment behavior
```

### One architectural detail before we go further

Right now `/health` always returns:

```
status = Healthy
```

even if the application has bad configuration.

I don't want to change that quite yet.

There are actually two useful concepts hiding here:

```
/health
    "Is the application process alive?"

/readiness
    "Can this environment perform the work we require?"
```

If we make `/health` test every downstream dependency, we'll blur **liveness** and **readiness** together. Given the distinction you made earlier about QA being useful even when some capabilities are unavailable, separating them will give us a much better Week 3 experiment.

So let's do this incrementally.

First, make those two configuration changes and run this test:

```PowerShell
$env:EnvironmentSettings__DisplayName = "DEV"
dotnet run --project .\src\PolicyService
```

Then call `/health`.

Send me the response you get. After that we'll add **`/readiness`**, which is where we'll start modeling blocking versus conditional checks.


**The Tension**
You established (principle #2): environment-specific values must not be baked into the immutable artifact; they should be injected at deployment/runtime.

Yet you currently have two files inside the service project:

appsettings.Development.json → DisplayName = "DEV"
appsettings.QA.json → DisplayName = "QA"
When you dotnet publish, those files get copied into the published output. ASP.NET Core auto-loads appsettings.{ASPNETCORE_ENVIRONMENT}.json. So today, the DEV and QA values are physically shipped inside the artifact, and the environment is selected by a flag — which is exactly the pattern principle #2 says to move away from.

Meanwhile the plan is to prove the opposite model works:

````
$env:EnvironmentSettings__DisplayName = "DEV"
dotnet run --project .\src\PolicyService
````

...injecting the value from outside the artifact.

**Decision **
Keep appsettings.Development.json and appsettings.QA.json as schema placeholders (structure present, real values blank), inject real DEV/QA values externally, and add explicit validation later to get the loud-fail behavior.

Both files are now schema placeholders. Here's the state going into the experiment:

|Source|EnvironmentSettings:DisplayName|
|--|--|
|appsettings.json:10|"Default"|
|appsettings.Development.json:8|"" (blank placeholder)|
|appsettings.QA.json:8|"" (blank placeholder)|

And launchSettings.json:9 pins ASPNETCORE_ENVIRONMENT=Development for dotnet run, on http://localhost:5006.

**Before you run anything — predict**
ASP.NET Core config layers load in this order, each overriding the previous:

appsettings.json
appsettings.{ASPNETCORE_ENVIRONMENT}.json
Environment variables (__ maps to :, so EnvironmentSettings__DisplayName → EnvironmentSettings:DisplayName)
Command-line args
Now predict the environment field in the /health response for each case. Don't run it yet — commit to an answer:

Experiment A — no env var set, plain dotnet run:

````
dotnet run --project .\src\PolicyService
# GET http://localhost:5006/health  → environment = ?
````

Experiment B — inject DEV:

````
$env:EnvironmentSettings__DisplayName = "DEV"
dotnet run --project .\src\PolicyService
# environment = ?
````

Experiment C — inject QA (remember env vars persist in a PowerShell session):

````
$env:EnvironmentSettings__DisplayName = "QA"
dotnet run --project .\src\PolicyService
# environment = ?
````

Here's the trap I want you to reason through for Experiment A: many people would say it reports "Default". Look at the precedence table again and tell me what it actually reports — and connect your answer back to the fail-silent point we just discussed.

Three questions for you before we run:

1) What does A report, and why is that result a live demonstration of the "empty file present ≠ fail loud" nuance?
2) What do B and C report, and what does that prove about promoting one artifact across environments?
3) After Experiment C, if you opened a new terminal and ran dotnet run again with no env var, what would you get — and what does that teach you about where injected config actually lives?

---

**Why Experiment A was blank**
Walk the layers for ASPNETCORE_ENVIRONMENT=Development:

|Order	|Source	|Value written|
|---|---|---|
|1	|appsettings.json|	"Default"|
|2	|appsettings.Development.json|	"" ← overrides #1|
|3	|env var (not set in A)	|—|

Layer 2 loaded after layer 1 and won. Your blank placeholder file didn't "do nothing" — it actively overwrote "Default" with "". The app then started perfectly happy serving a blank environment name.

That is the fail-silent trap made real: a present-but-empty config file produced a silently wrong value, and nothing complained. This is your concrete proof that "loud failure" must come from validation, not from file presence. File that observation — it's the justification for the /readiness work.
---
**Why did the app default to the dev config when none what explicitly set?**

Because the "default" wasn't a default at all — something explicitly set it, and that something is a local-development-only file that will not exist in your pipeline. This is one of the most important things to understand before you build the DEV/QA stages, so let's be precise.

What actually selected Development
There are two different config concepts in play, and they're easy to conflate:

|Concept	|Controlled by	|What it does|
|--|--|--|
|Which environment	|ASPNETCORE_ENVIRONMENT	|Selects which appsettings.{env}.json loads + the "Hosting environment" banner|
|Config values	|the layered appsettings files + env vars	|The actual settings the app reads|

When you ran dotnet run, the log said:
````
Using launch settings from .\src\PolicyService\Properties\launchSettings.json...
...
Hosting environment: Development
````

That first line is the answer. launchSettings.json:9 has:

````
"environmentVariables": {
  "ASPNETCORE_ENVIRONMENT": "Development"
}
````

So dotnet run injected ASPNETCORE_ENVIRONMENT=Development for you, which caused appsettings.Development.json to load (the blank override), which is why Experiment A came back empty. You never "left it unset" — dotnet run set it on your behalf.

**Key Concept**

Right now the injected env var is the real lever, and ASPNETCORE_ENVIRONMENT is effectively labeling — because you deliberately blanked the DEV/QA files. But be careful about over-generalizing: ASPNETCORE_ENVIRONMENT is always doing real selection work (it chooses which file loads and drives framework behavior like the developer exception page vs. the production error handler). It only looks like pure labeling because the files it selects are empty. The moment you put a non-secret structural default into appsettings.QA.json, that lever starts doing configuration work again.

So the precise statement is: "In this design, environment-specific values are injected, and ASPNETCORE_ENVIRONMENT selects behavior + labels the environment. Secrets and per-environment values never live in the artifact." That's a defensible architecture you can write down.

That is exactly the separation Week 3 wants: the artifact is inert; the environment supplies identity and secrets.

---
**Next: the /readiness endpoint**
You already did the hard design thinking in your Week 3 notes:

|Check	|Classification|
|--|--|
|Application health (liveness)	|blocking|
|Required configuration	|blocking when required for the intended scope|
|Database connectivity	|conditional|
|POST /policies smoke test	|conditional|

Now translate that model into an actual endpoint contract. Before I write any code, reason through these three design decisions — they're the real leadership choices:

1) Status code semantics
    - /health returns 200 Healthy. What should /readiness return when a blocking check fails? When only a conditional check fails but all blocking checks pass? Should a failed conditional check even change the HTTP status, or just appear in the body? (Think about what a load balancer or a pipeline gate does with the status code.)

2) How "conditional" gets decided
    - A check like "database connectivity" is blocking in one QA cycle and irrelevant in another. Where does that decision come from at deploy time — hardcoded, ASPNETCORE_ENVIRONMENT, or an injected config flag? Which keeps the artifact inert?

3) Response shape 
    - What does the JSON body need to contain so that a human and an automated gate can both tell (a) is the app alive, (b) which checks ran, (c) which passed/failed, and (d) which failures are promotion-blocking vs. informational?

---
**Responses**

**1) HTTP status codes for readiness — the nuance**

One opinion — "HTTP codes describe the server's ability to communicate, not the application's internal execution" — is the REST-purist view, and it's correct for your business API. When someone calls POST /policies and the policy is created, 201 describes the outcome of that request. You should never return 500 just because some unrelated subsystem is unhealthy. Hold onto that instinct for your real endpoints.

But /readiness is a different kind of endpoint, and here's the distinction that reconciles everything:

/readiness is not a business API. It is a machine-facing control signal consumed by dumb infrastructure — load balancers, orchestrators (Kubernetes readiness probes), and pipeline gates. Most of those consumers only read the status code. They do not parse your JSON body.

Now the key insight that should satisfy your instinct: returning 503 Service Unavailable when not ready is not "abusing HTTP to report app internals." Look at what 503 officially means:

503 Service Unavailable — the server is currently unable to handle the request.

That is literally the readiness statement. The server process is alive enough to answer, but it's declaring "don't route work to me yet." So 503 is the HTTP-native, semantically-correct way to say "not ready" — it's describing the server's ability to serve, which is exactly your principle, just applied to "serve real traffic" instead of "answer this ping."

So the reconciled contract:

|Situation	|Status code	|Why|
|--|--|--|
|All blocking checks pass	|200	|Server can serve its intended workload|
|Any blocking check fails	|503	|Server is up but not fit to serve — HTTP-native "not ready"|
|A conditional check fails but blocking pass	|200	|App is ready for this cycle's intended scope; failure is informational in the body|

The reason a dumb probe needs the code in the status line and not the body: a Kubernetes readiness probe or an Azure Load Balancer health probe won't deserialize your checks[] array — it flips traffic on/off based purely on 200 vs not-200. Your rich body is for the humans and the pipeline gate; the status code is for the dumb probe. You serve both audiences at once.

**2) Injected config flag**

The reason a check is required comes from what QA intends to test this cycle, which is a deployment-time decision, not a property of the code. Putting it in an injected flag keeps the artifact inert. Lock it in.

**3) Response shape — good, with one refinement**

Your array-of-checks idea is solid. One refinement: you bundled three different concepts into a single type field (promotion-blocking, informational, live-check). Those are actually three orthogonal axes, and separating them makes the body far easier to sort/filter (your own stated goal):

|Axis	|Example values	|Answers|
|--|--|--|
|what is checked	|configuration, database, policy-smoke-test	|the identity of the check|
|category	|config, dependency, liveness	|what class of thing it inspects|
|enforcement	|blocking, conditional	|does failure block promotion?
|result	|Pass, Fail, Skipped	|the outcome|

Keeping enforcement separate from result is what lets a gate compute "any blocking check with result=Fail → stop the promotion" trivially.

**Proposed /readiness contract**

````
{
  "status": "Ready | NotReady",
  "environment": "DEV",
  "timestamp": "2026-09-18T...Z",
  "checks": [
    {
      "name": "required-configuration",
      "category": "config",
      "enforcement": "blocking",
      "result": "Pass",
      "detail": "EnvironmentSettings:DisplayName = 'DEV'"
    }
  ]
}
````

---
**Incremental build — slice 1**

Rather than build all four checks at once, let's lay the skeleton first: the endpoint, the response shape, the 200/503 logic, and a single blocking configuration check. Once you can see it flip between Ready/NotReady, we'll add the conditional DB and smoke-test checks driven by injected flags (slice 2).

Add these into your program.cs file:

````
app.MapGet("/readiness", (IConfiguration configuration) =>
{
    var displayName = configuration["EnvironmentSettings:DisplayName"];

    var checks = new List<ReadinessCheck>
    {
        // Blocking: the environment must know its own identity to be usable.
        new(
            Name: "required-configuration",
            Category: "config",
            Enforcement: "blocking",
            Result: string.IsNullOrWhiteSpace(displayName) ? "Fail" : "Pass",
            Detail: string.IsNullOrWhiteSpace(displayName)
                ? "EnvironmentSettings:DisplayName is not set"
                : $"EnvironmentSettings:DisplayName = '{displayName}'")
    };

    var blockingFailed = checks.Any(c =>
        c.Enforcement == "blocking" && c.Result == "Fail");

    var response = new
    {
        status = blockingFailed ? "NotReady" : "Ready",
        environment = displayName,
        timestamp = DateTimeOffset.UtcNow,
        checks
    };

    return Results.Json(
        response,
        statusCode: blockingFailed
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK);
});
````

````
record ReadinessCheck(
    string Name,
    string Category,
    string Enforcement,
    string Result,
    string Detail);
````
----
**Test it yourself — predict first**
Two runs will exercise both branches of the blocking logic. Predict each before running.

R1 — DEV injected (config present):

````
$env:EnvironmentSettings__DisplayName = "DEV"
dotnet run --project .\src\PolicyService
# then, in another pwsh tab:
Invoke-RestMethod -Uri http://localhost:5006/readiness -Method Get -SkipHttpErrorCheck | ConvertTo-Json
````

R2 — nothing injected (blank placeholder wins):

````
# fresh terminal so the env var from R1 is gone
Remove-Item Env:EnvironmentSettings__DisplayName -ErrorAction SilentlyContinue
dotnet run --project .\src\PolicyService
Invoke-RestMethod -Uri http://localhost:5006/readiness -Method Get -SkipHttpErrorCheck | ConvertTo-Json
````

**Predict for each:**

HTTP status code (200 or 503)
status field (Ready / NotReady)
the result of the required-configuration check

Two things to notice while you're there:

1) Use -SkipHttpErrorCheck — otherwise Invoke-RestMethod throws on the 503 and you won't see the body. That's itself a lesson: your rich body is only useful to a caller that chooses to read it past the status code.
2) Compare /health vs /readiness in the R2 (blank) case. What does each report, and why is that difference the entire point of separating liveness from readiness?

---

Let's go ahead and execute this experiment: 

````
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue; Start-Sleep -Seconds 1; $env:EnvironmentSettings__DisplayName = "DEV"; Start-Sleep -Milliseconds 200; dotnet run --project .\src\PolicyService

````

````
$r = Invoke-WebRequest -Uri http://localhost:5006/readiness -Method Get -SkipHttpErrorCheck; "HTTP $($r.StatusCode)"; $r.Content | ConvertFrom-Json | ConvertTo-Json -Depth 5

````
R1 confirmed: HTTP 200, Ready, config check Pass. Now R2 — same artifact, nothing injected:


````
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue; Start-Sleep -Seconds 1; Remove-Item Env:EnvironmentSettings__DisplayName -ErrorAction SilentlyContinue; dotnet run --project .\src\PolicyService
````

````
$h = Invoke-WebRequest -Uri http://localhost:5006/health -Method Get -SkipHttpErrorCheck; "HEALTH  -> HTTP $($h.StatusCode)"; $r = Invoke-WebRequest -Uri http://localhost:5006/readiness -Method Get -SkipHttpErrorCheck; "READINESS -> HTTP $($r.StatusCode)"; $r.Content | ConvertFrom-Json | ConvertTo-Json
````

Both confirmed. And R2 produced the single most important artifact of your entire week:

|Endpoint	|R2 (blank config)	|Meaning|
|--|--|--|
|/health	|HTTP 200	|The process is alive — deployment executed successfully|
|/readiness	|HTTP 503	|The environment is not fit for its intended work|


Read that carefully. Same running process. Same artifact. One says "I'm here," the other says "don't send me work." That is literally the picture behind your required Week 3 deliverable — "Deployment Success vs. Environment Readiness." You now have a live, reproducible demonstration of it: the app started (deploy = SUCCESS) but the environment isn't ready (readiness = FAIL). Keep this R2 output; it's your diagram's real-world evidence.

---

**Slice 2 — the conditional checks**


Now we model the part your notes called conditional: database connectivity and the POST /policies smoke test. The mechanism, from your own answer #2: an injected config flag decides whether a conditional check is enforced this cycle.

Here's the enforcement model I want to implement, because it makes the gate logic trivial:

- Each check reports the effective enforcement for this deployment.
- required-configuration → always blocking.
- database → enforcement = RequireDatabase ? "blocking" : "conditional"; if not required, result = "Skipped".
- policy-smoke-test → enforcement = RequirePolicySmokeTest ? "blocking" : "conditional"; if not required, Skipped.
- Gate rule stays: 503 iff any check has enforcement="blocking" AND result="Fail".

That one rule cleanly expresses your key idea: "QA may intentionally accept a partially functional environment" — a skipped conditional check never blocks, but the same check promoted to required this cycle does.

*For now, we will code a local db connection but later will swap over to a github provisioned in memory db. If there are any issues spinning up a local db, just skip this step and impelement the github db later on for this tests
 

 Let's test out a real connection, which is the richest for the failure experiments. Let me see what's already available on your machine before installing anything, so we pick the lightest path that still lets you simulate an unavailable dependency.

LocalDB is installed (SqlLocalDB.exe) and so is sqlcmd — no Docker needed. LocalDB is the perfect fit for this lab: it's a file-based, on-demand SQL Server instance, and crucially you can stop it to simulate an unavailable dependency. Let me start it and create a lab database.

````
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "IF DB_ID('PolicyDb') IS NULL CREATE DATABASE [PolicyDb]; SELECT name FROM sys.databases WHERE name = 'PolicyDb';"
````

Another option is just to manually create the PolicyDb database on your local db and use your own connection string later on in the lab. 

The connection string we will use is: 

````
Server=(localdb)\MSSQLLocalDB;Database=PolicyDb;Integrated Security=true;Connect Timeout=3
````

Use this script to add the sql client package to our project: 

````
dotnet add .\src\PolicyService\PolicyService.csproj package Microsoft.Data.SqlClient
````

Now test and make sure we can still build, then we will move on to the rest of slice 2 code. 

**Slice 2 Code Changes**

Update our `readiness` endpoint signature in program.cs:

````
app.MapGet("/readiness", async (
    IConfiguration configuration,
    PolicyService.Services.PolicyService policyService) =>
{
````

Add these steps within the `readiness` endpoint:

````
// Conditional: database connectivity is only enforced when this
    // deployment scope declares it required (injected flag).
    var requireDatabase = configuration.GetValue<bool>("Readiness:RequireDatabase");
    if (!requireDatabase)
    {
        checks.Add(new(
            Name: "database",
            Category: "dependency",
            Enforcement: "conditional",
            Result: "Skipped",
            Detail: "Database not required for this deployment scope"));
    }
    else
    {
        var (result, detail) = await CheckDatabaseAsync(
            configuration.GetConnectionString("PolicyDatabase"));
        checks.Add(new(
            Name: "database",
            Category: "dependency",
            Enforcement: "blocking",
            Result: result,
            Detail: detail));
    }

    // Conditional: the core policy workflow smoke test, likewise only
    // enforced when this deployment scope declares it required.
    var requireSmokeTest = configuration.GetValue<bool>("Readiness:RequirePolicySmokeTest");
    if (!requireSmokeTest)
    {
        checks.Add(new(
            Name: "policy-smoke-test",
            Category: "workflow",
            Enforcement: "conditional",
            Result: "Skipped",
            Detail: "Policy smoke test not required for this deployment scope"));
    }
    else
    {
        var (result, detail) = RunPolicySmokeTest(policyService);
        checks.Add(new(
            Name: "policy-smoke-test",
            Category: "workflow",
            Enforcement: "blocking",
            Result: result,
            Detail: detail));
    }
````

And we will add these helper methods further down in the file:
````

static async Task<(string Result, string Detail)> CheckDatabaseAsync(
    string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return ("Fail", "ConnectionStrings:PolicyDatabase is not set");
    }

    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return ("Pass", "Opened a connection to the policy database");
    }
    catch (Exception ex)
    {
        return ("Fail", $"Database connection failed: {ex.Message}");
    }
}

static (string Result, string Detail) RunPolicySmokeTest(
    PolicyService.Services.PolicyService service)
{
    try
    {
        var created = service.CreatePolicy(
            new CreatePolicyRequest("readiness-probe", "smoke-test"));
        var fetched = service.GetPolicy(created.Id);

        return fetched is not null
            ? ("Pass", $"Created and retrieved policy {created.Id}")
            : ("Fail", "Created policy could not be retrieved");
    }
    catch (Exception ex)
    {
        return ("Fail", $"Policy smoke test threw: {ex.Message}");
    }
}
````

Now build and test this endpoint!

````
Get-Process dotnet, PolicyService -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue; Start-Sleep -Seconds 1
$env:EnvironmentSettings__DisplayName = "DEV"
$env:Readiness__RequireDatabase = "true"
$env:Readiness__RequirePolicySmokeTest = "true"
$env:ConnectionStrings__PolicyDatabase = "Server=(localdb)\MSSQLLocalDB;Database=PolicyDb;Integrated Security=true;Connect Timeout=3"
dotnet run --project .\src\PolicyService
````

````
$r = Invoke-WebRequest -Uri http://localhost:5006/readiness -Method Get -SkipHttpErrorCheck; "READINESS -> HTTP $($r.StatusCode)"; $r.Content | ConvertFrom-Json | ConvertTo-Json 
````

Your result should look something like this: 

````
pth 5                                  
READINESS -> HTTP 200                                                           
{
  "status": "Ready",
  "environment": "DEV",
  "timestamp": "2026-09-18T15:00:19.5875085-05:00",
  "checks": [
    {
      "name": "required-configuration",
      "category": "config",
      "enforcement": "blocking",
      "result": "Pass",
      "detail": "EnvironmentSettings:DisplayName = 'DEV'"
    },
    {
      "name": "database",
      "category": "dependency",
      "enforcement": "blocking",
      "result": "Pass",
      "detail": "Opened a connection to the policy database"
    },
    {
      "name": "policy-smoke-test",
      "category": "workflow",
      "enforcement": "blocking",
      "result": "Pass",
      "detail": "Created and retrieved policy 1"
    }
  ]
}
````

Happy path confirmed — a real LocalDB connection succeeded, the smoke test created and read back policy 1, all three blocking checks Pass → 200 Ready. The machinery is real, not theater.

Now the heart of Week 3: deliberately breaking things and recording the two dimensions separately.

---

**Setting up the Failure Matrix**

Next we want to lay out our 4 tests:

Base config for all DB experiments: DisplayName=DEV, RequireDatabase=true, RequirePolicySmokeTest=false (isolating the database variable). We only change the connection string or the LocalDB state.

| #	|Scenario|	What we change|
|--|--|--|
|E1	|Wrong connection |string	Server=(localdb)\NopeInstance;... (instance doesn't exist)|
|E2	|Missing database	|valid server, Database=GhostDb (db doesn't exist)|
|E3	|Unavailable dependency	|valid string, but sqllocaldb stop MSSQLLocalDB first|
|E4	|Invalid configuration	|RequireDatabase=true but ConnectionStrings__PolicyDatabase=""|


**Predict before we run**
Commit to an answer for each. Fill in these four columns:

| #	|database |result (Pass/Fail/Skipped)	|/readiness HTTP (200/503)	|Deployment execution (SUCCESS/FAIL)	|Environment readiness (READY/NOT READY)|
|--|--|--|--|--|--|
|E1|	|?	|?	|?	|?|
|E2|	|?	|?	|?	|?|
|E3|	|?	|?	|?	|?|
|E4|	|?	|?	|?	|?|
|E5|	|?	|?	|?	|?|

---

**Run the Tests**

Tests 1,2, 4,5: 

````
Get-Process dotnet, PolicyService -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
$exe = ".\src\PolicyService\bin\Debug\net9.0\win-x64\PolicyService.exe"

function Invoke-Scenario {
    param($Name, $Conn, $Require)
    $env:EnvironmentSettings__DisplayName    = "DEV"
    $env:Readiness__RequireDatabase          = $Require
    $env:Readiness__RequirePolicySmokeTest   = "false"
    $env:ConnectionStrings__PolicyDatabase   = $Conn
    $env:ASPNETCORE_URLS                     = "http://localhost:5006"
    $p = Start-Process -FilePath $exe -PassThru -WindowStyle Hidden
    for ($i = 0; $i -lt 40; $i++) {
        try { Invoke-WebRequest http://localhost:5006/health -TimeoutSec 1 -SkipHttpErrorCheck | Out-Null; break }
        catch { Start-Sleep -Milliseconds 250 }
    }
    $h  = Invoke-WebRequest http://localhost:5006/health    -SkipHttpErrorCheck
    $r  = Invoke-WebRequest http://localhost:5006/readiness -SkipHttpErrorCheck
    $rb = $r.Content | ConvertFrom-Json
    $db = $rb.checks | Where-Object { $_.name -eq 'database' }
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
    [pscustomobject]@{
        Scenario = $Name; HealthHTTP = $h.StatusCode; DbResult = $db.result
        ReadyHTTP = $r.StatusCode; Status = $rb.status; DbDetail = $db.detail
    }
}

$results = @()
$results += Invoke-Scenario "E1 wrong-conn"     "Server=(localdb)\NopeInstance;Database=PolicyDb;Integrated Security=true;Connect Timeout=3" "true"
$results += Invoke-Scenario "E2 missing-db"     "Server=(localdb)\MSSQLLocalDB;Database=GhostDb;Integrated Security=true;Connect Timeout=3" "true"
$results += Invoke-Scenario "E4 invalid-config" "" "true"
$results += Invoke-Scenario "E5 qa-partial"     "Server=(localdb)\NopeInstance;Database=PolicyDb;Integrated Security=true;Connect Timeout=3" "false"
$results | Format-Table -AutoSize -Wrap
````

Test 3 (run separately for clarity):
````
# ensure DB is up, start service with DB required + valid conn
sqllocaldb start MSSQLLocalDB
$env:EnvironmentSettings__DisplayName  = "DEV"
$env:Readiness__RequireDatabase        = "true"
$env:Readiness__RequirePolicySmokeTest = "false"
$env:ConnectionStrings__PolicyDatabase = "Server=(localdb)\MSSQLLocalDB;Database=PolicyDb;Integrated Security=true;Connect Timeout=3"
Start-Process -FilePath ".\src\PolicyService\bin\Debug\net9.0\win-x64\PolicyService.exe" -WindowStyle Hidden
Start-Sleep -Seconds 3

"BEFORE stop:"
(Invoke-WebRequest http://localhost:5006/readiness -SkipHttpErrorCheck).StatusCode   # expect 200

sqllocaldb stop MSSQLLocalDB      # dependency dies, app keeps running

"AFTER stop:"
(Invoke-WebRequest http://localhost:5006/health    -SkipHttpErrorCheck).StatusCode   # still 200 (alive)
(Invoke-WebRequest http://localhost:5006/readiness -SkipHttpErrorCheck).StatusCode   # now 503 (not ready)

# cleanup
Get-Process PolicyService -ErrorAction SilentlyContinue | Stop-Process -Force
sqllocaldb start MSSQLLocalDB
````

`The observation to write in your notes: liveness held constant while readiness changed, with no redeploy. That single sentence is why liveness and readiness are separate endpoints.`

You now have a running record of, per deployment, (a) did execution succeed and (b) was the environment ready. When you go to persist that as deployment provenance (artifact ID, commit, branch, build time, deploy time, target env, validation result), where should the validation result come from — the pipeline re-deriving it, or the /readiness response itself captured at deploy time? 

--- 

**Refactoring**

Make these updates to lock down categories, statuses, and configuration in program.cs:

1) Add a using:
````
using System.Text.Json.Serialization;
````

2) Add a couple enums:
````
[JsonConverter(typeof(JsonStringEnumConverter))]
enum Enforcement
{
    Blocking,
    Conditional
}

[JsonConverter(typeof(JsonStringEnumConverter))]
enum CheckResult
{
    Pass,
    Fail,
    Skipped
}
````

3) 
Update ReadinessCheck record
````
record ReadinessCheck(
    string Name,
    string Category,
    Enforcement Enforcement,
    CheckResult Result,
    string Detail);
````

4) Update the /readiness handler: 
````
    var checks = new List<ReadinessCheck>
    {
        new(
            Name: "required-configuration",
            Category: "config",
            Enforcement: Enforcement.Blocking,
            Result: string.IsNullOrWhiteSpace(displayName)
                ? CheckResult.Fail
                : CheckResult.Pass,
            Detail: string.IsNullOrWhiteSpace(displayName)
                ? "EnvironmentSettings:DisplayName is not set"
                : $"EnvironmentSettings:DisplayName = '{displayName}'")
    };

    var requireDatabase = configuration.GetValue<bool>("Readiness:RequireDatabase");
    if (!requireDatabase)
    {
        checks.Add(new(
            Name: "database",
            Category: "dependency",
            Enforcement: Enforcement.Conditional,
            Result: CheckResult.Skipped,
            Detail: "Database not required for this deployment scope"));
    }
    else
    {
        var (result, detail) = await CheckDatabaseAsync(
            configuration.GetConnectionString("PolicyDatabase"));
        checks.Add(new(
            Name: "database",
            Category: "dependency",
            Enforcement: Enforcement.Blocking,
            Result: result,
            Detail: detail));
    }

    var requireSmokeTest = configuration.GetValue<bool>("Readiness:RequirePolicySmokeTest");
    if (!requireSmokeTest)
    {
        checks.Add(new(
            Name: "policy-smoke-test",
            Category: "workflow",
            Enforcement: Enforcement.Conditional,
            Result: CheckResult.Skipped,
            Detail: "Policy smoke test not required for this deployment scope"));
    }
    else
    {
        var (result, detail) = RunPolicySmokeTest(policyService);
        checks.Add(new(
            Name: "policy-smoke-test",
            Category: "workflow",
            Enforcement: Enforcement.Blocking,
            Result: result,
            Detail: detail));
    }

    var blockingFailed = checks.Any(c =>
        c.Enforcement == Enforcement.Blocking && c.Result == CheckResult.Fail);
````

5) Update two helper signatures:
````
static async Task<(CheckResult Result, string Detail)> CheckDatabaseAsync(
    string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return (CheckResult.Fail, "ConnectionStrings:PolicyDatabase is not set");
    }

    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return (CheckResult.Pass, "Opened a connection to the policy database");
    }
    catch (Exception ex)
    {
        return (CheckResult.Fail, $"Database connection failed: {ex.Message}");
    }
}

static (CheckResult Result, string Detail) RunPolicySmokeTest(
    PolicyService.Services.PolicyService service)
{
    try
    {
        var created = service.CreatePolicy(
            new CreatePolicyRequest("readiness-probe", "smoke-test"));
        var fetched = service.GetPolicy(created.Id);

        return fetched is not null
            ? (CheckResult.Pass, $"Created and retrieved policy {created.Id}")
            : (CheckResult.Fail, "Created policy could not be retrieved");
    }
    catch (Exception ex)
    {
        return (CheckResult.Fail, $"Policy smoke test threw: {ex.Message}");
    }
}
````

Ok, now that this is cleaned up let's get back to the tests. 

--- 


**1. Hit /readiness and capture both signals**

````
# after the artifact is started with injected env config
$resp   = Invoke-WebRequest "http://localhost:5006/readiness" -SkipHttpErrorCheck
$status = [int]$resp.StatusCode           # the contract gate: 200 | 503
$body   = $resp.Content | ConvertFrom-Json # the service's self-report + per-check breakdown
````

Two things, deliberately kept separate:

- httpStatus — the gate (what your promotion decision keys off of) body.status + body.checks[] — the evidence (why)
- You learned earlier why -SkipHttpErrorCheck is mandatory here: without it, a 503 throws and you lose the very body you're trying to preserve. The failure path is exactly when the evidence matters most.


**2. Fold it into a deployment metadata record**

````
{
  "artifact": {
    "id": "PolicyService-1.0.0+3f9a2c1",
    "version": "1.0.0",
    "commit": "3f9a2c1e…",
    "branch": "main",
    "buildTimeUtc": "2026-09-18T14:02:11Z"
  },
  "deployment": {
    "targetEnvironment": "DEV",
    "deployTimeUtc": "2026-09-18T14:07:44Z",
    "deployedBy": "github-actions",
    "runId": "1234567890"
  },
  "validation": {
    "endpoint": "/readiness",
    "httpStatus": 200,
    "capturedAtUtc": "2026-09-18T14:07:46Z",
    "outcome": "Ready",
    "evidence": { /* verbatim /readiness body — no re-derivation */ }
  }
}
````
The discipline that makes this provenance and not just logging:

- artifact block is copied forward unchanged from the CI job outputs. When you promote DEV → QA, the QA record has an identical artifact block but a different deployment and validation. Same artifact, validated twice — that's the immutability proof, on paper.
- evidence is the raw body, stored byte-for-byte. No boolean summary replacing it.
- Everything UTC.

**3. Persist it durably**

Write the record to a file and upload it as a workflow artifact (or commit to a provenance store) so it outlives the runner. The runner is ephemeral; the evidence must not be.

Next, let's look at the pipeline step: 

---

**The Pipeline**

The whole design collapses to a strict ordering rule, and this is the part most people get wrong:

````
capture → persist → gate — in that order, as separate steps.
````

If the readiness gate and the metadata write are the same step, a 503 either fails before it writes (silent gap — the thing you rejected) or you bury the gate logic and lose the clean fail signal. Splitting them means the evidence is on disk and uploaded before anything is allowed to fail the job.

`The Capture + Record Script`
This is the core. It captures both signals, builds the record, writes it, and — critically — does not throw on 503. It only reports the outcome via a step output.

````
# scripts/record-readiness.ps1
param(
  [Parameter(Mandatory)] [string] $BaseUrl,          # e.g. http://localhost:5006
  [Parameter(Mandatory)] [string] $TargetEnvironment,# DEV | QA
  [Parameter(Mandatory)] [string] $OutFile           # e.g. artifacts/deploy-metadata-DEV.json
)

$ErrorActionPreference = 'Stop'

# 1. CAPTURE — both signals, and do NOT throw on 503
$resp   = Invoke-WebRequest "$BaseUrl/readiness" -SkipHttpErrorCheck
$status = [int]$resp.StatusCode
$body   = $resp.Content | ConvertFrom-Json

# 2. ASSEMBLE — artifact block copied forward from CI outputs (env), unchanged
$record = [ordered]@{
  artifact = [ordered]@{
    id           = $env:ARTIFACT_ID
    version      = $env:ARTIFACT_VERSION
    commit       = $env:GITHUB_SHA
    branch       = $env:GITHUB_REF_NAME
    buildTimeUtc = $env:ARTIFACT_BUILD_TIME
  }
  deployment = [ordered]@{
    targetEnvironment = $TargetEnvironment
    deployTimeUtc     = (Get-Date).ToUniversalTime().ToString('o')
    executionResult   = 'Succeeded'   # the process started; that's the deployment-execution axis
    runId             = $env:GITHUB_RUN_ID
  }
  validation = [ordered]@{
    endpoint      = '/readiness'
    httpStatus    = $status
    outcome       = $body.status               # Ready | NotReady, verbatim from the service
    capturedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    evidence      = $body                       # the whole body, byte-for-byte
  }
}

# 3. PERSIST
New-Item -ItemType Directory -Force -Path (Split-Path $OutFile) | Out-Null
$record | ConvertTo-Json -Depth 10 | Set-Content -Path $OutFile -Encoding utf8
Write-Host "Wrote provenance record to $OutFile (httpStatus=$status, outcome=$($body.status))"

# 4. REPORT outcome to the job — but do NOT fail here
"outcome=$($body.status)"    | Out-File $env:GITHUB_OUTPUT -Append
"httpStatus=$status"         | Out-File $env:GITHUB_OUTPUT -Append
````

Note what it does not do: it never calls exit 1. Persisting evidence is not the place to fail the build.

---

We're moving the databases over to provisioned databases in the github pipeline. This means that github will temporarily host an internal database as part of the build process and check it's ability to make the connection. 

At this point in the lab, your pipeline should look something like this: 

````
name: PolicyService CI

on:
  push:
    branches:
      - main
    paths-ignore:
      - 'artifacts/**'
      - 'docs/**'
      - 'labs/**'

  pull_request:
    branches:
      - main
    paths-ignore:
      - 'artifacts/**'
      - 'docs/**'
      - 'labs/**'

  workflow_dispatch:

jobs:
  build:
    runs-on: windows-latest

    env:
      VERSION_PREFIX: "0.2"

    steps:
      - name: Checkout source
        uses: actions/checkout@v7

      - name: Install .NET SDK
        uses: actions/setup-dotnet@v6
        with:
          global-json-file: global.json

      - name: Calculate artifact version
        shell: pwsh
        run: |
          $version = "$env:VERSION_PREFIX.${{ github.run_number }}"
          $commit = "${{ github.sha }}"
          $shortCommit = $commit.Substring(0, 8)

          Write-Host "Application version: $version"
          Write-Host "Source revision: $commit"
          Write-Host "Short revision: $shortCommit"

          "APP_VERSION=$version" >> $env:GITHUB_ENV
          "SHORT_COMMIT=$shortCommit" >> $env:GITHUB_ENV

      - name: Restore
        shell: pwsh
        run: |
          dotnet restore --locked-mode

          if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
          }

      - name: Compile
        shell: pwsh
        run: |
          dotnet build `
            --configuration Release `
            --no-restore `
            -p:Version=$env:APP_VERSION `
            -p:SourceRevisionId=${{ github.sha }}

          if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
          }

      - name: Unit Tests
        shell: pwsh
        run: |
          dotnet test `
            --configuration Release `
            --no-build

          if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
          }

      - name: Publish Application
        shell: pwsh
        run: |
          $publishPath = "$env:GITHUB_WORKSPACE\artifacts\publish"

          Remove-Item "$env:GITHUB_WORKSPACE\artifacts" `
            -Recurse `
            -Force `
            -ErrorAction SilentlyContinue

          New-Item `
            -ItemType Directory `
            -Path $publishPath `
            -Force | Out-Null

          dotnet publish `
            "$env:GITHUB_WORKSPACE\src\PolicyService\PolicyService.csproj" `
            --configuration Release `
            --no-build `
            --no-restore `
            --output $publishPath

          if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
          }

      - name: Package Artifact
        shell: pwsh
        run: |
          $publishPath = "$env:GITHUB_WORKSPACE\artifacts\publish"
          $artifactPath = "$env:GITHUB_WORKSPACE\artifacts\package"

          New-Item `
            -ItemType Directory `
            -Path $artifactPath `
            -Force | Out-Null

          $metadata = [ordered]@{
            artifact          = "PolicyService"
            version           = $env:APP_VERSION
            buildTimeUtc      = (Get-Date).ToUniversalTime().ToString('o')
            pipelineRun       = "${{ github.run_number }}"
            sourceRevision    = "${{ github.sha }}"
            sourceBranch      = "${{ github.ref }}"
            targetFramework   = "net9.0"
            runtimeIdentifier = "win-x64"
            selfContained     = $true
            sdkVersion        = (dotnet --version)
          }

          $metadata |
            ConvertTo-Json |
            Set-Content "$publishPath\artifact-manifest.json"

          $artifactName = "PolicyService-$env:APP_VERSION-$env:SHORT_COMMIT.zip"
          $zipPath = Join-Path $artifactPath $artifactName

          Compress-Archive `
            -Path "$publishPath\*" `
            -DestinationPath $zipPath

          $hash = Get-FileHash $zipPath -Algorithm SHA256

          "$($hash.Hash)  $artifactName" |
            Set-Content "$zipPath.sha256"

          Write-Host "Artifact: $artifactName"
          Write-Host "SHA256:   $($hash.Hash)"

      - name: Publish Artifact
        uses: actions/upload-artifact@v7
        with:
          name: PolicyService
          path: artifacts/package/
  
  deploy-dev:
    needs: build
    runs-on: windows-latest
    steps:
      - name: Provision DEV database
        shell: pwsh
        run: |
          sqllocaldb create MSSQLLocalDB 2>$null   # no-op if it already exists
          sqllocaldb start  MSSQLLocalDB
          sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "IF DB_ID('PolicyDb') IS NULL CREATE DATABASE PolicyDb;"
          
      - name: Checkout source        # needed for scripts/record-readiness.ps1
        uses: actions/checkout@v7

      - name: Download build artifact
        uses: actions/download-artifact@v4
        with:
          name: PolicyService
          path: incoming

      - name: Verify integrity and expand
        shell: pwsh
        run: |
          $zip = Get-ChildItem incoming\*.zip | Select-Object -First 1
          $expected = (Get-Content "$($zip.FullName).sha256").Split(' ')[0]
          $actual   = (Get-FileHash $zip.FullName -Algorithm SHA256).Hash
          if ($actual -ne $expected) {
            Write-Error "Artifact hash mismatch. Refusing to deploy."; exit 1
          }
          Expand-Archive $zip.FullName -DestinationPath app -Force
          "ARTIFACT_ZIP=$($zip.BaseName)" >> $env:GITHUB_ENV

      - name: Carry provenance forward from the artifact
        shell: pwsh
        run: |
          $m = Get-Content app\artifact-manifest.json | ConvertFrom-Json
          "ARTIFACT_ID=$env:ARTIFACT_ZIP"          >> $env:GITHUB_ENV
          "ARTIFACT_VERSION=$($m.version)"          >> $env:GITHUB_ENV
          "ARTIFACT_BUILD_TIME=$($m.buildTimeUtc)"  >> $env:GITHUB_ENV
          "ARTIFACT_COMMIT=$($m.sourceRevision)"    >> $env:GITHUB_ENV
          "ARTIFACT_BRANCH=$($m.sourceBranch)"      >> $env:GITHUB_ENV

      - name: Inject DEV config and start the app
        shell: pwsh
        run: |
          # environment-specific values injected at deploy time — the artifact stays inert
          $env:ASPNETCORE_URLS               = "http://localhost:5006"
          $env:ASPNETCORE_ENVIRONMENT        = "Development"
          $env:EnvironmentSettings__DisplayName = "DEV"
          $env:Readiness__RequireDatabase        = "true"
          $env:ConnectionStrings__PolicyDatabase = "Server=(localdb)\MSSQLLocalDB;Database=PolicyDb;Integrated Security=true;Connect Timeout=3"
          Start-Process -FilePath "app\PolicyService.exe" -PassThru | Out-Null

      - name: Wait for liveness
        id: liveness
        shell: pwsh
        run: |
          $live = $false
          foreach ($i in 1..30) {
            try {
              $r = Invoke-WebRequest http://localhost:5006/health -SkipHttpErrorCheck -TimeoutSec 2
              if ($r.StatusCode -eq 200) { $live = $true; break }
            } catch { }
            Start-Sleep -Seconds 1
          }
          "live=$($live.ToString().ToLower())" >> $env:GITHUB_OUTPUT
          if (-not $live) { Write-Host "App never became live within timeout." }

      - name: Capture readiness and record provenance
        id: record
        if: always()
        shell: pwsh
        run: |
          if ('${{ steps.liveness.outputs.live }}' -eq 'true') {
            ./scripts/record-readiness.ps1 -BaseUrl http://localhost:5006 -TargetEnvironment DEV -OutFile artifacts/deploy-metadata-DEV.json -ExecutionResult Succeeded
          } else {
            ./scripts/record-readiness.ps1 -BaseUrl http://localhost:5006 -TargetEnvironment DEV -OutFile artifacts/deploy-metadata-DEV.json -ExecutionResult Failed -FailureReason "App never became live within timeout"
          }

      - name: Upload provenance record
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: deploy-metadata-DEV
          path: artifacts/deploy-metadata-DEV.json

      - name: Deployment execution gate
        shell: pwsh
        run: |
          if ('${{ steps.record.outputs.executionResult }}' -ne 'Succeeded') {
            Write-Error "Deployment execution Failed. See provenance record."; exit 1
          }

      - name: Readiness gate
        shell: pwsh
        run: |
          if ('${{ steps.record.outputs.outcome }}' -ne 'Ready') {
            Write-Error "Environment NotReady (httpStatus=${{ steps.record.outputs.httpStatus }})."; exit 1
          }
     

````

Three things worth internalizing about this shape:

1) if: always() on the upload is what enforces your "write on 503" decision at the pipeline level. Without it, a failed gate would skip the upload and you'd lose the very record that explains the failure. The decision you made verbally is now mechanically guaranteed.

2) The gate is last and separate. By the time exit 1 can fire, the record is already written and uploaded. Deployment-execution (Succeeded) and readiness-outcome (NotReady) end up as two distinct facts on disk — your diagram's two axes, captured before the job is allowed to go red.

3) The artifact block comes from environment variables set by the CI job, not recomputed here. When you promote to QA, the same ARTIFACT_ID / ARTIFACT_BUILD_TIME flow through unchanged — so the DEV record and QA record will share an identical artifact block. That shared block is your on-paper proof that the same immutable artifact was validated in both environments.

Also, update our manifest to have the build time: 
````
          $metadata = [ordered]@{
            artifact          = "PolicyService"
            version           = $env:APP_VERSION
            buildTimeUtc      = (Get-Date).ToUniversalTime().ToString('o')   # <-- add
            pipelineRun       = "${{ github.run_number }}"
            sourceRevision    = "${{ github.sha }}"
            sourceBranch      = "${{ github.ref }}"
            ...
````

Note the two spots where the manifest now feeds the record: update record-readiness.ps1 to read $env:ARTIFACT_COMMIT / $env:ARTIFACT_BRANCH instead of $env:GITHUB_SHA / $env:GITHUB_REF_NAME, so the record's commit is the artifact's commit, not the runner's.

**Part A — Make DEV's readiness check hit a real SQL database**

Two edits. First, provision the database on the runner before the app starts (the windows-latest image ships SqlLocalDB and sqlcmd, same tools you use locally):

````
      - name: Provision DEV database
        shell: pwsh
        run: |
          sqllocaldb create MSSQLLocalDB 2>$null   # no-op if it already exists
          sqllocaldb start  MSSQLLocalDB
          sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "IF DB_ID('PolicyDb') IS NULL CREATE DATABASE PolicyDb;"
````

Second, flip the config in your "Inject DEV config and start the app" step from skip to require:

````
          $env:Readiness__RequireDatabase        = "true"
          $env:ConnectionStrings__PolicyDatabase = "Server=(localdb)\MSSQLLocalDB;Database=PolicyDb;Integrated Security=true;Connect Timeout=3"
````

That connection string is identical to the one you've been running locally, so the exact same steps reproduce on your machine — you already have PolicyDb. Run the Provision, Inject, Wait for liveness, and record blocks in a local pwsh terminal and you're exercising the real dependency path end to end.

One caveat worth a comment in the file: LocalDB is a property of the runner image, not your artifact. If a future image drops it, DEV goes NotReady for an infrastructure reason, not a code reason — which is exactly the kind of thing your provenance record is designed to make visible. (Fallbacks if that day comes: the preinstalled SQL Express service, or a SQL service container.)

**Part B — Write a record with executionResult: Failed**

Right now the failure path dies in "Wait for liveness" before the record step, so a failed execution leaves no evidence. Fix it in two places.

1. Teach record-readiness.ps1 to write a Failed record. Add an ExecutionResult parameter and make the readiness probe conditional:

````
param(
  [Parameter(Mandatory)] [string] $BaseUrl,
  [Parameter(Mandatory)] [string] $TargetEnvironment,
  [Parameter(Mandatory)] [string] $OutFile,
  [ValidateSet('Succeeded','Failed')] [string] $ExecutionResult = 'Succeeded',
  [string] $FailureReason = ''
)
$ErrorActionPreference = 'Stop'

if ($ExecutionResult -eq 'Succeeded') {
  $resp     = Invoke-WebRequest "$BaseUrl/readiness" -SkipHttpErrorCheck
  $status   = [int]$resp.StatusCode
  $body     = $resp.Content | ConvertFrom-Json
  $outcome  = $body.status
  $evidence = $body
} else {
  $status   = $null
  $outcome  = 'NotEvaluated'     # <-- NOT "NotReady" — see note below
  $evidence = $null
}

$record = [ordered]@{
  artifact = [ordered]@{
    id           = $env:ARTIFACT_ID
    version      = $env:ARTIFACT_VERSION
    commit       = $env:ARTIFACT_COMMIT
    branch       = $env:ARTIFACT_BRANCH
    buildTimeUtc = $env:ARTIFACT_BUILD_TIME
  }
  deployment = [ordered]@{
    targetEnvironment = $TargetEnvironment
    deployTimeUtc     = (Get-Date).ToUniversalTime().ToString('o')
    executionResult   = $ExecutionResult
    failureReason     = if ($FailureReason) { $FailureReason } else { $null }
    runId             = $env:GITHUB_RUN_ID
  }
  validation = [ordered]@{
    endpoint      = '/readiness'
    httpStatus    = $status
    outcome       = $outcome
    capturedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    evidence      = $evidence
  }
}

New-Item -ItemType Directory -Force -Path (Split-Path $OutFile) | Out-Null
$record | ConvertTo-Json -Depth 10 | Set-Content -Path $OutFile -Encoding utf8

"outcome=$outcome"                 | Out-File $env:GITHUB_OUTPUT -Append
"httpStatus=$status"               | Out-File $env:GITHUB_OUTPUT -Append
"executionResult=$ExecutionResult" | Out-File $env:GITHUB_OUTPUT -Append
````

2. Rewire the workflow so liveness reports instead of aborting, and the record step always runs and branches:

````
      - name: Wait for liveness
        id: liveness
        shell: pwsh
        run: |
          $live = $false
          foreach ($i in 1..30) {
            try {
              $r = Invoke-WebRequest http://localhost:5006/health -SkipHttpErrorCheck -TimeoutSec 2
              if ($r.StatusCode -eq 200) { $live = $true; break }
            } catch { }
            Start-Sleep -Seconds 1
          }
          "live=$($live.ToString().ToLower())" >> $env:GITHUB_OUTPUT
          if (-not $live) { Write-Host "App never became live within timeout." }

      - name: Capture readiness and record provenance
        id: record
        if: always()
        shell: pwsh
        run: |
          if ('${{ steps.liveness.outputs.live }}' -eq 'true') {
            ./scripts/record-readiness.ps1 -BaseUrl http://localhost:5006 -TargetEnvironment DEV -OutFile artifacts/deploy-metadata-DEV.json -ExecutionResult Succeeded
          } else {
            ./scripts/record-readiness.ps1 -BaseUrl http://localhost:5006 -TargetEnvironment DEV -OutFile artifacts/deploy-metadata-DEV.json -ExecutionResult Failed -FailureReason "App never became live within timeout"
          }

      - name: Upload provenance record
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: deploy-metadata-DEV
          path: artifacts/deploy-metadata-DEV.json

      - name: Deployment execution gate
        shell: pwsh
        run: |
          if ('${{ steps.record.outputs.executionResult }}' -ne 'Succeeded') {
            Write-Error "Deployment execution Failed. See provenance record."; exit 1
          }

      - name: Readiness gate
        shell: pwsh
        run: |
          if ('${{ steps.record.outputs.outcome }}' -ne 'Ready') {
            Write-Error "Environment NotReady (httpStatus=${{ steps.record.outputs.httpStatus }})."; exit 1
          }
````

This gives your model three reachable states instead of two — worth holding in mind, because it changes the diagram:

|executionResult	|outcome	|Meaning|
|--|--|--|
|Succeeded	|Ready	|deployed and serving|
|Succeeded	|NotReady	|process up, environment not fit (your E-series)|
|Failed	|NotEvaluated	|never came up — new corner you just added|

--- 
**Food for thought...**

`Where exactly is it deploying when it targets a dev or QA deploy in this pipeline?`

Nowhere external — and that's worth being precise about, because it changes what your green checkmark actually certifies.

The deploy target is the GitHub-hosted runner itself. When deploy-dev runs, it spins up a fresh, ephemeral windows-latest VM, expands the artifact onto it, and starts PolicyService.exe bound to http://localhost:5006 on that same VM. The app is only ever reachable from within the job. Nobody — not you, not a browser, not another service — can hit it. When the job ends (pass or fail), the runner is destroyed and the app and its LocalDB evaporate with it.

So "DEV" and "QA" here are labels, not places. What the label actually does:

What you might assume	What actually happens
Routes to a persistent DEV server	Runs on the throwaway runner, localhost only
Connects to a shared DEV database	Creates a fresh empty PolicyDb in the runner's LocalDB, gone at job end
App stays up after the run	Process dies when the runner is torn down
-TargetEnvironment DEV picks a host	It only selects which config values get injected (DisplayName=DEV, the connection string, etc.)
This is exactly the "DEV/QA simulated in the pipeline, not persistent servers" constraint you set at the start. What the pipeline faithfully rehearses is the choreography: same immutable artifact promoted forward, environment-specific config injected at deploy time, readiness validated against a real (if ephemeral) dependency, provenance recorded. What it does not exercise is a real network deploy to a durable host, DNS, environment isolation, or a database that outlives the run.

Which sets up the honest question for your retrospective and diagram: on an ephemeral runner where the "environment" is born and dies inside one job, what does a Ready result actually certify — the health of a real place, or the artifact's ability to become ready when a correctly-configured environment is placed under it? 

---

### Your liveness result is currently being treated as deployment execution

Right now this logic says:

```PowerShell
if ('${{ steps.liveness.outputs.live }}' -eq 'true') {
    ./scripts/record-readiness.ps1 ... -ExecutionResult Succeeded
} else {
    ./scripts/record-readiness.ps1 ... -ExecutionResult Failed `
        -FailureReason "App never became live within timeout"
}
```

That slightly collapses the distinction we've been trying to preserve.

Suppose this happens:

```
Artifact downloaded        SUCCESS
Hash verified              SUCCESS
Artifact expanded          SUCCESS
Configuration supplied     SUCCESS
Process launch command     SUCCESS

GET /health                FAIL
```

I'd classify that as:

```
Deployment execution: SUCCESS
Environment readiness: NOT READY
```

The health check is **evidence about the resulting environment**, not necessarily evidence that the deployment mechanism failed.

That is exactly the Week 3 distinction the workbook is asking you to record independently.

## I would separate these two states explicitly

Conceptually:

```
DEPLOYMENT EXECUTION
────────────────────
Artifact acquired
Integrity verified
Artifact installed
Configuration supplied
Process launch attempted
        │
        └── Succeeded

ENVIRONMENT VALIDATION
──────────────────────
/health
/readiness
dependency checks
workflow checks
        │
        └── Ready / NotReady
```

A failed `Start-Process` is a deployment execution failure.

A successfully executed `Start-Process` followed by `/health` failing is a validation/readiness failure.

That might seem like a small distinction, but later it gives you much better evidence:

```JSON
{
  "deploymentExecution": "Succeeded",
  "environmentReadiness": "NotReady",
  "checks": {
    "liveness": "Failed"
  }
}
```

instead of:

```JSON
{
  "deploymentExecution": "Failed",
  "environmentReadiness": "NotReady"
}
```

The first tells you much more about where the delivery system failed.

* * *

## I'd change the start step slightly

Give it an ID and explicitly capture whether the deployment operation completed:

```YAML
- name: Inject DEV config and start the app
  id: deployment
  shell: pwsh
  run: |
    $env:ASPNETCORE_URLS                  = "http://localhost:5006"
    $env:ASPNETCORE_ENVIRONMENT           = "Development"
    $env:EnvironmentSettings__DisplayName = "DEV"
    $env:Readiness__RequireDatabase       = "true"
    $env:ConnectionStrings__PolicyDatabase = "Server=(localdb)\MSSQLLocalDB;Database=PolicyDb;Integrated Security=true;Connect Timeout=3"

    try {
      $process = Start-Process `
        -FilePath "app\PolicyService.exe" `
        -PassThru

      Write-Host "PolicyService process started with PID $($process.Id)"
      "result=Succeeded" >> $env:GITHUB_OUTPUT
    }
    catch {
      Write-Error "Deployment execution failed: $_"
      "result=Failed" >> $env:GITHUB_OUTPUT
      exit 1
    }
```

There is one complication: if this step exits `1`, ordinary subsequent steps get skipped.

Since you specifically want a provenance record **even for failed deployment execution**, I'd actually allow the step to record failure without immediately terminating the job:

```YAML
- name: Inject DEV config and start the app
  id: deployment
  shell: pwsh
  run: |
    $env:ASPNETCORE_URLS                   = "http://localhost:5006"
    $env:ASPNETCORE_ENVIRONMENT            = "Development"
    $env:EnvironmentSettings__DisplayName  = "DEV"
    $env:Readiness__RequireDatabase        = "true"
    $env:ConnectionStrings__PolicyDatabase = "Server=(localdb)\MSSQLLocalDB;Database=PolicyDb;Integrated Security=true;Connect Timeout=3"

    try {
      $process = Start-Process `
        -FilePath "app\PolicyService.exe" `
        -PassThru

      Write-Host "PolicyService process started with PID $($process.Id)"

      "result=Succeeded" >> $env:GITHUB_OUTPUT
      "failureReason="   >> $env:GITHUB_OUTPUT
    }
    catch {
      Write-Host "Deployment execution failed: $_"

      "result=Failed" >> $env:GITHUB_OUTPUT
      "failureReason=$($_.Exception.Message)" >> $env:GITHUB_OUTPUT
    }
```

Then liveness should run only if deployment execution succeeded:

```YAML
- name: Wait for liveness
  id: liveness
  if: steps.deployment.outputs.result == 'Succeeded'
  shell: pwsh
  run: |
    $live = $false

    foreach ($i in 1..30) {
      try {
        $r = Invoke-WebRequest `
          http://localhost:5006/health `
          -SkipHttpErrorCheck `
          -TimeoutSec 2

        if ($r.StatusCode -eq 200) {
          $live = $true
          break
        }
      }
      catch { }

      Start-Sleep -Seconds 1
    }

    "live=$($live.ToString().ToLower())" >> $env:GITHUB_OUTPUT

    if (-not $live) {
      Write-Host "Application did not become live within timeout."
    }
```

Notice that **liveness failure doesn't fail this step**. That's deliberate. We're collecting evidence first.

* * *

## Then let the metadata recorder describe what happened

Something like:

```YAML
- name: Capture readiness and record provenance
  id: record
  if: always()
  shell: pwsh
  run: |
    $executionResult = '${{ steps.deployment.outputs.result }}'

    if ($executionResult -eq 'Succeeded') {
      ./scripts/record-readiness.ps1 `
        -BaseUrl http://localhost:5006 `
        -TargetEnvironment DEV `
        -OutFile artifacts/deploy-metadata-DEV.json `
        -ExecutionResult Succeeded
    }
    else {
      ./scripts/record-readiness.ps1 `
        -BaseUrl http://localhost:5006 `
        -TargetEnvironment DEV `
        -OutFile artifacts/deploy-metadata-DEV.json `
        -ExecutionResult Failed `
        -FailureReason '${{ steps.deployment.outputs.failureReason }}'
    }
```

Now your two gates really represent two separate questions:

```
Deployment execution gate
    ↓
Did our deployment operation work?

Readiness gate
    ↓
Did it produce an acceptable environment?
```

That is a much cleaner model.

* * *

## One other change before QA: don't deploy pull requests

Right now this workflow runs both `build` and `deploy-dev` for:

```YAML
push:
pull_request:
workflow_dispatch:
```

Building and testing a PR makes sense.

Automatically deploying every PR as part of this delivery workflow is a different policy decision.

I'd keep CI on PRs, but restrict deployment to `main`:

```YAML
deploy-dev:
  if: github.ref == 'refs/heads/main'
  needs: build
  runs-on: windows-latest
```

That gives you:

```
Pull request
    ↓
Build
Unit test
Package
    ↓
STOP

Merge/push to main
    ↓
Build
Unit test
Package
    ↓
Deploy DEV
```

This also gives us an important new Week 3 concept:

**A deployable artifact existing does not mean it must be deployed.**

Artifact creation and artifact promotion are separate lifecycle decisions.

* * *

## And your commented database section is useful for the lab

You currently have:

```YAML
# - name: Provision DEV database
```

commented out while your configuration says:

```PowerShell
$env:Readiness__RequireDatabase = "true"
```

That's actually perfect for one of the Week 3 experiments.

If LocalDB doesn't already contain `PolicyDb`, I would predict:

```
Build                    PASS
Artifact creation        PASS
Artifact integrity       PASS
Deployment execution     PASS
Application liveness     probably PASS
Database readiness       FAIL
Overall readiness        NOT READY
Promotion                BLOCKED
```

That's almost exactly the scenario the workbook wants you to observe.

Don't uncomment the database provisioning yet.

### Your next experiment

Before we implement QA, I'd run DEV in this state and see whether your prediction matches reality.

Specifically, predict these five outcomes before running it:

| Stage | Your prediction |
| --- | --- |
| Artifact integrity | ? |
| Deployment execution | ? |
| `/health` | ? |
| Database readiness | ? |
| Overall DEV readiness | ? |

Then run it with the database provisioning still disabled.

What matters most here is **where the failure surfaced**:

| Stage | Predicted | Actual |
| --- | --- | --- |
| Artifact integrity | Pass | Pass |
| Deployment execution | Pass | Pass |
| `/health` | Pass | Pass |
| Database readiness | Fail | Fail |
| Overall DEV readiness | Fail | Fail — HTTP 503 |

The final pipeline failure:

```
Write-Error: Environment NotReady (httpStatus=503).
Error: Process completed with exit code 1.
```

is therefore **not a failed deployment**. It is the readiness gate deliberately stopping promotion because the environment did not satisfy its readiness criteria.

Conceptually, this run looks like:

```
Source
  │
  ▼
Build ─────────────── PASS
  │
  ▼
Artifact ──────────── PASS
  │
  ▼
Integrity Check ───── PASS
  │
  ▼
Deploy DEV ────────── PASS
  │
  ▼
Process Started ───── PASS
  │
  ▼
/health ───────────── PASS
  │
  ▼
/readiness ────────── FAIL (503)
  │
  ▼
Promotion Gate ────── BLOCKED
```

That's a very clean demonstration of the Week 3 concept.

### Your provenance script is also doing the right thing

I looked through `record-readiness.ps1`. When deployment execution succeeds, it calls:

```PowerShell
$resp = Invoke-WebRequest "$BaseUrl/readiness" -SkipHttpErrorCheck
```

That `-SkipHttpErrorCheck` is important here. A `503` doesn't prevent you from collecting the readiness response; instead, the script records:

```
HTTP status: 503
outcome:     NotReady
evidence:    <the actual readiness response>
```

and then exposes the values to the later GitHub Actions gate.

That means you're following a useful pattern:

```
Observe failure
      ↓
Record evidence
      ↓
Publish provenance
      ↓
Enforce policy
```

rather than:

```
Observe failure
      ↓
Immediately crash
      ↓
Lose useful context
```

That's a significant improvement in delivery-system design.

There's also a subtle state you've correctly accounted for in the script:

```PowerShell
$outcome = 'NotEvaluated'
```

when deployment execution itself fails.

That's better than calling it `NotReady`.

If we couldn't successfully deploy the application, then we don't actually know whether the resulting environment is ready — **we never reached a valid readiness evaluation**.

So you now have three meaningful states:

```
Deployment Failed
    ↓
Readiness = NotEvaluated

Deployment Succeeded
    ↓
Readiness check fails
    ↓
Readiness = NotReady

Deployment Succeeded
    ↓
Readiness check passes
    ↓
Readiness = Ready
```

That's much more expressive than a single red/green deployment status.

## One thing I'd inspect before fixing the database

Download the `deploy-metadata-DEV` artifact from this run and look at the JSON.

Based on your script, it should resemble:

```JSON
{
  "artifact": {
    "id": "PolicyService-0.2.16-2fccfbd6",
    "version": "0.2.16",
    "commit": "2fccfbd6640e2679f934de2a6360bcd34a85652b",
    "branch": "refs/heads/main",
    "buildTimeUtc": "..."
  },
  "deployment": {
    "targetEnvironment": "DEV",
    "deployTimeUtc": "...",
    "executionResult": "Succeeded",
    "failureReason": null,
    "runId": "..."
  },
  "validation": {
    "endpoint": "/readiness",
    "httpStatus": 503,
    "outcome": "NotReady",
    "capturedAtUtc": "...",
    "evidence": {
      ...
    }
  }
}
```

Pay particular attention to `validation.evidence`.

The most useful provenance record doesn't merely tell us:

> NotReady

It tells us something more like:

> NotReady **because database connectivity failed while the application itself remained live.**

That's the difference between a status and diagnostic evidence.

### Then we should run the inverse experiment

After you've inspected that record, uncomment your database provisioning:

```YAML
- name: Provision DEV database
  shell: pwsh
  run: |
    sqllocaldb create MSSQLLocalDB 2>$null
    sqllocaldb start MSSQLLocalDB
    sqlcmd -S "(localdb)\MSSQLLocalDB" `
      -Q "IF DB_ID('PolicyDb') IS NULL CREATE DATABASE PolicyDb;"
```

Before running it, the expected state transition is:

```
                    BEFORE              AFTER

Artifact             PASS                PASS
Deployment           PASS                PASS
Liveness             PASS                PASS
Database             FAIL       ─────►   PASS
Readiness          NOT READY    ─────►   READY
Promotion           BLOCKED     ─────►   ALLOWED
```

This is a particularly useful experiment because **the artifact doesn't change at all**.

You're changing an environmental dependency:

```
Artifact A + Broken DEV  → NotReady

Artifact A + Healthy DEV → Ready
```

That demonstrates another major Week 3 idea: **environment readiness is a property of the deployed system, not just a property of the application artifact.**

Go ahead and first inspect the `deploy-metadata-DEV.json`. If the evidence looks useful, provision the database and rerun the exact same deployment. Then we'll compare the two provenance records before introducing the **DEV → QA promotion and approval gate**.

* * *

You now have evidence for two different states using the **same application artifact**:

| Condition | First run | Second run |
| --- | --- | --- |
| Artifact integrity | Pass | Pass |
| Deployment execution | Pass | Pass |
| Application liveness | Pass | Pass |
| Database readiness | Fail | Pass |
| Overall environment readiness | NotReady | Ready |
| Promotion eligibility | Blocked | Allowed |

That is a strong demonstration that **artifact quality and environment readiness are related but distinct**. Nothing about the artifact itself had to change; the environmental dependency changed, and the readiness result changed with it.

This is exactly the Week 3 behavior the workbook is trying to get you to reason about: deployment can complete successfully while the resulting environment is unusable, and post-deployment validation determines whether promotion should continue.

### Next step: add QA promotion

You’re now ready to add the second environment and make the workflow:

```
Build
  ↓
Publish immutable artifact
  ↓
Deploy DEV
  ↓
Validate DEV
  ↓
Approval / promotion gate
  ↓
Deploy SAME artifact to QA
  ↓
Validate QA
```

The important constraint remains: **QA should download the existing `PolicyService` artifact from the build job. It should not restore, build, test, or publish again.**

I’d make `deploy-qa` depend on `deploy-dev`:

```YAML
deploy-qa:
  needs: deploy-dev
  runs-on: windows-latest
```

and then repeat the deployment pattern with QA-specific runtime configuration, for example:

```PowerShell
$env:ASPNETCORE_URLS                    = "http://localhost:5007"
$env:ASPNETCORE_ENVIRONMENT             = "Production"
$env:EnvironmentSettings__DisplayName   = "QA"
$env:Readiness__RequireDatabase         = "true"
$env:ConnectionStrings__PolicyDatabase  = "Server=(localdb)\MSSQLLocalDB;Database=PolicyQaDb;Integrated Security=true;Connect Timeout=3"
```

For the approval step, GitHub environments are the cleanest way to represent it. You can create a GitHub Environment named `QA`, configure required reviewers, and then attach the job to it:

```YAML
deploy-qa:
  needs: deploy-dev
  runs-on: windows-latest
  environment: QA
```

That gives you an actual promotion gate between DEV and QA rather than just a sequencing dependency.

* * *

 For this lab, an independent QA database reinforces the Week 3 idea that **DEV and QA are separate environments with independent state and dependencies**, while the application artifact remains identical.

There is also an important detail about your GitHub-hosted setup: `deploy-dev` and `deploy-qa` run on separate fresh runners anyway. So even if both used a database named `PolicyDb`, they would not actually share the same LocalDB instance. I would still use distinct names—`PolicyDevDb` and `PolicyQaDb`—because it makes the environment boundary explicit in your configuration and provenance.

Your target becomes:

```
                         PolicyService-0.2.16-2fccfbd6.zip
                                      │
                        ┌─────────────┴─────────────┐
                        │                           │
                       DEV                         QA
                        │                           │
                 PolicyDevDb                 PolicyQaDb
                        │                           │
                    /readiness                  /readiness
                        │                           │
                      Ready                       Ready
                        │
                        └──── Approval ────────────►
```

Notice what is shared and what isn't:

```
Shared across environments
──────────────────────────
Artifact bytes
Artifact ID
Version
Commit
Branch
Build timestamp

Environment-specific
──────────────────────────
Database
Connection string
Environment name
Deployment timestamp
Readiness result
```

## Add the QA job

I would now add the following after `deploy-dev`.

```YAML
deploy-qa:
  needs: deploy-dev
  runs-on: windows-latest
  environment: QA

  steps:
    - name: Checkout source
      uses: actions/checkout@v7

    - name: Download build artifact
      uses: actions/download-artifact@v4
      with:
        name: PolicyService
        path: incoming

    - name: Verify integrity and expand
      shell: pwsh
      run: |
        $zip = Get-ChildItem incoming\*.zip | Select-Object -First 1

        $expected = (Get-Content "$($zip.FullName).sha256").Split(' ')[0]
        $actual = (Get-FileHash $zip.FullName -Algorithm SHA256).Hash

        if ($actual -ne $expected) {
          Write-Error "Artifact hash mismatch. Refusing to deploy."
          exit 1
        }

        Expand-Archive `
          $zip.FullName `
          -DestinationPath app `
          -Force

        "ARTIFACT_ZIP=$($zip.BaseName)" >> $env:GITHUB_ENV
```

Then carry the artifact's provenance forward exactly as DEV does:

```YAML
    - name: Carry provenance forward from the artifact
      shell: pwsh
      run: |
        $m = Get-Content app\artifact-manifest.json | ConvertFrom-Json

        "ARTIFACT_ID=$env:ARTIFACT_ZIP"           >> $env:GITHUB_ENV
        "ARTIFACT_VERSION=$($m.version)"          >> $env:GITHUB_ENV
        "ARTIFACT_BUILD_TIME=$($m.buildTimeUtc)"  >> $env:GITHUB_ENV
        "ARTIFACT_COMMIT=$($m.sourceRevision)"    >> $env:GITHUB_ENV
        "ARTIFACT_BRANCH=$($m.sourceBranch)"      >> $env:GITHUB_ENV
```

Provision an independent QA database:

```YAML
    - name: Provision QA database
      shell: pwsh
      run: |
        sqllocaldb create MSSQLLocalDB 2>$null
        sqllocaldb start MSSQLLocalDB

        sqlcmd `
          -S "(localdb)\MSSQLLocalDB" `
          -Q "IF DB_ID('PolicyQaDb') IS NULL CREATE DATABASE PolicyQaDb;"
```

Then inject **QA configuration**, without modifying the artifact:

```YAML
    - name: Inject QA config and start the app
      id: deployment
      shell: pwsh
      run: |
        $env:ASPNETCORE_URLS                    = "http://localhost:5007"
        $env:ASPNETCORE_ENVIRONMENT             = "Production"
        $env:EnvironmentSettings__DisplayName   = "QA"
        $env:Readiness__RequireDatabase         = "true"
        $env:ConnectionStrings__PolicyDatabase  = "Server=(localdb)\MSSQLLocalDB;Database=PolicyQaDb;Integrated Security=true;Connect Timeout=3"

        try {
          $process = Start-Process `
            -FilePath "app\PolicyService.exe" `
            -PassThru

          Write-Host "PolicyService started with PID $($process.Id)"

          "result=Succeeded" >> $env:GITHUB_OUTPUT
          "failureReason=" >> $env:GITHUB_OUTPUT
        }
        catch {
          Write-Host "Deployment execution failed: $_"

          "result=Failed" >> $env:GITHUB_OUTPUT
          "failureReason=$($_.Exception.Message)" >> $env:GITHUB_OUTPUT
        }
```

Then your QA validation is basically the DEV validation pointed at port `5007`:

```YAML
    - name: Wait for liveness
      id: liveness
      if: steps.deployment.outputs.result == 'Succeeded'
      shell: pwsh
      run: |
        $live = $false

        foreach ($i in 1..30) {
          try {
            $r = Invoke-WebRequest `
              http://localhost:5007/health `
              -SkipHttpErrorCheck `
              -TimeoutSec 2

            if ($r.StatusCode -eq 200) {
              $live = $true
              break
            }
          }
          catch { }

          Start-Sleep -Seconds 1
        }

        "live=$($live.ToString().ToLower())" >> $env:GITHUB_OUTPUT

        if (-not $live) {
          Write-Host "Application did not become live within timeout."
        }
```

Then capture the QA provenance:

```YAML
    - name: Capture readiness and record provenance
      id: record
      if: always()
      shell: pwsh
      run: |
        $executionResult = '${{ steps.deployment.outputs.result }}'

        if ($executionResult -eq 'Succeeded') {
          ./scripts/record-readiness.ps1 `
            -BaseUrl http://localhost:5007 `
            -TargetEnvironment QA `
            -OutFile artifacts/deploy-metadata-QA.json `
            -ExecutionResult Succeeded
        }
        else {
          ./scripts/record-readiness.ps1 `
            -BaseUrl http://localhost:5007 `
            -TargetEnvironment QA `
            -OutFile artifacts/deploy-metadata-QA.json `
            -ExecutionResult Failed `
            -FailureReason '${{ steps.deployment.outputs.failureReason }}'
        }
```

And publish/gate it:

```YAML
    - name: Upload provenance record
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: deploy-metadata-QA
        path: artifacts/deploy-metadata-QA.json

    - name: Deployment execution gate
      shell: pwsh
      run: |
        if ('${{ steps.record.outputs.executionResult }}' -ne 'Succeeded') {
          Write-Error "Deployment execution Failed. See provenance record."
          exit 1
        }

    - name: Readiness gate
      shell: pwsh
      run: |
        if ('${{ steps.record.outputs.outcome }}' -ne 'Ready') {
          Write-Error "Environment NotReady (httpStatus=${{ steps.record.outputs.httpStatus }})."
          exit 1
        }
```

## The important test after this

Don't intentionally break anything yet.

First prove the happy path:

```
Build
   ↓
DEV deployment
   ↓
DEV Ready
   ↓
Approval
   ↓
QA deployment
   ↓
QA Ready
```

Then compare the two metadata files.

You should be able to demonstrate:

```
deploy-metadata-DEV.json
artifact.id      ─┐
version           │
commit            ├── SAME
branch            │
buildTimeUtc     ─┘

targetEnvironment = DEV
deployTimeUtc     = unique
validation        = DEV result
```

versus:

```
deploy-metadata-QA.json
artifact.id      ─┐
version           │
commit            ├── SAME
branch            │
buildTimeUtc     ─┘

targetEnvironment = QA
deployTimeUtc     = unique
validation        = QA result
```

That comparison is your first concrete proof of **artifact promotion** rather than merely two successful deployments.

Once that passes, I'd make the next Week 3 experiment a deliberately broken **QA-only connection string**. That's more interesting than breaking DEV again, because DEV should pass and approve promotion, the exact same artifact should reach QA, deployment execution should succeed, and **QA alone should become `NotReady`**. That will demonstrate the difference between an artifact problem and an environment-specific problem extremely clearly.

* * *

**Note:**

If you got this error: (Line: 164, Col: 5): Unexpected value 'envioronment', it means taht you found a typo earlier in my lab. Congrats! Just fix the spelling and this should clear things up. 

* * *

You **do not have to create the environment first just to make the YAML valid**. If `QA` doesn't already exist, GitHub can create the environment when the workflow references it. However, if you want this line to act as a real **manual approval gate**, then you should configure the `QA` environment in the repository settings and add a required reviewer.

That is typically under:

```
Repository
  → Settings
  → Environments
  → New environment
  → QA
```

Then configure the protection rules for that environment, particularly **Required reviewers** if your repository/account supports that feature.

Conceptually:

```
deploy-dev
   │
   └── DEV Ready
           │
           ▼
     deploy-qa job
           │
     environment: QA
           │
           ▼
     Approval required
           │
           ▼
       QA deployment
```

So first fix the spelling and rerun it. If the workflow then goes directly into QA without waiting for approval, that means the `QA` environment exists without a reviewer/protection rule, and that's the next thing we'd configure.

* * *

You’re roughly **two-thirds to three-quarters through Week 3**. The core delivery system is working; what remains is mostly **failure injection, evidence capture, and the portfolio/knowledge artifacts**.

The workbook’s Week 3 goal is to promote one artifact through DEV and QA, externalize configuration, capture provenance, and use post-deployment validation plus approvals. Your current pipeline now satisfies most of that core implementation.

| Week 3 requirement | Status | Evidence from what you’ve built |
| --- | --- | --- |
| Build once | ✅ Done | One build job creates the packaged artifact |
| Promote same artifact to DEV and QA | ✅ Done | Both deployment jobs download the build artifact rather than rebuilding |
| Externalize environment configuration | ✅ Done | DEV/QA values are injected at deployment/runtime |
| DEV deployment | ✅ Done | DEV deploys and validates successfully |
| DEV health/readiness validation | ✅ Done | `/health` and `/readiness` are separated |
| QA deployment | ✅ Done | Independent QA job and DB |
| QA health/readiness validation | ✅ Done | QA validates after deployment |
| Capture deployment provenance | ✅ Done | Artifact, commit, branch, build/deployment time, environment, validation result |
| Separate deployment success from readiness | ✅ Demonstrated | Missing DB: deployment passed, readiness returned 503/NotReady |
| Approval before QA | ⚠️ Verify | `environment: QA` exists; if you configured required reviewers, this is done. If not, QA is sequenced but not manually gated |
| Wrong connection string experiment | ⬜ Remaining | Not deliberately tested yet |
| Missing database experiment | ✅ Done | This was your first DEV readiness failure |
| Invalid configuration experiment | ⬜ Remaining | Still to test |
| Unavailable dependency experiment | ⬜ Remaining | Still to test |
| Required diagram | ⬜ Remaining | “Deployment Success vs. Environment Readiness” |
| Deployment metadata schema | 🟡 Mostly implemented | Your JSON exists; we still need to turn it into the documented portfolio schema |
| Reflection prompts | ⬜ Remaining | Three Week 3 prompts |
| Knowledge check | ⬜ Remaining | Six questions, from memory |
| Weekly retrospective | ⬜ Remaining | Four retrospective questions |

The workbook specifically requires all four deliberate failure cases, with **deployment execution and environment readiness recorded separately**, and then the diagram plus metadata schema as the portfolio deliverable.

### What we will do next

1. **Verify the promotion gate first.** If your `QA` GitHub Environment has a required reviewer and the workflow actually stopped awaiting approval, we can check that objective off. If it simply proceeded automatically, configure the reviewer protection now.
2. **Run the three remaining failure experiments**, one at a time. I’d do them in this order:
    
    * Wrong QA connection string.
    * Invalid required configuration.
    * Unavailable dependency.
    
    For each one, do what you did with the missing database: **predict the result first**, then execute it, then compare the prediction with the provenance record.
3. **Confirm artifact promotion evidence.** Compare the DEV and QA metadata records from one successful run and verify that artifact ID/version/commit/build time are identical while target environment, deployment time, and validation evidence differ. That gives you direct evidence that this is artifact promotion, not independent builds.
4. **Create the Week 3 deliverable.** We’ll make the “Deployment Success vs. Environment Readiness” diagram from the experiments you actually ran, rather than creating a generic theoretical diagram. Then we’ll document the deployment metadata schema you’ve already implemented.
5. **Finish the learning portion.** We’ll answer the three reflection prompts, then I’ll give you the six knowledge-check questions without exposing the answer key, and finally we’ll write the Week 3 retrospective. The workbook also calls for an end-of-Week-3 review where you should be able to explain artifact promotion and environment readiness without notes.

So I would **not add more pipeline architecture right now**. You’ve built enough. The next useful thing is to test whether the architecture behaves correctly under failure.

Let’s start with the **wrong QA connection string** experiment. Before changing anything, predict these five outcomes:

**artifact integrity, QA deployment execution, `/health`, database readiness, and overall QA readiness.**

* * *

Yes. Since this is a public personal repository, GitHub supports required reviewers for environments, so you can make `QA` a real manual promotion gate. [GitHub Docs+1](https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments?utm_source=chatgpt.com)

Go to:

**Repository → Settings → Environments → QA**

Then under **Deployment protection rules**:

1. Enable **Required reviewers**.
2. Add yourself or another GitHub user as a reviewer.
3. Decide whether to enable **Prevent self-review**.
    * If you enable it and you are the person triggering the workflow, you will need a different reviewer.
    * For this lab, if you're working alone, leave **Prevent self-review** off so you can approve your own QA promotion. [GitHub Docs](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/manage-environments?utm_source=chatgpt.com)

Your existing job is already wired correctly:

```YAML
deploy-qa:
  needs: deploy-dev
  runs-on: windows-latest
  environment: QA
```

Once `QA` has a required reviewer configured, GitHub will stop the `deploy-qa` job before it starts. The job should show as **Waiting**, and the reviewer must approve it before the runner is allocated and the QA steps execute. [GitHub Docs+1](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/control-deployments?utm_source=chatgpt.com)

That gives you the flow we want:

```
Build
  ↓
Deploy DEV
  ↓
DEV readiness = Ready
  ↓
QA job requested
  ↓
WAITING FOR APPROVAL
  ↓
Human approves promotion
  ↓
Deploy QA
  ↓
QA readiness validation
```

One architectural point to notice: the approval is **not approving the build**. The artifact has already been built and DEV has already validated it. The approval is authorizing **promotion of that particular artifact into the QA environment**.

After you configure it, trigger a new workflow run and confirm that `deploy-qa` pauses in the **Waiting** state after DEV succeeds. Once you see that, approve it and let QA finish. That will complete the approval-gate requirement for Week 3.

* * *

That checks off the **approval/promotion gate** requirement for Week 3. Your flow now matches the workbook target:

```
Build
  ↓
Artifact
  ↓
Deploy DEV
  ↓
DEV health/readiness validation
  ↓
Manual approval
  ↓
Deploy QA
  ↓
QA health/readiness validation
```

That means the core happy-path implementation is complete.

The next phase is the deliberate failure testing required by the workbook. You have already completed the **missing database** case, so the remaining three are:

1. Wrong connection string
2. Invalid configuration
3. Unavailable dependency

For each one, we want to record separately whether **deployment execution succeeded** and whether **environment readiness succeeded**.

Let’s start with the **wrong QA connection string**, because it gives us a particularly useful DEV-vs-QA comparison.

Change only the QA connection string to point to a database that does not exist, for example:

```PowerShell
$env:ConnectionStrings__PolicyDatabase = `
  "Server=(localdb)\MSSQLLocalDB;Database=DoesNotExist;Integrated Security=true;Connect Timeout=3"
```

Keep everything else unchanged.

Before you run it, give me your predictions for these:

| Stage | Prediction |
| --- | --- |
| Build | ? |
| DEV readiness | ? |
| QA deployment execution | ? |
| QA `/health` | ? |
| QA database readiness | ? |
| Overall QA readiness | ? |

The important question here is whether you expect this to behave like an **artifact failure** or an **environment-specific failure**, and why.

* * *
The key classification is:

```
Artifact problem?              No
Environment-specific problem?  Yes
```

Why? Because the same artifact already proved itself in DEV. The only thing we changed for QA was runtime configuration.

So the expected flow is:

```
Build                         PASS
  ↓
Deploy DEV                    PASS
  ↓
DEV readiness                 READY
  ↓
Manual promotion approval
  ↓
Deploy SAME artifact to QA    PASS
  ↓
QA /health                    PASS
  ↓
QA database check             FAIL
  ↓
QA readiness                  NOT READY
  ↓
Promotion/use of QA blocked
```

That is a particularly strong Week 3 example because it demonstrates all three ideas at once:

* **Artifact promotion worked**
* **Deployment execution worked**
* **Environment readiness failed**

The workbook specifically wants you to inject a wrong connection string and record deployment execution separately from readiness, so this experiment maps directly to the lab requirement.

Go ahead and run it.

When it finishes, check two things:

1. The QA job should fail specifically at the readiness gate, not during artifact download or process start.
2. `deploy-metadata-QA.json` should show something conceptually like:

```JSON
{
  "deployment": {
    "executionResult": "Succeeded"
  },
  "validation": {
    "httpStatus": 503,
    "outcome": "NotReady"
  }
}
```

If it behaves that way, we'll mark **wrong connection string** complete and move to the **invalid configuration** experiment.


* * *

If you did everything correctly, the error should come back: 

**Write-Error: Environment NotReady (httpStatus=503).**

**Error:** Process completed with exit code 1.

Your wrong-connection-string experiment behaved as predicted:

| Stage | Result |
| --- | --- |
| Build | Pass |
| DEV readiness | Pass |
| QA deployment execution | Pass |
| QA `/health` | Pass |
| QA database readiness | Fail |
| Overall QA readiness | Fail |
| QA gate | Blocked with HTTP 503 |

The important part is that the pipeline failed at the **readiness gate**, not at build or deployment execution:

```
Write-Error: Environment NotReady (httpStatus=503).
```

So this is a clean example of:

```
Deployment execution = Succeeded
Environment readiness = NotReady
```

That directly satisfies the workbook’s requirement to distinguish deployment success from environment readiness for a deliberately bad connection string.

It also gives you stronger evidence than the missing-database test, because DEV succeeded with the same artifact and QA failed only after QA-specific configuration was injected. That strongly points to an **environment-specific configuration failure**, not an artifact defect.

Before reverting, I’d save the QA provenance file from this run. Ideally it should show something like:

```JSON
{
  "deployment": {
    "targetEnvironment": "QA",
    "executionResult": "Succeeded"
  },
  "validation": {
    "httpStatus": 503,
    "outcome": "NotReady"
  }
}
```

You now have two completed failure experiments:

* Missing database ✅
* Wrong connection string ✅
* Invalid configuration ⬜
* Unavailable dependency ⬜

After you revert the bad connection string and confirm the happy path is restored, the next experiment should be **invalid configuration**. For that one, we should deliberately break a required config value and predict whether the application will still become live, whether `/readiness` will reject it, or whether startup itself will fail.

* * *

A good QA config-failure candidate is a **required non-database setting that does not prevent the process from starting**.

That gives you the cleanest experiment because you want to distinguish:

```
Deployment execution      = Succeeded
Application liveness      = Pass
Configuration validation  = Fail
Environment readiness     = NotReady
```

rather than accidentally turning it into another database/dependency failure.

For your current service, the simplest candidate is probably:

```
EnvironmentSettings:DisplayName
```

You could deliberately make QA inject an invalid value:

```PowerShell
$env:EnvironmentSettings__DisplayName = ""
```

or omit it entirely.

Then have `/readiness` treat an empty or missing environment name as invalid configuration.

Conceptually:

```C#
var environmentName =
    configuration["EnvironmentSettings:DisplayName"];

var configValid =
    !string.IsNullOrWhiteSpace(environmentName);
```

If invalid:

```
/health      -> 200
/readiness   -> 503
```

That would make this experiment different from the two you've already done.

Your failure set would then look like:

| Experiment | Failure category |
| --- | --- |
| Missing database | Missing environmental resource |
| Wrong connection string | Incorrect environment configuration affecting a dependency |
| Missing/invalid environment setting | **Invalid application configuration** |
| Unavailable dependency | External runtime dependency failure |

### An even stronger version

If you want the configuration failure to feel less cosmetic, you could introduce a setting that genuinely represents required application behavior, such as:

```JSON
"PolicySettings": {
  "DefaultRegion": "US"
}
```

Then QA deliberately supplies:

```PowerShell
$env:PolicySettings__DefaultRegion = ""
```

and readiness validates that it exists.

That is architecturally cleaner than using `DisplayName`, because `DisplayName` is mostly descriptive. A setting like `DefaultRegion`, `PolicyMode`, or `TenantCode` represents configuration the application could plausibly require to function correctly.

For this lab, though, I would keep it simple unless you want more realism. **Missing `EnvironmentSettings__DisplayName` is enough to demonstrate configuration validation** as long as `/readiness` explicitly treats it as required.

Before running it, I would predict:

```
Build                  Pass
DEV readiness          Pass
QA deployment          Pass
QA /health             Pass
QA config check        Fail
QA database check      Pass
Overall QA readiness   Fail
```

That would give you a very clean third failure mode.

* * *
You should see something like: 

````
Run if ('NotReady' -ne 'Ready') {...
Write-Error: Environment NotReady (httpStatus=503).
Error: Process completed with exit code 1.
````

This is you your third deliberate failure case.

This run demonstrates a different failure category from the previous two:

| Experiment | Deployment | Liveness | Specific failure | Overall readiness |
| --- | --- | --- | --- | --- |
| Missing database | Succeeded | Pass | Required DB absent | NotReady |
| Wrong QA connection string | Succeeded | Pass | DB connection invalid | NotReady |
| Invalid QA configuration | Succeeded | Pass | Required config invalid/missing | NotReady |

And again, the failure surfaced at the **readiness gate**:

```
Environment NotReady (httpStatus=503)
```

So the system is behaving consistently: deployment completes, the application starts, validation detects an unusable condition, and promotion/use is blocked.

That checks off **invalid configuration**.

### One failure experiment remains: unavailable dependency

I’d make this one clearly different from the database tests by introducing a simple external HTTP dependency.

For example, configure QA with:

```PowerShell
$env:ExternalDependency__Url = "http://localhost:5999/health"
```

with nothing listening on port `5999`.

Then `/readiness` can attempt a short HTTP request to that URL.

The experiment should test:

```
Application
    │
    ├── Configuration       PASS
    ├── Database            PASS
    └── External service    FAIL
                              │
                              ▼
                         NOT READY
```

Before we run it, I would expect:

| Stage | Prediction |
| --- | --- |
| Build | Pass |
| DEV readiness | Pass |
| QA deployment | Pass |
| QA `/health` | Pass |
| QA configuration | Pass |
| QA database | Pass |
| External dependency | Fail |
| Overall QA readiness | Fail |

This one is valuable because it demonstrates that **your application can be healthy and correctly configured while the overall environment is still unsuitable because something outside the application is unavailable**.

Your current `/readiness` design is already set up nicely for this experiment because you have the same pattern we want for an external dependency: **conditional requirement → blocking check when required → evidence captured in the response**.

For the last Week 3 failure case, I’d add a configurable HTTP dependency rather than hard-coding a failure.

First register `HttpClient` near your other services:

```C#
builder.Services.AddSingleton<PolicyService.Services.PolicyService>();
builder.Services.AddHttpClient();
```

Then add `IHttpClientFactory` to the readiness endpoint:

```C#
app.MapGet("/readiness", async (
    IConfiguration configuration,
    PolicyService.Services.PolicyService policyService,
    IHttpClientFactory httpClientFactory) =>
{
```

Then add this check alongside your database and smoke-test checks:

```C#
var requireExternalDependency =
    configuration.GetValue<bool>("Readiness:RequireExternalDependency");

if (!requireExternalDependency)
{
    checks.Add(new(
        Name: "external-dependency",
        Category: CheckCategory.Dependency,
        Enforcement: Enforcement.Conditional,
        Result: CheckResult.Skipped,
        Detail: "External dependency not required for this deployment scope"));
}
else
{
    var dependencyUrl = configuration["ExternalDependency:Url"];

    var (result, detail) = await CheckExternalDependencyAsync(
        httpClientFactory,
        dependencyUrl);

    checks.Add(new(
        Name: "external-dependency",
        Category: CheckCategory.Dependency,
        Enforcement: Enforcement.Blocking,
        Result: result,
        Detail: detail));
}
```

Then add a helper similar to your database checker:

```C#
static async Task<(CheckResult Result, string Detail)>
    CheckExternalDependencyAsync(
        IHttpClientFactory httpClientFactory,
        string? dependencyUrl)
{
    if (string.IsNullOrWhiteSpace(dependencyUrl))
    {
        return (
            CheckResult.Fail,
            "ExternalDependency:Url is not configured");
    }

    if (!Uri.TryCreate(
        dependencyUrl,
        UriKind.Absolute,
        out var uri))
    {
        return (
            CheckResult.Fail,
            $"ExternalDependency:Url is invalid: '{dependencyUrl}'");
    }

    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(2);

        using var response = await client.GetAsync(uri);

        if (response.IsSuccessStatusCode)
        {
            return (
                CheckResult.Pass,
                $"Dependency responded with HTTP {(int)response.StatusCode}");
        }

        return (
            CheckResult.Fail,
            $"Dependency returned HTTP {(int)response.StatusCode}");
    }
    catch (Exception ex)
    {
        return (
            CheckResult.Fail,
            $"Dependency unavailable: {ex.GetType().Name}: {ex.Message}");
    }
}
```

### First prove the normal path still works

For DEV and QA initially set:

```PowerShell
$env:Readiness__RequireExternalDependency = "false"
```

Run the pipeline once and make sure both environments are still `Ready`.

Your readiness response should now contain something like:

```JSON
{
  "name": "external-dependency",
  "category": "Dependency",
  "enforcement": "Conditional",
  "result": "Skipped",
  "detail": "External dependency not required for this deployment scope"
}
```

That verifies we didn't accidentally break the existing system.

### Then run the failure experiment

For **QA only**, set:

```PowerShell
$env:Readiness__RequireExternalDependency = "true"
$env:ExternalDependency__Url = "http://127.0.0.1:5999/health"
```

Don't put anything on port `5999`.

DEV can continue with:

```PowerShell
$env:Readiness__RequireExternalDependency = "false"
```

Now we have a deliberately unavailable dependency rather than invalid configuration.

I'd predict:

| Stage | Expected |
| --- | --- |
| Build | Pass |
| DEV deployment | Pass |
| DEV readiness | Ready |
| QA approval | Allowed |
| QA deployment execution | Pass |
| QA `/health` | Pass |
| Required configuration | Pass |
| Database | Pass |
| Policy smoke test | Whatever your current policy requires |
| External dependency | **Fail** |
| Overall QA readiness | **NotReady / 503** |

The distinction we're testing is:

```
PolicyService itself
    HEALTHY

Database
    HEALTHY

Configuration
    VALID

External HTTP dependency
    UNAVAILABLE
         │
         ▼
Environment
    NOT READY
```

That gives you a genuinely different fourth failure mode from the first three.

It also fits the reasoning you established earlier: a dependency shouldn't universally block readiness just because it exists. Your `RequireExternalDependency` setting makes its enforcement depend on the intended deployment/testing scope. That is exactly the kind of readiness-policy reasoning Week 3 is trying to develop.

Once this produces the expected `503`, all four required failure-injection cases will be complete, and I'd stop modifying the pipeline and move us into **evidence comparison → required diagram → metadata schema → knowledge check/retrospective**.

* * *

It should have failed with this message: 

{

          "name": "external-dependency",

          "category": "Dependency",

          "enforcement": "Blocking",

          "result": "Fail",

          "detail": "Dependency unavailable: TaskCanceledException: The request was canceled due to the configured HttpClient.Timeout of 2 seconds elapsing."

        }

That completes the **unavailable dependency** experiment.

The important part is that the readiness system didn't just return `NotReady`; it captured **why**:

```JSON
{
  "name": "external-dependency",
  "category": "Dependency",
  "enforcement": "Blocking",
  "result": "Fail",
  "detail": "Dependency unavailable: TaskCanceledException: The request was canceled due to the configured HttpClient.Timeout of 2 seconds elapsing."
}
```

That tells us several useful things at once:

```
PolicyService process     = running
Deployment execution      = succeeded
Configuration             = valid
Database                  = available
External dependency       = unavailable / non-responsive
Overall readiness         = NotReady
```

The timeout is actually a good failure mode for this test. It demonstrates something slightly different from "connection refused": the dependency might exist somewhere on the network but is not responding within the service's acceptable time budget.

That means you've now completed all four failure cases required by Week 3.

| Experiment | Deployment execution | Environment readiness | Failure category |
| --- | --- | --- | --- |
| Missing database | Succeeded | NotReady | Missing environmental resource |
| Wrong connection string | Succeeded | NotReady | Incorrect environment configuration |
| Invalid required configuration | Succeeded | NotReady | Invalid application configuration |
| Unavailable dependency | Succeeded | NotReady | Runtime dependency failure |

And you've also demonstrated the successful case:

```
Deployment execution = Succeeded
Readiness             = Ready
Promotion             = Allowed
```

So at this point, I would **stop changing the pipeline**. We've gathered the technical evidence we need.

## Where we are now

The implementation portion of Week 3 is essentially complete:

* Build once ✅
* Immutable artifact ✅
* Promote same artifact to DEV and QA ✅
* Environment-specific configuration ✅
* DEV validation ✅
* Manual QA approval ✅
* QA validation ✅
* Deployment provenance ✅
* Missing DB failure ✅
* Wrong connection string failure ✅
* Invalid configuration failure ✅
* Unavailable dependency failure ✅
* Deployment success separated from environment readiness ✅

The workbook's remaining Week 3 work is mostly about turning what you've learned into durable evidence: the required **"Deployment Success vs. Environment Readiness" diagram**, a small **deployment metadata schema**, the reflection questions, knowledge check, and retrospective.

### I suggest we do the portfolio artifact next

Before drawing the diagram, we should capture the model your experiments actually proved.

You effectively observed these four states:

```
                    ENVIRONMENT
                 Ready       Not Ready
              ┌───────────┬─────────────┐
Deployment    │           │             │
Succeeded     │ NORMAL    │ VALIDATION  │
              │           │ FAILURE     │
              ├───────────┼─────────────┤
Failed        │   N/A     │ DEPLOYMENT  │
              │           │ FAILURE     │
              └───────────┴─────────────┘
```

The interesting quadrant is:

```
Deployment = Succeeded
Environment = NotReady
```

because you now have **four concrete examples** proving that this state is real.

I think that's exactly what your Week 3 diagram should communicate.

That completes the **diagram half of the Week 3 portfolio deliverable**.

The other required piece is the **small deployment metadata schema**. The workbook specifically calls for the diagram plus a metadata schema, and your pipeline already generates most of the fields we need, so this should be mostly documentation rather than new implementation.

I’d make the schema describe these sections:

* **Artifact identity** — artifact ID, version, commit, branch, build time
* **Deployment execution** — target environment, deployment time, execution result, failure reason, pipeline/run ID
* **Validation** — readiness endpoint, HTTP status, outcome, timestamp, and detailed evidence/check results

If you want, next we can turn your existing `deploy-metadata-DEV.json` / `deploy-metadata-QA.json` structure into a clean portfolio-ready schema and briefly explain why each field exists.

* * *

I’d make this a small Markdown artifact, probably:

```
docs/week-03/deployment-metadata-schema.md
```

The workbook requires the Week 3 portfolio artifact to include both the diagram and a small deployment metadata schema, and it specifically calls out artifact ID, commit, branch, build time, deployment time, target environment, and validation result.

Here’s a portfolio-ready version based on the pipeline you actually built.

````Markdown
# Deployment Metadata Schema

## Purpose

The deployment metadata record provides traceability between:

1. The immutable application artifact that was built.
2. The environment into which that artifact was deployed.
3. The result of deployment execution.
4. The readiness validation performed after deployment.

Deployment execution and environment readiness are recorded separately because a deployment
can complete successfully while the resulting environment is not usable.

---

## Schema

{
  "artifact": {
    "id": "PolicyService-0.2.16-2fccfbd6",
    "version": "0.2.16",
    "commit": "2fccfbd6640e2679f934de2a6360bcd34a85652b",
    "branch": "refs/heads/main",
    "buildTimeUtc": "2026-09-19T03:59:06Z"
  },
  "deployment": {
    "targetEnvironment": "QA",
    "deployTimeUtc": "2026-09-19T04:10:00Z",
    "executionResult": "Succeeded",
    "failureReason": null,
    "runId": "123456789"
  },
  "validation": {
    "endpoint": "/readiness",
    "httpStatus": 200,
    "outcome": "Ready",
    "capturedAtUtc": "2026-09-19T04:10:05Z",
    "evidence": {
      "status": "Ready",
      "environment": "QA",
      "checks": [
        {
          "name": "required-configuration",
          "category": "Config",
          "enforcement": "Blocking",
          "result": "Pass",
          "detail": "EnvironmentSettings:DisplayName = 'QA'"
        },
        {
          "name": "database",
          "category": "Dependency",
          "enforcement": "Blocking",
          "result": "Pass",
          "detail": "Database connection succeeded"
        }
      ]
    }
  }
}

## Field Definitions

| Field | Purpose |
| --- | --- |
| `artifact.id` | Unique identifier for the immutable artifact being promoted. |
| `artifact.version` | Human-readable application version. |
| `artifact.commit` | Source commit from which the artifact was produced. |
| `artifact.branch` | Source branch or ref associated with the build. |
| `artifact.buildTimeUtc` | Time the artifact was originally built. |
| `deployment.targetEnvironment` | Environment receiving the artifact, such as DEV or QA. |
| `deployment.deployTimeUtc` | Time the deployment was attempted. |
| `deployment.executionResult` | Whether deployment execution itself succeeded or failed. |
| `deployment.failureReason` | Reason deployment execution failed, if applicable. |
| `deployment.runId` | CI/CD workflow run associated with the deployment. |
| `validation.endpoint` | Endpoint used to evaluate environment readiness. |
| `validation.httpStatus` | HTTP status returned by readiness validation. |
| `validation.outcome` | Overall readiness result, such as `Ready`, `NotReady`, or `NotEvaluated`. |
| `validation.capturedAtUtc` | Time validation evidence was captured. |
| `validation.evidence` | Detailed machine-readable results explaining the readiness outcome. |

## Result Semantics

### Deployment execution

`Succeeded`

The artifact was acquired, verified, installed, configured, and the application process was  
successfully started.

`Failed`

The deployment process itself could not be completed.

### Environment readiness

`Ready`

All blocking readiness checks passed.

`NotReady`

Deployment succeeded, but one or more blocking validation checks failed.

`NotEvaluated`

Deployment execution failed before meaningful environment validation could be performed.

## Promotion Invariants

The following artifact fields must remain unchanged when promoting the artifact from DEV to QA:

* Artifact ID
* Version
* Commit
* Branch
* Build time

The following fields are expected to differ between environments:

* Target environment
* Deployment time
* Validation result and evidence

This demonstrates artifact promotion: the same previously-built artifact is moved between  
environments rather than rebuilt for each environment.

````

I particularly like the final **Promotion Invariants** section for your portfolio. It turns the schema from “here are some JSON fields” into an architectural statement:

```text
DEV record                QA record
──────────                ─────────
artifact.id       ──────► SAME
version           ──────► SAME
commit            ──────► SAME
branch            ──────► SAME
buildTimeUtc      ──────► SAME

environment       ──────► DIFFERENT
deployTimeUtc     ──────► DIFFERENT
validation        ──────► INDEPENDENT RESULT
````

That directly demonstrates that you understand **provenance** rather than merely collecting metadata.

One optional improvement I'd consider is adding:

```JSON
"schemaVersion": "1.0"
```

at the root in the future. You don't need it for Week 3, but if this record evolves in later weeks, especially when you reach the Environment Certification Service, versioning the metadata contract gives consumers a way to distinguish record formats. I wouldn't add anything else right now; your current model is appropriately small for the workbook's requirement.


* * *
## Finishing Out Week 3

The implementation and portfolio work are essentially complete, so the remaining Week 3 work is the **reflection prompts, knowledge check, and weekly retrospective**. The workbook also calls for an end-of-Week-3 review where you should be able to explain artifact promotion and environment readiness without notes.

You’ve already completed the practical requirements: one immutable artifact promoted through DEV and QA, deployment-time configuration, provenance, readiness checks, a manual QA approval gate, and all four required failure experiments. You also now have the required **“Deployment Success vs. Environment Readiness”** diagram and deployment metadata schema.

Let’s finish the written portion. Answer these **from memory**; don’t consult the workbook answer key yet.

## Week 3 Reflection

1. What conditions should block promotion?

Promotion should be blocked when required gates for the target environment fail. These may include approval, application accessibility, required configuration, dependency availability, and required smoke or readiness tests. The exact blocking conditions depend on the intended use and risk tolerance of the target environment.

2. Which validation checks belong immediately after deployment?

Immediately after deployment, validation should check that the application or service started successfully, required endpoints are accessible, required configuration is valid, and critical dependencies are reachable. Where appropriate, a small smoke test can verify a critical workflow.

3. Could your validation run even if a different deployment tool were used? Explain why or why not.

Yes. The readiness process is independent of the deployment tool because it evaluates the resulting environment rather than the deployment mechanism. Any deployment process could deploy and configure the application, then invoke the same readiness validation and record its results.

## Knowledge Check

1. Can a deployment technically succeed while the environment is unusable? Explain.

Deployment execution can succeed while the resulting environment is still unusable because of configuration, dependency, or accessibility failures.

2. What is artifact promotion?

The same previously built artifact moves through successive environments without being rebuilt.

3. What information establishes deployment provenance?

Deployment provenance is the information that lets us trace exactly what artifact was deployed, where it came from, when and where it was deployed, and by what deployment run or actor.

4. Should environment validation be part of the artifact build? Why or why not?

No, environment validation should not be part of the artifact build. These are two separate concepts that communicate different information according to a specific context. The artifact is prepared/built before a context is given.

5. Why might post-deployment validation be independent of the deployment tool?

Post-deployment validation should be independent of the deployment tool so the same validation can be reused across different deployment mechanisms and survive a future platform migration. It verifies the resulting environment, not the tool that created it.

6. What is a deployment gate?

A deployment gate is a rule, automated check, or manual approval that must pass before deployment or promotion is allowed to continue.

## Weekly Retrospective

**A. What became clearer this week?**

**B. What was harder than expected?**

**C. What would you do differently in a production system?**

**D. What artifact from this week best demonstrates growth?**

## Week 3 completion check

The workbook's Week 3 goals were to promote the same artifact through DEV and QA, externalize environment configuration, capture deployment provenance, and use post-deployment validation and approvals.

You've now completed:

* ✅ **Build once and promote the same artifact**
* ✅ **DEV deployment**
* ✅ **DEV health/readiness validation**
* ✅ **Manual approval before QA**
* ✅ **QA deployment**
* ✅ **QA health/readiness validation**
* ✅ **Independent DEV and QA databases**
* ✅ **Environment-specific configuration supplied at deployment time**
* ✅ **Artifact integrity verification with SHA-256**
* ✅ **Deployment provenance records**
* ✅ **Deployment execution recorded separately from readiness**
* ✅ **Missing database experiment**
* ✅ **Wrong connection string experiment**
* ✅ **Invalid configuration experiment**
* ✅ **Unavailable dependency experiment**
* ✅ **Required “Deployment Success vs. Environment Readiness” diagram**
* ✅ **Deployment metadata schema**
* ✅ **Week 3 reflection questions**
* ✅ **Week 3 knowledge check**
* ✅ **Week 3 retrospective**

Your failure experiments were particularly useful because all four demonstrated the same important state:

```
Deployment Execution = Succeeded
          +
Environment Validation = Failed
          =
Environment = NotReady
```

while identifying four different causes:

```
Missing database
Wrong connection string
Invalid required configuration
Unavailable external dependency
```

That gives you actual experimental evidence for the architectural distinction rather than just a definition.

### Before committing Week 3

I would make sure the repository contains something approximately like:

```
docs/week-03/
├── deployment-success-vs-readiness.png
├── deployment-metadata-schema.md
├── experiments.md
└── retrospective.md
```

`experiments.md` doesn't have to be elaborate. A table showing predicted versus actual results for your four failures would make this week's portfolio evidence considerably stronger.

At this point, I would mark **Week 3 complete**.

The workbook's suggested end-of-Week-3 review is whether you can explain **artifact promotion** and **environment readiness** without notes. Based on your knowledge-check answers and the experiments you ran, you've demonstrated both.

**Week 4 is Architecture Communication and ADRs**, where we'll take a lot of what you just built and learn how to represent it at different architectural levels rather than continuing to expand the pipeline.

* * *

## Week 3 Expirements doc: 

Your week 3 expirements doc (expirements.md) should look something like this: 

````
# Week 3 Experiments: Continuous Deployment and Artifact Promotion

## Purpose

The Week 3 experiments were designed to test the difference between **deployment execution** and **environment readiness** while promoting the same immutable application artifact through DEV and QA.

The pipeline used the following flow:

```text
Build
  ↓
Publish immutable artifact
  ↓
Deploy DEV
  ↓
DEV health/readiness validation
  ↓
Manual approval
  ↓
Deploy same artifact to QA
  ↓
QA health/readiness validation
```

The artifact was built once, packaged with provenance metadata, verified with SHA-256 before deployment, and then reused in both DEV and QA. Environment-specific configuration was supplied at deployment time rather than embedded in the artifact.

The readiness endpoint evaluated several types of checks:

- Required configuration
- Database connectivity
- Policy smoke test when required
- External dependency availability when required

The key question for each experiment was:

> Did deployment execution succeed, and was the resulting environment ready for its intended use?

---

## Baseline: Successful DEV and QA Promotion

Before injecting failures, the pipeline was run through the complete happy path.

### Expected Result

- Build succeeds
- Artifact integrity check succeeds
- DEV deployment succeeds
- DEV health check succeeds
- DEV readiness succeeds
- Manual approval is required before QA
- QA deployment succeeds
- QA health check succeeds
- QA readiness succeeds

### Actual Result

All stages passed as expected.

The same artifact was promoted from DEV to QA without rebuilding. DEV and QA used separate environment-specific configuration and separate LocalDB databases.

### What This Demonstrated

This established the baseline for artifact promotion:

- The artifact identity remained unchanged between DEV and QA.
- The target environment, deployment time, configuration, and validation results were environment-specific.
- Promotion to QA occurred only after DEV readiness passed and a manual approval gate was satisfied.

---

# Experiment 1: Missing Database

## Goal

Determine whether a deployment can succeed even when a required environmental dependency is missing.

The DEV database provisioning step was intentionally disabled while database readiness remained required.

### Prediction

| Stage | Prediction |
|---|---|
| Artifact integrity | Pass |
| Deployment execution | Pass |
| `/health` | Pass |
| Database readiness | Fail |
| Overall DEV readiness | Fail |

### Actual Result

The prediction matched the actual behavior.

- Artifact integrity passed.
- Deployment execution succeeded.
- The application became live and `/health` returned successfully.
- Database readiness failed.
- `/readiness` returned HTTP 503.
- The readiness gate failed with:

```text
Environment NotReady (httpStatus=503)
```

After the database provisioning step was restored, the same pipeline passed.

## Interpretation

The application artifact was valid and the deployment mechanism completed successfully. The environment was not ready because a required database dependency was absent.

This demonstrated that **environment readiness is a property of the deployed system and its dependencies, not only of the application artifact**.

---

# Experiment 2: Incorrect QA Connection String

## Goal

Determine whether an environment-specific configuration error could cause QA readiness to fail even though the same artifact had already passed in DEV.

The QA connection string was intentionally changed to reference a database that did not exist.

### Prediction

| Stage | Prediction |
|---|---|
| Build | Pass |
| DEV readiness | Pass |
| QA deployment execution | Pass |
| QA `/health` | Pass |
| QA database readiness | Fail |
| Overall QA readiness | Fail |

### Actual Result

The prediction matched the actual behavior.

- Build passed.
- DEV deployment and readiness passed.
- The artifact was approved for promotion to QA.
- QA deployment execution succeeded.
- QA `/health` passed.
- QA database readiness failed.
- QA `/readiness` returned HTTP 503.
- The readiness gate blocked the environment.

## Interpretation

Because the identical artifact passed in DEV and failed only after QA-specific configuration was injected, the failure was clearly environment-specific rather than an artifact defect.

This demonstrated the value of separating:

- artifact identity,
- deployment execution,
- environment configuration,
- and environment readiness.

---

# Experiment 3: Invalid Required Configuration

## Goal

Verify that readiness validation detects a missing or invalid required application setting even when the application process can still start.

The QA value for:

```text
EnvironmentSettings:DisplayName
```

was intentionally removed or supplied as an invalid empty value.

The readiness endpoint treats this setting as required configuration.

### Prediction

| Stage | Prediction |
|---|---|
| Build | Pass |
| DEV readiness | Pass |
| QA deployment execution | Pass |
| QA `/health` | Pass |
| QA configuration check | Fail |
| QA database check | Pass |
| Overall QA readiness | Fail |

### Actual Result

The configuration validation failed as expected.

The pipeline reached the readiness gate and failed with:

```text
Environment NotReady (httpStatus=503)
```

Deployment execution and application liveness still succeeded.

## Interpretation

This experiment isolated configuration validity from application liveness.

A running process was not considered sufficient evidence that the environment was usable. Required runtime configuration had to satisfy the readiness policy before the environment could be considered ready.

---

# Experiment 4: Unavailable External Dependency

## Goal

Verify that readiness validation can detect the failure of a required external runtime dependency.

An external dependency readiness check was added using a two-second `HttpClient` timeout.

For QA, the dependency was marked as required and configured to target an endpoint that was unavailable.

### Prediction

| Stage | Prediction |
|---|---|
| Build | Pass |
| DEV readiness | Pass |
| QA deployment execution | Pass |
| QA `/health` | Pass |
| QA required configuration | Pass |
| QA database readiness | Pass |
| External dependency | Fail |
| Overall QA readiness | Fail |

### Actual Result

The external dependency check failed with the following evidence:

```json
{
  "name": "external-dependency",
  "category": "Dependency",
  "enforcement": "Blocking",
  "result": "Fail",
  "detail": "Dependency unavailable: TaskCanceledException: The request was canceled due to the configured HttpClient.Timeout of 2 seconds elapsing."
}
```

The overall environment readiness result was `NotReady`.

## Interpretation

The PolicyService process itself remained healthy, configuration was valid, and the database was available. The environment was still considered not ready because a dependency required for that deployment scope was unavailable.

This demonstrated that readiness can include dependencies outside the application process itself.

---

# Experiment Summary

| Experiment | Deployment Execution | Liveness | Failing Check | Environment Readiness |
|---|---|---|---|---|
| Baseline successful deployment | Succeeded | Pass | None | Ready |
| Missing database | Succeeded | Pass | Database | NotReady |
| Incorrect QA connection string | Succeeded | Pass | Database | NotReady |
| Invalid required configuration | Succeeded | Pass | Required configuration | NotReady |
| Unavailable external dependency | Succeeded | Pass | External dependency | NotReady |

The repeated pattern was:

```text
Deployment Execution = Succeeded
Environment Validation = Failed
Environment = NotReady
```

The experiments showed that deployment success and readiness are related but independent outcomes.

---

# Deployment and Validation Model

The resulting model used three distinct states:

```text
Deployment execution failed
    ↓
Readiness = NotEvaluated

Deployment execution succeeded
    ↓
Blocking readiness check failed
    ↓
Readiness = NotReady

Deployment execution succeeded
    ↓
All blocking readiness checks passed
    ↓
Readiness = Ready
```

This avoids incorrectly treating all pipeline failures as deployment failures.

---

# Key Lessons

## 1. Deployment success does not prove environment readiness

A deployment mechanism can successfully install and start an application while configuration, databases, or external dependencies still make the environment unusable.

## 2. Artifact promotion reduces ambiguity

The exact same artifact was promoted from DEV to QA. If DEV passed and QA failed after environment-specific configuration was applied, the investigation could focus on environmental differences rather than questioning whether QA received different application bits.

## 3. Readiness policy is contextual

Not every readiness check must always be blocking.

The Week 3 design allowed checks to be either blocking or conditional based on the intended deployment or testing scope. For example, database connectivity or a policy workflow may be required for one QA cycle but unnecessary for another.

## 4. Liveness and readiness answer different questions

`/health` answered whether the application process was available.

`/readiness` answered whether the deployed environment met the conditions required for its intended use.

A successful health check therefore did not automatically imply that the environment was ready.

## 5. Validation should capture evidence before enforcing policy

The pipeline recorded readiness evidence and deployment provenance before failing the readiness gate.

The pattern was:

```text
Observe
  ↓
Record evidence
  ↓
Publish provenance
  ↓
Enforce promotion policy
```

This preserved diagnostic information even when an environment failed validation.

## 6. Post-deployment validation can remain independent of the deployment mechanism

The readiness process evaluated the resulting environment rather than the GitHub Actions deployment mechanism itself.

The same validation concept could therefore be reused with another deployment platform as long as that platform can:

- deploy the artifact,
- provide environment-specific configuration,
- invoke the readiness endpoint,
- and record the result.

---

# Conclusion

The Week 3 experiments demonstrated that software delivery should not treat a successful deployment command as proof that an environment is usable.

The pipeline now distinguishes between:

1. **Artifact correctness and identity**
2. **Deployment execution**
3. **Environment readiness**
4. **Promotion approval**

The most important observed state was:

```text
Deployment = Succeeded
Environment = NotReady
```

All four injected failures produced this result for different reasons, providing direct evidence for the value of independent post-deployment validation and artifact promotion.

````