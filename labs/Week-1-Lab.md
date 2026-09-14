**Week 1**

The workbook’s intent is specifically to build **one coherent repository** over the full ten weeks, with each week adding engineering capability and a communication artifact. Week 1 is about reasoning about delivery as an end-to-end system, not merely learning how to write a pipeline.

For the public project, I suggest we create something like:

```
technical-leadership-lab/
│
├── src/
│   └── PolicyService/
│
├── tests/
│   └── PolicyService.Tests/
│
├── scripts/
│   └── build.ps1
│
├── pipelines/
│
├── docs/
│   ├── diagrams/
│   ├── adrs/
│   ├── reliability/
│   ├── proposals/
│   └── week-01/
│       ├── delivery-system-map.md
│       └── retrospective.md
│
├── .gitignore
├── README.md
└── TechnicalLeadershipLab.sln
```

That closely follows the workbook's recommended portfolio structure while giving us room to grow through the later architecture, reliability, observability, and ADR exercises.

### What we're building in Week 1

The actual Week 1 lab is intentionally small:

* a .NET Web API
* `GET /health`
* `GET /policies/{id}`
* `POST /policies`
* a unit-test project
* a build script
* a version number
* one configuration value that differs between DEV and QA
* **no automated deployment yet**

Then we draw the lifecycle from developer commit through release acceptance and identify where state changes, human intervention, failure introduction, and failure detection occur.

The portfolio deliverable at the end is a **one-page Software Delivery System Map** organized around **Control, Artifact, and Verification**.

I think we should use a modern ASP.NET Core API rather than mimic the older .NET Framework systems you work with professionally. That keeps this portfolio project transferable and prevents it from becoming a disguised copy of a company system. The workbook explicitly recommends keeping the work generic enough to demonstrate transferable skill.

A simple fictional **Policy Service** is perfect. It gives us enough business behavior later for databases, observability, synthetic transactions, failure injection, and SLOs without turning the project into an insurance application.

## First session: establish the repository

Let's start very small. On your machine, create an empty folder and run:

```PowerShell
mkdir technical-leadership-lab
cd technical-leadership-lab

git init

dotnet new sln -n TechnicalLeadershipLab

dotnet new webapi -n PolicyService -o src/PolicyService
dotnet new mstest -n PolicyService.Tests -o tests/PolicyService.Tests

dotnet sln add src/PolicyService/PolicyService.csproj
dotnet sln add tests/PolicyService.Tests/PolicyService.Tests.csproj

dotnet add tests/PolicyService.Tests/PolicyService.Tests.csproj reference src/PolicyService/PolicyService.csproj
```

I'd use **MSTest** here because you're already familiar with it. The point of this project isn't to prove you can learn xUnit syntax; it's to build delivery and architecture skills.

Before we write the endpoints, though, I want to establish one habit that will matter throughout all ten weeks:

> **Every technical thing we add should have a reason for existing in the delivery system.**

For example, `/health` isn't merely another API endpoint. It becomes a **verification interface** that later lets another component determine something about the state of the running system.

That's the type of shift Week 1 is trying to create.

### Public GitHub repository

Once the skeleton exists locally, I'd create a public repository named something like:

**`technical-leadership-lab`**

with a short description such as:

> A project-based study of software delivery, architecture, observability, reliability, and technical decision-making using a small .NET service.

Don't initialize the GitHub repository with a README or `.gitignore` if we've already created those locally. Then you'll just connect the remote:

```PowerShell
git remote add origin https://github.com/YOUR-USERNAME/technical-leadership-lab.git

git branch -M main
git add .
git commit -m "Initialize technical leadership lab"
git push -u origin main
```

I can't publish directly to your GitHub account from this workspace because the GitHub integration available here is disabled by your organization's administrator, but we can build everything together locally and you can push each milestone.

One thing I **don't** want to do is give you the Week 1 knowledge-check answers. The workbook explicitly says to answer those from memory before checking the answer key. When we reach that point, I'll quiz you rather than supplying the answers.

For now, run these two commands and record their output:

```PowerShell
dotnet --version
git --version
```

Then we'll build the initial Policy API together and make the **first meaningful design decisions** rather than just scaffolding a demo.

* * *

.NET 9 is a natural fit for this public portfolio project.

Next, let’s scaffold the solution exactly around the Week 1 requirements: a small Web API, unit-test project, build script, versioning, and later one DEV/QA configuration difference.

Run this from:

```PowerShell
C:\Users\stith_b\source\repos\technical-leadership-lab
```

```PowerShell
dotnet new sln -n TechnicalLeadershipLab

dotnet new webapi -n PolicyService -o src\PolicyService
dotnet new mstest -n PolicyService.Tests -o tests\PolicyService.Tests

dotnet sln TechnicalLeadershipLab.sln add src\PolicyService\PolicyService.csproj
dotnet sln TechnicalLeadershipLab.sln add tests\PolicyService.Tests\PolicyService.Tests.csproj

dotnet add tests\PolicyService.Tests\PolicyService.Tests.csproj reference src\PolicyService\PolicyService.csproj
```

Then verify the baseline:

```PowerShell
dotnet build
dotnet test
```

A note before you run it: the .NET 9 `webapi` template may create some example weather endpoints and may use minimal APIs. That’s fine. We’ll replace the template behavior with the three workbook endpoints:

```
GET  /health
GET  /policies/{id}
POST /policies
```

The Week 1 goal is not architectural sophistication yet. It’s to create just enough working software that we can reason about how a source change becomes running software and where control, artifacts, verification, people, and failure points exist.

Once those commands complete, record the output from:

```PowerShell
dotnet build
dotnet test
```

It should look something like this:

PS C:\Users\stith_b\source\repos\technical-leadership-lab> dotnet build
Restore complete (18.2s)
  PolicyService succeeded (12.0s) → src\PolicyService\bin\Debug\net9.0\PolicyService.dll
  PolicyService.Tests succeeded (2.6s) → tests\PolicyService.Tests\bin\Debug\net9.0\PolicyService.Tests.dll

Build succeeded in 34.7s

Workload updates are available. Run dotnet workload list for more information.
PS C:\Users\stith_b\source\repos\technical-leadership-lab> dotnet test 
Restore complete (2.1s)
  PolicyService succeeded (1.0s) → src\PolicyService\bin\Debug\net9.0\PolicyService.dll
  PolicyService.Tests succeeded (0.3s) → tests\PolicyService.Tests\bin\Debug\net9.0\PolicyService.Tests.dll
Test Parallelization enabled for C:\Users\stith_b\source\repos\technical-leadership-lab\tests\PolicyService.Tests\bin\Debug\net9.0\PolicyService.Tests.dll (Workers: 8, Scope: MethodLevel)
  PolicyService.Tests test succeeded (3.1s)

Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, duration: 3.0s
Build succeeded in 7.9s

Workload updates are available. Run dotnet workload list for more information.

* * *

You now have a clean baseline: the solution restores, compiles, and the test project executes successfully.

That means we can move to the first real Week 1 implementation step: build the three required endpoints, keeping the service intentionally small. The workbook asks specifically for `GET /health`, `GET /policies/{id}`, and `POST /policies`.

I’d keep this first version as a minimal API with an in-memory policy store. We do **not** want to introduce a database yet, because that would add infrastructure concerns before Week 1 needs them.

Replace the contents of `src\PolicyService\Program.cs` with:

```C#
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var policies = new Dictionary<int, Policy>();

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTimeOffset.UtcNow
    });
});

app.MapGet("/policies/{id:int}", (int id) =>
{
    return policies.TryGetValue(id, out var policy)
        ? Results.Ok(policy)
        : Results.NotFound();
});

app.MapPost("/policies", (CreatePolicyRequest request) =>
{
    var nextId = policies.Count == 0
        ? 1
        : policies.Keys.Max() + 1;

    var policy = new Policy(
        nextId,
        request.PolicyHolderName,
        request.PolicyType,
        DateTimeOffset.UtcNow);

    policies[nextId] = policy;

    return Results.Created($"/policies/{nextId}", policy);
});

app.Run();

public record CreatePolicyRequest(
    string PolicyHolderName,
    string PolicyType);

public record Policy(
    int Id,
    string PolicyHolderName,
    string PolicyType,
    DateTimeOffset CreatedAt);
```

Then run:

```PowerShell
dotnet build
dotnet run --project src\PolicyService
```

The console should give you a URL such as:

```
http://localhost:5000
```

or:

```
https://localhost:7000
```

Use the actual URL your application reports.

You can test the health endpoint with:

```PowerShell
Invoke-RestMethod http://localhost:5000/health
```

Then create a policy:

```PowerShell
$body = @{
    policyHolderName = "Jane Doe"
    policyType = "Homeowners"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri http://localhost:5000/policies `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

Then retrieve it:

```PowerShell
Invoke-RestMethod http://localhost:5000/policies/1
```

You should end up proving this flow:

```
POST /policies
      │
      ▼
Policy created in memory
      │
      ▼
GET /policies/1
      │
      ▼
Same policy returned
```

There is already an interesting delivery-system lesson hidden here.

Our application has **state**, but right now that state only exists inside the running process:

```
Process starts
     │
     ▼
Dictionary<int, Policy>
     │
     ├── POST adds state
     │
     └── GET reads state
     │
     ▼
Process stops
     │
     ▼
State disappears
```

That is completely acceptable for this stage. In fact, it gives us a useful distinction later between:

* source state
* build artifacts
* configuration state
* application runtime state
* persistent data state

That way of categorizing state is exactly the kind of thinking the Week 1 exercise is trying to encourage when it asks you to identify where **state changes** happen throughout delivery.

### One small design decision

Notice that `/health` currently says only:

```JSON
{
  "status": "Healthy"
}
```

Conceptually, that means:

> "The application process is running and can serve an HTTP request."

It does **not** mean:

> "The entire system is ready for business use."

That distinction will become important later in the workbook when we separate deployment success from environment readiness. For Week 1, a simple liveness-style health endpoint is exactly what we want.

Once you've verified all three endpoints, the next step should be **unit testing the policy behavior**, and then we'll add the DEV/QA configuration difference. After that, we'll have enough material to start drawing the delivery-system map instead of inventing it abstractly.

* * *

We’re at the point where Week 1 becomes more useful than just scaffolding.

The workbook’s remaining hands-on work for this week is to add a unit-test project, a build script, a version number, one configuration value that differs between DEV and QA, and then manually map the lifecycle from commit through release acceptance.

Let’s do the code changes in this order:

1. Extract the policy behavior so it’s testable.
2. Add meaningful unit tests.
3. Add environment-specific configuration.
4. Add version information.
5. Add a build script.
6. Create the Week 1 delivery-system map and retrospective.

### Step 1: move policy behavior out of `Program.cs`

Create:

```
src\PolicyService\Services\PolicyService.cs
```

with:

```C#
namespace PolicyService.Services;

public class PolicyService
{
    private readonly Dictionary<int, Policy> _policies = new();

    public Policy CreatePolicy(CreatePolicyRequest request)
    {
        var nextId = _policies.Count == 0
            ? 1
            : _policies.Keys.Max() + 1;

        var policy = new Policy(
            nextId,
            request.PolicyHolderName,
            request.PolicyType,
            DateTimeOffset.UtcNow);

        _policies[nextId] = policy;

        return policy;
    }

    public Policy? GetPolicy(int id)
    {
        return _policies.GetValueOrDefault(id);
    }
}

public record CreatePolicyRequest(
    string PolicyHolderName,
    string PolicyType);

public record Policy(
    int Id,
    string PolicyHolderName,
    string PolicyType,
    DateTimeOffset CreatedAt);
```

Now update `Program.cs`:

```C#
using PolicyService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PolicyService.Services.PolicyService>();

var app = builder.Build();

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTimeOffset.UtcNow
    });
});

app.MapGet(
    "/policies/{id:int}",
    (int id, PolicyService.Services.PolicyService service) =>
    {
        var policy = service.GetPolicy(id);

        return policy is null
            ? Results.NotFound()
            : Results.Ok(policy);
    });

app.MapPost(
    "/policies",
    (
        CreatePolicyRequest request,
        PolicyService.Services.PolicyService service) =>
    {
        var policy = service.CreatePolicy(request);

        return Results.Created(
            $"/policies/{policy.Id}",
            policy);
    });

app.Run();
```

The fully-qualified class name looks slightly awkward because our namespace and class both say `PolicyService`. That’s harmless for now. Later we can rename it to something like `PolicyRepository` or `PolicyManager` if that better matches the responsibility.

### Step 2: replace the placeholder test

In the test project, remove the generated test and create:

```
tests\PolicyService.Tests\PolicyServiceTests.cs
```

```C#
using PolicyService.Services;

namespace PolicyService.Tests;

[TestClass]
public class PolicyServiceTests
{
    [TestMethod]
    public void CreatePolicy_AssignsFirstPolicyId()
    {
        var service = new Services.PolicyService();

        var request = new CreatePolicyRequest(
            "Jane Doe",
            "Homeowners");

        var policy = service.CreatePolicy(request);

        Assert.AreEqual(1, policy.Id);
        Assert.AreEqual("Jane Doe", policy.PolicyHolderName);
        Assert.AreEqual("Homeowners", policy.PolicyType);
    }

    [TestMethod]
    public void GetPolicy_ReturnsPreviouslyCreatedPolicy()
    {
        var service = new Services.PolicyService();

        var created = service.CreatePolicy(
            new CreatePolicyRequest(
                "Jane Doe",
                "Auto"));

        var retrieved = service.GetPolicy(created.Id);

        Assert.IsNotNull(retrieved);
        Assert.AreEqual(created.Id, retrieved.Id);
    }

    [TestMethod]
    public void GetPolicy_ReturnsNull_WhenPolicyDoesNotExist()
    {
        var service = new Services.PolicyService();

        var result = service.GetPolicy(999);

        Assert.IsNull(result);
    }
}
```

Then run:

```PowerShell
dotnet test
```

We want these tests to exercise **business behavior**, not ASP.NET itself. At this stage that distinction is useful: a unit test verifies a small code-level behavior, while `/health` is a runtime verification surface. Those will become different kinds of verification in your delivery map.

### Step 3: add a DEV/QA configuration difference

The workbook deliberately asks for one value that differs between environments.

Add this to `appsettings.json`:

```JSON
{
  "EnvironmentSettings": {
    "DisplayName": "Default"
  }
}
```

Create:

```
appsettings.Development.json
```

```JSON
{
  "EnvironmentSettings": {
    "DisplayName": "DEV"
  }
}
```

And create:

```
appsettings.QA.json
```

```JSON
{
  "EnvironmentSettings": {
    "DisplayName": "QA"
  }
}
```

Now make `/health` expose the configured environment:

```C#
app.MapGet("/health", (IConfiguration configuration) =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        environment =
            configuration["EnvironmentSettings:DisplayName"],
        timestamp = DateTimeOffset.UtcNow
    });
});
```

You can run DEV with:

```PowerShell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src\PolicyService
```

and verify `/health` reports `DEV`.

Then:

```PowerShell
$env:ASPNETCORE_ENVIRONMENT = "QA"
dotnet run --project src\PolicyService
```

and verify it reports `QA`.

This gives us our first explicit example of a delivery-system concern:

```
same source
   |
same compiled application
   |
different environment configuration
   |
different runtime behavior
```

That distinction is going to matter a lot later.

### Step 4: version the service

In:

```
src\PolicyService\PolicyService.csproj
```

add:

```XML
<PropertyGroup>
  <Version>1.0.0</Version>
</PropertyGroup>
```

Then expose it in `/health`:

```C#
using System.Reflection;
```

and:

```C#
var version =
    Assembly.GetExecutingAssembly()
        .GetName()
        .Version?
        .ToString();
```

Return that as part of the response:

```C#
app.MapGet("/health", (IConfiguration configuration) =>
{
    var version =
        Assembly.GetExecutingAssembly()
            .GetName()
            .Version?
            .ToString();

    return Results.Ok(new
    {
        status = "Healthy",
        environment =
            configuration["EnvironmentSettings:DisplayName"],
        version,
        timestamp = DateTimeOffset.UtcNow
    });
});
```

Now `/health` answers three different questions:

```
Is the process responding?
What environment am I talking to?
What version is running?
```

That is already a much more useful operational interface.

### Step 5: create a build script

Create:

```
scripts\build.ps1
```

```PowerShell
$ErrorActionPreference = "Stop"

Write-Host "Restoring packages..."
dotnet restore

Write-Host "Building solution..."
dotnet build --no-restore --configuration Release

Write-Host "Running tests..."
dotnet test --no-build --configuration Release

Write-Host "Build completed successfully."
```

Run it from the repository root:

```PowerShell
.\scripts\build.ps1
```

This gives you a repeatable local build entry point, which Week 2 will later turn into a CI pipeline.

### The important exercise before we draw anything

Once the tests and build script are working, don’t move immediately to the diagram.

I want you to answer these four questions from your own understanding:

1. What is the **artifact** produced by `dotnet build`?
2. Which parts of what we’ve built are **controls**?
3. Which parts are **verification**?
4. Where has **state changed** so far, starting from editing source code through running the application?

Those map directly into the Week 1 learning outcomes: understanding the source-to-running-software path, recognizing state changes and human intervention, and distinguishing prevention from detection.

Send me your answers even if you’re uncertain. I’ll challenge/refine them rather than just give you the workbook answer key.

* * *

The workbook specifically asks us to identify where **state changes, human intervention, failure introduction, and failure detection occur** along the path from developer commit through release acceptance. So “state” here is broader than application/domain state.

### 1. Artifact

After `dotnet build`, we have compiled output such as:

```
PolicyService.dll
PolicyService.exe
PolicyService.deps.json
PolicyService.runtimeconfig.json
dependency DLLs
configuration files
...
```

> **Artifact:** The versioned collection of compiled application binaries and supporting files produced by the build process that can be executed or subsequently packaged/deployed.

There's an important nuance we'll explore much more in Week 2: right now our build **output** isn't necessarily a deliberately packaged, immutable deployment artifact yet. But you're thinking about it correctly.

* * *

### 2. Control

Think of a **control** as something that constrains or governs how software moves through the delivery system.

For example:

```
Source
  │
  │  CONTROL: must compile
  ▼
Build
  │
  │  CONTROL: tests must pass
  ▼
Build Output
  │
  │  CONTROL: person chooses environment
  ▼
Run in QA configuration
```

We already have several primitive controls.

`dotnet build` won't produce successful output if compilation fails. Our build script stops when a command fails because of:

```PowerShell
$ErrorActionPreference = "Stop"
```

Our tests can prevent the build script from being considered successful. And `ASPNETCORE_ENVIRONMENT` controls which environment-specific configuration gets selected.

So I would describe **controls** as:

> Mechanisms that constrain, govern, or influence what is allowed to happen in the delivery process.

This becomes especially interesting when we distinguish **preventive** controls from **detective** controls.

For example, imagine someone accidentally breaks `CreatePolicy()`.

A unit test doesn't prevent the developer from writing the defect.

It **detects** the defect.

But if our process says:

```
Tests fail → artifact cannot be promoted
```

we've used that detection mechanism as part of a **preventive gate** against promoting known-bad software.

That's an important distinction.

* * *

### 3. Verification

We identified both examples:

> Test suite and health endpoint.

But notice that they verify **different things**.

```
Unit tests
   │
   └── "Does this code exhibit the behavior we expect?"

              vs.

Health endpoint
   │
   └── "Is this running application responding?"
```

This distinction will become increasingly important as we progress.

A successful unit test does not prove that QA works.

A successful `/health` request does not prove that creating a policy works.

And neither necessarily proves that a user can complete a real business workflow.

You're already encountering the central theme of several later weeks in miniature.

* * *

## 4. State change

Think about the entire delivery system.

You begin here:

```
Developer edits Program.cs
```

Something changed.

Your **working tree state** changed.

Then:

```
git add .
git commit
```

Something else changed.

Your **repository state** changed.

Then:

```
dotnet build
```

Something else changed.

Your filesystem now contains compiled output:

```
src/
  PolicyService/
    bin/
      Release/
        net9.0/
          PolicyService.dll
```

That's another state change.

Then you do:

```PowerShell
$env:ASPNETCORE_ENVIRONMENT = "QA"
```

Your execution environment now has a different configuration state.

Then:

```PowerShell
dotnet run
```

You go from:

```
Application not running
```

to:

```
Application process running
```

Another state change.

Then:

```http
POST /policies
```

Your application runtime goes from:

```
policies = {}
```

to:

```
policies = {
    1: Jane Doe
}
```

That's the application-state change you identified.

Finally, stop the process:

```
policies = {
    1: Jane Doe
}
```

becomes:

```
<does not exist>
```

because our storage isn't persistent.

### This is the mental shift Week 1 is after

Instead of seeing:

```
Code → Build → Deploy
```

start seeing:

```
              STATE TRANSITION
Source edit ─────────────────────► Modified working tree

              STATE TRANSITION
Commit ──────────────────────────► New repository state

              STATE TRANSITION
Build ───────────────────────────► Compiled output exists

              STATE TRANSITION
Configure QA ────────────────────► Runtime configuration selected

              STATE TRANSITION
Start process ───────────────────► Service becomes active

              STATE TRANSITION
POST /policies ──────────────────► Runtime data changes
```

And every transition invites technical-leadership questions:

**Who or what causes this transition?**

**What controls it?**

**What can go wrong during it?**

**How would we know that it went wrong?**

That's why the workbook isn't asking you merely to draw a pipeline. Its Week 1 objective is explicitly to move from thinking about individual pipelines to reasoning about delivery as an **end-to-end system**.

And this should feel somewhat familiar from your deployment-reliability work: “the deployment tool says it finished” describes only **one transition**. It doesn't establish that every resulting state is correct.

### One more exercise before we create the diagram

Let's apply that model yourself rather than having me fill it in.

Suppose our lifecycle is:

```
Developer
    │
    ▼
Edit Source
    │
    ▼
Git Commit
    │
    ▼
Build
    │
    ▼
Unit Tests
    │
    ▼
Build Output
    │
    ▼
Configure QA
    │
    ▼
Start Application
    │
    ▼
Health Check
    │
    ▼
Release Acceptance
```

Pick **three transitions** from that lifecycle and tell me:

**A.** What state changed?

**B.** Who/what caused the change — human or automation?

**C.** What failure could be introduced?

**D.** Where would that failure currently be detected?

Don't worry about getting the terminology perfect. The reasoning is the exercise. Once you've done three, we'll use your answers as the raw material for the actual Week 1 **Control / Artifact / Verification delivery-system map** rather than making up a diagram independently.

* * *

The important shift is that **state isn't synonymous with business/application data**. We're tracking meaningful states throughout the whole delivery system.

### 1. Source-code change

Editing source code generally succeeds perfectly well even when you've written bad code:

```
Developer edits source
        ↓
Source state changes successfully
        ↓
Defect may now exist
        ↓
Compiler / tests may detect defect later
```

For example:

```C#
public Policy? GetPolicy(int id)
{
    return null; // perfectly valid C#
}
```

The state transition succeeded. The compiler succeeds too.

But we've introduced a behavioral defect that a unit test might detect.

So our map could record:

| Transition | State change | Actor | Possible failure introduced | Detection |
| --- | --- | --- | --- | --- |
| Edit source | Working tree changes | Developer | Compile error, behavioral defect | Compiler, unit tests |

That distinction matters because the workbook specifically wants us to identify both **where failures can be introduced** and **where failures can be detected**.

They don't have to be the same place.

* * *

### 2. Git repository change

We should distinguish a **commit**, **push**, and **pull request**.

```
Working tree
    │
  commit
    ▼
Local Git repository
    │
   push
    ▼
Remote Git repository
    │
pull request / merge
    ▼
main
```

Those are actually **separate state transitions**.

For the moment, let's simplify our map to:

| Transition | State change | Actor | Possible failure | Detection |
| --- | --- | --- | --- | --- |
| Commit | Local repository gets new commit | Developer | Wrong files committed, incomplete change | Human review / later verification |
| Push | Remote repository receives commits | Developer + Git | Authentication, permissions, network | Git client |
| Merge | `main` receives change | Human / GitHub | Conflict, inappropriate change | GitHub conflict detection, review, later tests |

Notice something interesting: **Git can't necessarily tell us that a successful merge is a bad change.**

That's another recurring theme:

```
Git operation succeeded
        ≠
Software is correct
```

Just like later:

```
Build succeeded
        ≠
Software is correct
```

or:

```
Deployment succeeded
        ≠
Environment is healthy
```

We're beginning to distinguish **execution success** from **outcome correctness**.

* * *

### 3. Start application

> Application run state changes.

More specifically:

```
PolicyService process absent
             ↓
        dotnet run
             ↓
PolicyService process executing
             ↓
HTTP listener available
```

Failure examples might be startup exceptions, hosting problems, resource exhaustion, configuration problems, etc.

> "failure would be detected by the system when the application is unable to start properly"

**But which system?**

Suppose we execute:

```PowerShell
dotnet run
```

and get:

```
Now listening on: http://localhost:5000
```

The process started successfully.

But then:

```http
GET /health
```

returns `500`.

Was application startup successful?

Probably yes.

Is the application healthy?

No.

That's precisely why our `/health` endpoint exists as a **separate verification step**.

We can therefore model:

```
Start Application
       │
       │ execution
       ▼
Process running
       │
       │ verification
       ▼
GET /health
       │
       ├── 200 → expected state observed
       │
       └── failure → expected state not observed
```

That distinction is going to become foundational later in the workbook.

* * *

## Now look at the delivery system we've discovered

We haven't even introduced a pipeline yet, but you can already identify:

```
Developer
    │
    │ EDIT
    ▼
Source State
    │
    │ COMMIT / PUSH
    ▼
Repository State
    │
    │ BUILD
    ▼
Artifact State
    │
    │ CONFIGURE
    ▼
Environment State
    │
    │ START
    ▼
Runtime State
    │
    │ REQUESTS
    ▼
Application/Data State
```

And running alongside those transitions are verification mechanisms:

```
                     ┌── Compiler
Source ──────────────┤
                     └── Unit Tests

Repository ───────────── Human/Git controls

Artifact ─────────────── Build result

Configuration ────────── Currently mostly manual

Runtime ──────────────── /health

Business behavior ────── API request + eventually synthetic tests
```

That's considerably richer than:

```
Commit → Build → Deploy
```

which is exactly the Week 1 learning objective of mapping source change to running software and identifying state changes, human intervention, and failure detection.

### One final distinction before we build your portfolio diagram

The workbook also expects you to distinguish **prevention controls from detection controls**.

So consider this scenario:

A developer changes:

```C#
public Policy? GetPolicy(int id)
```

so that it always returns `null`.

We have four possible mechanisms:

**A.** A unit test fails.

**B.** GitHub refuses to merge the PR because required tests aren't passing.

**C.** The application is deployed and `/health` returns `200`.

**D.** Someone calls `GET /policies/1` and discovers it returns `404`.

For each one, classify it as **prevention**, **detection**, **both**, or **neither**, and briefly tell me why.

There's a slight trap in there. Once you've answered it, we'll have enough conceptual material to create the actual Week 1 **Control / Artifact / Verification** system map required by the workbook.

* * *

The trap was **#3**: `/health` returning `200` tells us something useful, but it detects nothing about the particular defect we're investigating.

| Scenario | Classification | Why |
| --- | --- | --- |
| Unit test fails | **Detection** | The defect already exists in source; the test reveals it. |
| GitHub blocks merge because tests fail | **Prevention** | It prevents the known-bad change from progressing into `main`. |
| `/health` returns 200 | **Neither** | It verifies process health, but doesn't detect or prevent this behavioral defect. |
| `GET /policies/1` returns unexpected 404 | **Detection** | The behavioral defect has propagated far enough to be visible at runtime. |

There's an especially useful relationship between #1 and #2:

```
Defect introduced
      │
      ▼
Unit test fails
      │
      │ DETECTION
      ▼
Failure known
      │
      ▼
Required test gate
      │
      │ PREVENTION
      ▼
Merge blocked
```

The test itself doesn't prevent anything. **A control uses the test result to prevent something.**

That gives us a cleaner vocabulary for the delivery map:

> **Verification produces evidence. A control uses evidence and rules to govern what happens next.**

That isn't stated verbatim in the workbook, but I think it's a useful model for the distinction it asks you to make between prevention and detection. The workbook specifically expects you to be able to distinguish those two concepts by the end of Week 1.

## Let's build your Week 1 artifact

We're now ready for the required **one-page Software Delivery System Map with Control, Artifact, and Verification sections**.

I don't want to write it for you, though. Let's construct it from the system you've actually built.

Create:

```
docs/week-01/delivery-system-map.md
```

Start with:

```Markdown
# Software Delivery System Map

## System Overview

The Policy Service is a small .NET Web API used to examine software
delivery as an end-to-end system. This map describes how a source-code
change moves from a developer workstation to a running application and
identifies the controls, artifacts, and verification mechanisms involved.

## Delivery Flow

Developer
    ↓
Source Change
    ↓
Git Commit
    ↓
Git Repository
    ↓
Build
    ↓
Unit Tests
    ↓
Build Output
    ↓
Environment Configuration
    ↓
Start Application
    ↓
Health Verification
    ↓
Release Acceptance

## Control

TODO

## Artifact

TODO

## Verification

TODO
```

Now let's have **you** fill the three TODO sections.

Don't worry about polished prose yet. For each section, identify what currently exists in our Week 1 system.

For **Control**, think:

> What governs or constrains progression through the system?

For **Artifact**:

> What concrete things are created or transformed as software travels through the system?

And for **Verification**:

> What evidence do we collect that tells us whether some expected property is actually true?

Give me your three lists. Then I'll review them as if this were an architecture review—looking especially for things you've categorized incorrectly or important boundaries you've missed—and we'll turn the result into the first portfolio-quality artifact.

* * *

Your end result should look something like this: 

```Markdown
## Control

- **Build control:** The build process stops if the application fails to compile, preventing an unsuccessful build from progressing.

- **Test control:** The build process stops if the unit test suite fails, preventing a build with known test failures from progressing.
```

There's an interesting limitation worth preserving for the map: **we don't have a deployment or release control yet.** That's intentional because Week 1 says not to automate deployment.

Once the script finishes, a human decides what happens next.

That absence is part of the system design, too.

```Markdown
## Artifact

- **Source commit:** A durable version of source code recorded in Git that represents the input to the build.

- **Build output:** Compiled application binaries, runtime metadata, configuration files, and dependencies produced from the source commit.
```

Later we'll make the relationship between those two much stronger:

```
Commit abc123
      │
      │ produces
      ▼
Artifact 1.4.7
```

Right now, that traceability isn't particularly strong—which is fine. Discovering that weakness is useful.

We have **build output**, but haven't deliberately packaged it into a distributable artifact. Week 2 explicitly introduces `Package -> Publish Artifact`, so keeping that distinction now will give you something meaningful to improve next week.

```Markdown
## Verification

- **Unit tests:** Verify specific expected behaviors of the PolicyService, including policy creation, retrieval, and handling of a nonexistent policy.

- **Health endpoint:** Verifies that the running application is responsive and capable of serving the health request.
```

Notice how deliberately narrow those claims are.

That's an important technical-leadership habit:

```
Evidence                         Claim we can make

3 unit tests pass          →     Tested behaviors work
/health returns 200        →     Application responds
Build succeeds             →     Source compiled successfully
```

rather than:

```
Tests pass                 →     Application works
/health returns 200        →     Environment is healthy
Build succeeds             →     Release is good
```

**Evidence should only support claims as strong as what was actually measured.**

That idea is going to become particularly important in the later observability and reliability weeks.


* * *

## Now identify the human interventions

Before we finish the diagram, there's one more Week 1 requirement we haven't explicitly represented. The workbook asks us to identify **human intervention** as well as state changes and failure points.

Look at your current lifecycle:

```
Developer
    ↓
Source Change
    ↓
Git Commit
    ↓
Git Repository
    ↓
Build
    ↓
Unit Tests
    ↓
Build Output
    ↓
Environment Configuration
    ↓
Start Application
    ↓
Health Verification
    ↓
Release Acceptance
```

For each step, classify its trigger as:

**Human** — someone explicitly initiates it.

**Automated** — another step automatically causes it.

**Mixed** — human initiation causes an automated sequence.

For example, don't overthink `Source Change`: that's obviously **Human**.

Give me your classifications for the rest. This is where we'll discover which parts of our Week 1 delivery system depend on someone remembering to do something—the exact question one of the workbook's reflection prompts asks.

* * *

Here is an example of how we might diagram the transitions: 

```
[Source Code]
     │
     │ Human: edit
     ▼
[Modified Source]
     │
     │ Human: commit
     ▼
[Git Commit]
     │
     │ Human: push
     ▼
[Remote Repository]
     │
     │ Human: run build.ps1
     ▼
[Build]
     │
     │ Automated: compile
     ▼
[Compiled Output]
     │
     │ Automated: unit tests
     ▼
[Verified Build Output]
     │
     │ Human: select environment configuration
     ▼
[Configured Application]
     │
     │ Human: start application
     ▼
[Running Application]
     │
     │ Human: call /health
     ▼
[Health Result]
     │
     │ Human decision
     ▼
[Release Acceptance]
```

Now we can see something important about our system immediately.

## Where are the humans?

They're everywhere.

The developer must remember to:

```
edit
  ↓
commit
  ↓
push
  ↓
start build
  ↓
select configuration
  ↓
start application
  ↓
perform health check
  ↓
accept/reject
```

Automation currently handles only a relatively small section:

```
Human
  │
  │ run build.ps1
  ▼
┌─────────────────────────────┐
│       AUTOMATED REGION      │
│                             │
│ restore                     │
│    ↓                        │
│ compile                     │
│    ↓                        │
│ unit tests                  │
│    ↓                        │
│ successful build output     │
└─────────────────────────────┘
  │
  ▼
Human
```

That is a meaningful finding from the exercise—not necessarily a criticism of the system.

Remember one of the Week 1 reflection questions:

> **Where does your design rely on a person remembering to do something?**

For our current system, the answer is essentially:

> Outside of the local build script, most transitions in the delivery lifecycle depend on explicit human action. A developer must push the source, initiate the build, select the target environment configuration, start the application, execute health verification, and make the final acceptance decision.

That's a much stronger observation than simply saying "our process is manual."

It identifies **where** it's manual.

* * *

## There's also an interesting control boundary

Your current system has:

```
                CONTROL BOUNDARY
                       │
                       ▼
               ┌───────────────┐
Human ────────►│  build.ps1    │
               │               │
               │ restore       │
               │    ↓          │
               │ build ──X     │
               │    ↓          │
               │ tests ──X     │
               │    ↓          │
               │ output        │
               └───────────────┘
                       │
                 control ends
                       ▼
                     Human
```

Inside that boundary, progression is deterministic:

**Compilation fails → stop.**

**Tests fail → stop.**

**Both succeed → complete.**

Outside it, we're relying primarily on human procedure.

That gives you an excellent answer to another Week 1 reflection question:

> **Which parts of the delivery process are deterministic today?**

Something like:

> The local build process is the most deterministic portion of the current delivery system. Given the same source and compatible tooling, the build script follows a defined sequence of restore, compile, and test operations and stops when an operation fails. The process after build completion is less deterministic because configuration, application startup, health verification, and release acceptance depend on manual actions.

There's a caveat hiding in "same source and compatible tooling" that Week 2 is going to attack pretty aggressively.

## Your Week 1 map is nearly complete

I'd now add a fourth section to `delivery-system-map.md`, even though the required three sections remain Control, Artifact, and Verification:

```Markdown
## Human Intervention

- **Source management:** A developer manually edits, commits, and pushes source changes.
- **Build initiation:** A developer manually starts the local build process.
- **Environment selection:** A developer manually selects the environment-specific configuration.
- **Application startup:** A developer manually starts the application.
- **Health verification:** A developer manually invokes the health endpoint and interprets the result.
- **Release acceptance:** A human makes the final decision about whether the running application is acceptable.

The build script is currently the primary automated region of the delivery system. Once initiated, restore, compilation, and unit testing execute in a defined sequence without additional human intervention.
```

That's portfolio-worthy because you're not just documenting components; you're communicating an architectural property of the delivery system.

### One final analysis before we polish the artifact

The workbook's third reflection question is probably the most interesting one:

> **Which failure could travel the farthest before being detected?**

Don't think only about compilation failures.

Imagine several possible defects:

```
Compile error
Incorrect policy behavior
Wrong environment configuration
Application won't start
Application starts but policy creation is broken
Wrong version deployed
```

Which one(s) could travel **furthest to the right** through our current delivery system before anything we've built would necessarily detect it?

Give me your reasoning rather than just the name of the failure.

* * *

A **wrong version deployed** could travel the furthest because nothing in our current Week 1 system actually compares the running application against an expected source/build identity. The health endpoint only proves the process is responsive; it does not prove that the correct version is running.

A **wrong but valid environment configuration** can be especially dangerous because every technical step may still succeed.

```
Build succeeds
    ↓
Tests succeed
    ↓
Application starts
    ↓
/health returns 200
    ↓
Wrong configuration is still in effect
```

That makes it a classic example of a failure that is not necessarily visible through generic technical checks.

I’d capture the reflection like this in your Week 1 document:

> **Which failure could travel the farthest before being detected?**  
> A wrong application version or incorrect but valid environment configuration could pass through the current delivery process without being detected. The build and unit tests verify properties of the source being built, while the health endpoint only verifies that the running application responds. The current system does not verify that the deployed runtime matches the expected source version or configuration.

That directly supports the workbook’s goal of identifying where failures can be introduced and where they can actually be detected.

It also gives you a very useful Week 1 conclusion:

> **A successful delivery process needs verification of identity and configuration, not just successful execution.**

That idea is going to reappear later when the workbook gets into deployment provenance and environment readiness.

At this point, I think your `delivery-system-map.md` is conceptually complete. The next best move is to polish the three required sections—**Control, Artifact, Verification**—and add the human-intervention and reflection notes underneath, so Week 1 has a clean portfolio artifact rather than just working notes.

* * *
That checks off the primary Week 1 portfolio deliverable. The workbook still has three things before we should call Week 1 complete: the **knowledge check**, **reflection prompts**, and **weekly retrospective**.

We've effectively worked through the reflection prompts already, so let's do the knowledge check properly. The workbook says to answer it **without consulting the answer key**, so I'll act as the reviewer rather than giving you the answers.

### Week 1 Knowledge Check

Answer these from memory, preferably in 1–3 sentences each:

**1. What is the difference between continuous integration and continuous delivery?**

**2. Why is a deployment pipeline more than a build script?**

**3. Why can human intervention increase deployment risk?**

**4. What is the difference between preventing a failure and detecting one?**

**5. What information would you need to identify exactly what code is running in QA?**

**6. Why can deployment validation be treated separately from deployment execution?**


* * *

### 1. Continuous integration vs. continuous delivery — Needs Refinement

The important idea: changes are continually integrated and verified together.

> Continuous integration regularly integrates and verifies source changes to establish that the software can still build and pass its checks. Continuous delivery extends that process by making the resulting software ready to move through packaging, deployment, and release in a repeatable manner.

The key progression is:

```
CI
Source → Build → Verify

CD
Source → Build → Verify → Package → Deliver/Deploy → Release readiness
```

### 2. Deployment pipeline vs. build script — Strong

> "A deployment pipeline has a broader scope that includes the changes that happen to an environment and infrastructure"

That's exactly the boundary the workbook wants you to see. Its answer also mentions artifact movement, environment configuration, controls, verification, and approvals.

This demonstrates the concept without needing to enumerate all of those.

### 3. Human intervention

> Human intervention increases deployment risk because manual processes are less deterministic. People can forget steps, make input mistakes, or make assumptions about context, and the same procedure may be performed differently between executions.

### 4. Prevention vs. detection — Needs Refinement

> Prevention attempts to stop an invalid state from occurring; detection determines that an invalid state exists.

Our earlier example then fits underneath that definition:

```
Unit test fails
       ↓
DETECTION
"We found a problem."

Required test gate blocks merge
       ↓
PREVENTION
"We won't allow that known problem
to progress farther."
```

### 5. Identifying what's running in QA 

Suppose this happens:

```
Branch: feature/policy
Monday ─── commit A
Tuesday ── commit B
Wednesday ─ commit C
```

Saying:

```
Branch: feature/policy
```

doesn't establish whether QA contains A, B, or C.

You need the immutable source identity:

```
Commit SHA: 91d27fa...
```

The workbook expects at minimum **artifact/build ID, source commit, branch/source reference, deployment timestamp, and target environment**.

So I'd answer:

> I would need the artifact/build ID, exact source commit, source branch/reference, deployment timestamp, and target environment.

This is also an answer I'd particularly want you to remember rather than memorize, because it connects directly to the failure you identified earlier: **wrong version deployed**.

### 6. Independent deployment validation 

> "The two processes can run independent of one another and serve different purposes. An environment may start healthy and later become unhealthy.

That gives independent validation an additional capability.

```
              DEPLOYMENT
                  │
                  ▼
             Environment
                  │
          ┌───────┴────────┐
          ▼                ▼
Immediately after       Tomorrow
deployment
          │                │
          ▼                ▼
     Validation        Validation
```

If validation only exists _inside_ deployment execution, it is naturally tied to that moment.

If validation is independently executable, the same capability can potentially answer:

> **"Is this environment healthy now?"**

That idea goes beyond the minimum Week 1 answer, but it's completely consistent with where the workbook eventually takes the project.

* * *

### Last thing: your retrospective

Before committing Week 1, answer the workbook's four retrospective questions in your own words:

1. **What became clearer this week?**
2. **What was harder than expected?**
3. **What would I do differently in a production system?**
4. **What artifact from this week best demonstrates growth?**

This one shouldn't be a test. Write what you actually experienced—even if one answer is "nothing was particularly difficult." We'll tighten it only if necessary, put it into `docs/week-01/retrospective.md`, and then make the **Week 1 completion commit**.


* * *
## Week 1 is complete

Against the workbook checklist, you've now done the substantive work:

* The lab builds and runs.
* You can explain the delivery-system concepts behind it.
* You've created the required Software Delivery System Map.
* You've identified concrete tradeoffs and weaknesses.
* You completed the knowledge check from memory.
* You've completed the retrospective.

Those are exactly the completion criteria the workbook defines for Week 1.

I'd finish with a clean Git commit:

```PowerShell
git status
git add .
git commit -m "Complete week 1 delivery systems study"
git push
```

And I'd tag it:

```PowerShell
git tag week-01
git push origin week-01
```

The tag is worth doing for this project. Ten weeks from now, we'll be able to inspect exactly what the system looked like at the end of Week 1 and compare it against the final architecture.

**Week 1: Think in Delivery Systems — Complete.**

The next step is **Week 2: Continuous Integration and Reproducible Builds**. That's where we take the relatively small automated island you discovered—

```
build.ps1
   ↓
Restore
   ↓
Compile
   ↓
Unit Tests
```

—and turn it into an actual CI process producing a traceable artifact. The workbook will also have us deliberately break compilation, tests, and package restoration so you learn to reason about **where different classes of delivery failure surface**, rather than merely getting a green pipeline.

That's a natural next step from what you just learned.