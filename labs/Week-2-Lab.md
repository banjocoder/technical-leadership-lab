## Week 2 Lab

Week 2 is a good transition from the system-level thinking you did in Week 1 into something concrete: **can we turn a specific source revision into a tested artifact in a way that is repeatable and explainable?**

The workbook defines Week 2 as **Continuous Integration and Reproducible Builds**, with four learning outcomes: build a pipeline-as-code workflow, explain artifact immutability/provenance, understand self-hosted agent dependencies, and diagnose failures at the correct stage. The required portfolio artifact is **ADR-001: Build Artifacts Are Immutable**.

Lets's work through it in roughly this order rather than jumping immediately into YAML:

1. **Establish our build contract** — what goes in, what comes out, and what must be true before an artifact exists.
2. **Make the build reproducible locally** — Restore → Compile → Unit Tests → Package.
3. **Move that exact process into CI** — then Publish Artifact only after the earlier stages succeed.
4. **Add artifact identity/provenance** — tie the artifact to the source commit.
5. **Perform the three failure experiments** — broken restore, broken compilation, broken test.
6. **Inspect the agent as part of the system** — especially useful given your experience with self-hosted ADO agents.
7. **Write ADR-001.**
8. **Do the reflection prompts and knowledge check from memory.**

That matches the lab on page 7 while preserving the workbook's intent that we learn from the failures rather than just create a working pipeline.

### Step 1: Define the build contract

Before touching the pipeline, I want you to reason about your Week 1 repository.

Think of the build as a function:

```
Source revision
+ declared dependencies
+ declared build instructions
-------------------------------
= tested artifact
```

For Week 2, an artifact should **not** simply mean "`dotnet build` succeeded." The pipeline we're aiming for is:

```
Source
  ↓
Restore
  ↓
Compile
  ↓
Unit Tests
  ↓
Package
  ↓
Publish Artifact
```

And there is an important rule:

```
If any preceding stage fails → no artifact is published.
```

That is directly related to the workbook's requirement that failed tests prevent artifact publication.

Before we implement anything, answer these four questions about the repository you already have. Don't research them; just tell me what you believe the answers are right now.

**1. What should the build artifact actually be?**  
For example: the compiled API binaries, a `.zip` containing the published application, a NuGet package, Docker image, etc.

**2. What inputs other than your source code are required to produce it?**  
Think SDK/runtime, NuGet packages, build scripts, test framework, package feeds, and anything installed on your machine.

**3. Which of those inputs are explicitly declared in the repository, and which currently exist only because your development machine happens to have them installed?**

**4. Suppose you build commit `ABC123` today and again six months from now. What things could cause the two resulting artifacts to be different?**

Those questions are the beginning of the **reproducibility** part of Week 2. Your answers will also give us the raw material for the Week 2 reflection question, _"What would make an artifact impossible to reproduce later?"_ on page 8.

Record your answers, then check them against the general responses below.

* * *


The Week 2 lab specifically wants a tested, traceable artifact, with environment-specific configuration kept out of it, and later asks you to reason about implicit self-hosted-agent dependencies.

### 1. Artifact definition

For this exercise, I'd define the artifact as:

> A ZIP containing the published application, its resolved runtime/application dependencies, non-environment-specific configuration, and identifying metadata.

You normally wouldn't need to put the original NuGet packages themselves into the ZIP. NuGet packages are **build inputs**; the DLLs/assets selected from them become part of the published output when required.

README and license files can certainly go into the bundle, although they're more packaging concerns than application-runtime requirements.

There's also an important distinction in your phrase **configuration files**. The workbook explicitly says:

> Keep environment-specific configuration out of the artifact.

So this would be okay:

```
appsettings.json
{
  "FeatureXEnabled": true,
  "RequestTimeoutSeconds": 30
}
```

But we don't want the artifact itself to contain something like:

```
ConnectionString = "QA-SQL-01..."
EnvironmentName = "QA"
```

The goal eventually becomes:

```
same artifact.zip
      │
      ├── + DEV configuration → DEV
      │
      └── + QA configuration  → QA
```

That becomes especially important in Week 3.

* * *

### 2. Build inputs

A strong model is:

```
BUILD INPUTS

Git commit
.NET SDK/toolchain
NuGet package versions
NuGet package sources
test framework + test adapter
build scripts
build configuration
target runtime/platform
non-environment-specific configuration
```

Then separately:

```
BUILD CONTROL

Manual trigger
Push trigger
Pull request trigger
Scheduled trigger
```

That's a subtle distinction, but it carries forward from your Week 1 system map: **control and artifact are different concerns**.

* * *

### 3. The runtime dependency

There are generally two ways a modern .NET application can be published.

A **framework-dependent deployment** contains your application and its application dependencies, but assumes the appropriate .NET runtime exists on the target machine:

```
Application
Dependencies
        ↓
Target machine's .NET runtime
```

A **self-contained deployment** publishes the applicable .NET runtime with the application:

```
Application
Dependencies
.NET runtime
        ↓
Target OS
```

So if you want the second model, that's a completely reasonable design choice for this lab.

There is still a platform assumption, though. A self-contained build normally targets a specific runtime/platform, such as:

```
win-x64
linux-x64
linux-arm64
```

So **self-contained does not mean independent of every execution environment**. It means you're removing the requirement that the machine separately install the .NET runtime.

That's actually a nice tradeoff for your later ADR:

**Benefit:** fewer implicit runtime requirements on deployment machines.

**Cost:** larger artifacts and platform-specific publishing.

* * *

### 4. Reproducibility

Imagine this:

```
Commit ABC123
     │
     ├── Build today
     │     .NET SDK 10.0.100
     │     Dependency X 3.2
     │
     └── Build six months later
           .NET SDK 10.0.300
           Dependency X 3.4
```

Same source does **not necessarily mean same artifact**.

Other things that can matter include:

* SDK/compiler version
* transitive dependency versions
* package-feed contents
* build scripts
* environment variables consumed during build
* target runtime identifier
* generated timestamps/version information
* tooling installed only on the build machine

For an ordinary managed .NET build, CPU model shouldn't normally affect the output. Architecture can matter, though, particularly when targeting something like `win-x64` versus `win-arm64`, or when native/AOT components get involved.

So I'd refine an answer to:

> Rebuilding the same commit may produce a different artifact if the SDK, compiler, dependencies, package sources, build configuration, scripts, or target runtime have changed. Any undeclared dependency on the build machine can also prevent reproducibility.

* * *

## Our Week 2 build contract, version 0.1

I think we can now write down a first contract:

```
SOURCE
  Git commit
       │
       ▼
RESTORE
  Resolve declared dependencies
       │
       ▼
COMPILE
  Build source
       │
       ▼
UNIT TEST
  Verify expected behavior
       │
       ▼
PUBLISH
  Produce self-contained application
       │
       ▼
PACKAGE
  ZIP published output + provenance
       │
       ▼
PUBLISH ARTIFACT
  Only if all previous stages succeeded
```

And the artifact itself has an important semantic meaning:

> **This artifact represents the output of a specific source revision that successfully passed the defined build and test process.**

That sentence will become important when we get to artifact immutability.

## Next: let's find the hidden dependencies

Before we create the pipeline, I want you to inspect your Week 1 project rather than guess.

From the repository root, run:

```PowerShell
dotnet --info
```

Then open the API project's `.csproj` and check the values you see for things like:

```XML
<TargetFramework>...</TargetFramework>
```

and whether any of these exist:

```XML
<RuntimeIdentifier>...</RuntimeIdentifier>
<RuntimeIdentifiers>...</RuntimeIdentifiers>
<SelfContained>...</SelfContained>
```

Also run:

```PowerShell
dotnet list package --include-transitive
```

Don't change anything yet.

We'll take these lists and classify each dependency as **explicit** or **implicit**, which directly answers the Week 2 reflection question about what on the build machine is declared versus merely assumed.

* * *

The Week 2 goal is to understand which dependencies are declared versus merely assumed by the machine running the build. That directly supports the workbook's reflection prompt, “What on your self-hosted agent is implicit rather than declared?”

### What is explicit today

Your repository probably explicitly declares:

```XML
<TargetFramework>net9.0</TargetFramework>
```

So the project says:

> “I require a .NET 9-compatible SDK/toolchain.”

Your NuGet package versions are also explicit:

```
PolicyService
Microsoft.AspNetCore.OpenApi  9.0.17

PolicyService.Tests
Microsoft.NET.Test.Sdk        17.12.0
MSTest                        3.6.4
```

The test tooling is therefore largely repository-declared rather than relying on Visual Studio to happen to have the right test adapter installed.

### What is implicit

The biggest hidden dependency we just found is this:

```
Installed SDK: 9.0.315
```

Your project says `net9.0`, but it does **not** currently say:

> “Build this using SDK 9.0.315.”

Those are different things.

Right now this could happen:

```
Your machine
Source commit ABC123
.NET SDK 9.0.315
        ↓
     Artifact A


Build agent
Source commit ABC123
.NET SDK 9.0.4xx
        ↓
     Artifact B
```

Both SDKs may successfully build `net9.0`.

That doesn't necessarily mean the artifacts will differ, but you haven't established that they won't.

This is precisely the kind of implicit agent state Week 2 is trying to make visible.

* * *

There's another implicit dependency:

```
OS: Windows
Architecture: x64
RID: win-x64
```

At the moment, your project does **not** declare:

```XML
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
```

So `win-x64` is currently a property of your development machine, not your application's declared artifact target.

That's okay for normal framework-dependent compilation. It becomes important because **you want a self-contained artifact**.

For that, we're eventually going to deliberately say:

```
Publish for win-x64
Include the .NET runtime
```

rather than allowing the build machine to implicitly make that choice.

### And there's a third dependency we haven't inspected

NuGet has to get these packages from somewhere.

You know:

```
Microsoft.AspNetCore.OpenApi → 9.0.17
MSTest → 3.6.4
```

But we haven't answered:

```
Where does 9.0.17 come from?
```

Probably NuGet.org, but that is still part of the build system.

So our model currently looks like this:

| Build dependency | Declared? | Current status |
| --- | --- | --- |
| Source revision | Yes | Git |
| Target framework | Yes | `net9.0` |
| Application package version | Yes | `9.0.17` |
| MSTest version | Yes | `3.6.4` |
| Test SDK | Yes | `17.12.0` |
| .NET SDK version | **No** | Machine happens to have `9.0.315` |
| Target runtime/architecture | **No** | Machine happens to be `win-x64` |
| Self-contained publishing | **No** | Not configured yet |
| NuGet sources | Unknown | Need to inspect |
| Dependency resolution lock | Unknown | Need to inspect |

And that last row is worth understanding.

You have explicit **top-level versions**, but you also have transitive dependencies:

```
MSTest
  ├─ MSTest.TestFramework
  ├─ MSTest.TestAdapter
  ├─ Microsoft.Testing.Platform
  ├─ Newtonsoft.Json
  └─ ...
```

You didn't choose every one of those directly.

Your restore process resolved them.

That gives us another reproducibility question:

> If we restore this project six months from now, can we prove NuGet will resolve exactly the same complete dependency graph?

We'll deal with that shortly.

## One important correction to our earlier artifact model

Now that we've seen the project, I would tighten the artifact definition.

Instead of:

> application DLLs + dependency DLLs + NuGet packages + runtime

I'd describe it as:

> **A ZIP containing the self-contained `win-x64` published output of PolicyService, plus artifact provenance metadata.**

NuGet packages themselves don't normally need to be distributed with the application. Restore consumes packages and `dotnet publish` produces the runtime files the application actually needs.

So conceptually:

```
NuGet packages
.NET SDK
source
configuration
      │
      │ BUILD INPUTS
      ▼
dotnet publish
      │
      ▼
Application DLL
Dependency DLLs
.NET runtime
host executable
runtime metadata
non-environment-specific configuration
      │
      ▼
artifact.zip
```

That's a much cleaner boundary.

## This also gives us our first reproducibility control

One common .NET mechanism for declaring the SDK version is a repository-level `global.json`.

Conceptually:

```JSON
{
  "sdk": {
    "version": "9.0.315"
  }
}
```

Then the machine doesn't merely ask:

> “Do you have some .NET 9 SDK?”

It asks:

> “Do you have the SDK version this repository expects?”

That converts part of the agent's implicit state into declared build configuration.

We aren't going to add it blindly yet, though, because I want you to see the dependency chain before we fix it.

### Next inspection

From the repository root, run:

```PowerShell
Get-ChildItem -Path . -Filter global.json -Recurse
```

Then:

```PowerShell
Get-ChildItem -Path . -Filter NuGet.Config -Recurse
```

and:

```PowerShell
dotnet nuget list source
```

Finally, see whether you already have a NuGet lock file:

```PowerShell
Get-ChildItem -Path . -Filter packages.lock.json -Recurse
```

Check these results.

At that point we'll have mapped essentially the entire **Restore** portion of:

```
Restore → Compile → Unit Tests → Package → Publish Artifact
```

Then we'll make a few intentional changes that turn those hidden machine assumptions into repository-controlled build inputs.

* * *


This inspection should find exactly the kind of hidden build-machine state Week 2 wants you to notice.

The workbook's goal here is not merely “make CI work”; it specifically wants you to understand **reproducibility, provenance, and self-hosted agent dependencies**.

Right now, I would classify your build as **partially reproducible**.

| Dependency | State |
| --- | --- |
| Source | Explicit through Git |
| Target framework | Explicit: `net9.0` |
| Direct NuGet versions | Explicit in project files |
| Test tooling | Explicit through NuGet |
| Exact .NET SDK | **Implicit**: your machine has `9.0.315` |
| NuGet sources | **Implicit**: coming from machine/user configuration |
| Transitive dependency graph | **Not locked** |
| Target runtime | **Not yet declared** |
| Self-contained publishing | **Not yet declared** |

Three things are especially interesting.

### 1. The SDK is currently machine state

You have:

```
.NET SDK 9.0.315
```

but nothing in the repository says that is the SDK that should be used.

Another developer could clone the repository and have:

```
9.0.203
```

or:

```
9.0.4xx
```

and your project may still build because all it declares is:

```XML
<TargetFramework>net9.0</TargetFramework>
```

So this is our first undeclared dependency.

* * *

### 2. Your NuGet sources are even more interesting

There's no repository `NuGet.Config`.

Yet this command:

```PowerShell
dotnet nuget list source
```

found multiple sources.

That means those sources are coming from **outside the repository**, probably machine- or user-level NuGet configuration.

And two are enabled:

```
nuget.org
Microsoft Nuget Packages
```

where the second one is:

```
C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\
```

So right now your restore is conceptually doing this:

```
Repository
    │
    ├── asks for package X
    │
    ▼
NuGet configuration on Ben's machine
    │
    ├── nuget.org
    └── local Microsoft package directory
```

Another engineer's machine may have a completely different set of package sources.

That's a classic self-hosted-agent problem:

> **The pipeline definition may be identical while the machine executing it supplies undocumented behavior.**

This connects directly to the Week 2 reflection question:

> “What on your self-hosted agent is implicit rather than declared?”

Your current answer would include **SDK version and NuGet source configuration**.

* * *

### 3. Your dependency graph isn't locked

You have explicit top-level versions such as:

```
MSTest 3.6.4
```

but MSTest brings quite a few transitive packages:

```
MSTest
  ↓
MSTest.TestFramework
MSTest.TestAdapter
Microsoft.Testing.Platform
Newtonsoft.Json
Microsoft.ApplicationInsights
...
```

There is currently no:

```
packages.lock.json
```

So you don't yet have a repository artifact recording the complete dependency resolution.

That doesn't mean NuGet randomly picks packages on every restore. But from the perspective we're learning in this exercise, the repository itself does not currently record the complete resolved dependency graph.

* * *

# Let's turn the implicit pieces into explicit ones

The workbook doesn't prescribe a specific .NET implementation for this, so the following is **our implementation choice** for the lab.

I'd add three controls.

### A. Pin the SDK with `global.json`

At the repository root:

```JSON
{
  "sdk": {
    "version": "9.0.315",
    "rollForward": "disable"
  }
}
```

This is intentionally strict.

It says:

```
This repository requires SDK 9.0.315
```

instead of:

```
Whatever suitable .NET 9 SDK happens to be installed is probably fine.
```

There is a tradeoff.

**Strict pinning**

* more deterministic
* easier to reason about
* agents must have exactly that SDK

**Allowing roll-forward**

* easier agent maintenance
* security/patch updates are easier
* build toolchain isn't quite as tightly controlled

For this learning exercise, I prefer the strict version because it exposes the dependency clearly. In a real organization, you might deliberately choose a looser policy.

* * *

### B. Add a repository `NuGet.Config`

At the repository root:

```XML
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org"
         value="https://api.nuget.org/v3/index.json"
         protocolVersion="3" />
  </packageSources>
</configuration>
```

The important part is:

```XML
<clear />
```

Now the repository isn't inheriting all those machine-level feeds.

Instead:

```
Repository NuGet.Config
       │
       └── nuget.org
```

becomes the declared restore configuration.

This also prevents something like your disabled company feeds from unexpectedly becoming part of this portfolio project's build if somebody changes their machine configuration later.

* * *

### C. Lock dependency resolution

For this lab, we can enable a NuGet lock file.

In each project file—or eventually centrally—we can add:

```XML
<PropertyGroup>
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
</PropertyGroup>
```

Then run:

```PowerShell
dotnet restore
```

You should get:

```
packages.lock.json
```

for the projects.

Those should be committed to Git.

Then CI can eventually restore using:

```PowerShell
dotnet restore --locked-mode
```

That changes the meaning of restore from approximately:

```
Resolve dependencies according to the project
```

to:

```
Restore the dependency graph we've already approved;
fail if the project requires the graph to change.
```

That will give us a very useful failure case later.

* * *

## Notice what we're doing architecturally

We started with:

```
              DEVELOPMENT MACHINE
       ┌─────────────────────────────┐
       │ SDK version                 │
Repo ──┼─ NuGet source configuration │──► Build
       │ package resolution          │
       │ OS/runtime assumptions      │
       └─────────────────────────────┘
```

We're moving toward:

```
              REPOSITORY
       ┌──────────────────────────┐
       │ global.json              │
       │ NuGet.Config             │
       │ project files            │
       │ packages.lock.json       │
       │ build instructions       │
       └────────────┬─────────────┘
                    │
                    ▼
                  Agent
                    │
                    ▼
                  Build
```

The agent still matters.

But increasingly the repository tells the agent **what environment it must provide**, rather than silently accepting whatever environment the agent already has.

That distinction is one of the most important ideas in this week.

## Before continuing

Go ahead and add:

```
global.json
NuGet.Config
```

and enable the lock files.

Then run:

```PowerShell
dotnet --version
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

We aren't publishing yet.

If all four work, send me the output plus whether `packages.lock.json` files appeared.

Then we'll move from **dependency reproducibility** into **producing the actual self-contained `win-x64` artifact**, and we'll decide exactly where the boundary should be between **Compile**, **Test**, and **Package**.

* * *

The workbook doesn’t prescribe a specific .NET mechanism for adding the central RestorePackagesWithLockFile; for your repo, the usual MSBuild approach is to put the property in a **`Directory.Build.props`** file at the repository root.

Create:

```
technical-leadership-lab/
├── Directory.Build.props
├── global.json
├── NuGet.Config
├── PolicyService/
├── PolicyService.Tests/
└── ...
```

Then add:

```XML
<Project>
  <PropertyGroup>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>
</Project>
```

MSBuild automatically imports `Directory.Build.props` for projects underneath that directory, so both `PolicyService` and `PolicyService.Tests` inherit the setting. You do **not** need to add the property to each `.csproj`.

Then run:

```PowerShell
dotnet restore
```

You should see a separate `packages.lock.json` generated for each project, roughly:

```
PolicyService/
├── PolicyService.csproj
└── packages.lock.json

PolicyService.Tests/
├── PolicyService.Tests.csproj
└── packages.lock.json
```

Commit those lock files to Git.

### One distinction worth making

This property:

```XML
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
```

means:

> Generate and use a lock file.

It does **not** mean:

> Fail if dependency resolution differs from the lock file.

For that, CI can later use:

```PowerShell
dotnet restore --locked-mode
```

So our eventual behavior becomes:

```
Developer changes package reference
        ↓
dotnet restore
        ↓
packages.lock.json changes
        ↓
Developer reviews + commits it
        ↓
CI runs dotnet restore --locked-mode
        ↓
Unexpected dependency change = BUILD FAILURE
```

That's exactly the kind of reproducibility control we're trying to create in Week 2.


* * *

You’ve now completed the first important part of Week 2: **the restore/build/test chain is working with substantially more of its dependencies declared by the repository rather than inherited silently from your machine.** That supports the workbook’s goals around reproducible builds and understanding agent dependencies.

A few observations output are worth recording.

Your repository should now effectively say:

```
SDK              = 9.0.315        via global.json
Target framework = net9.0         via .csproj
Package sources  = declared       via NuGet.Config
Package graph    = locked         via packages.lock.json
Test framework   = declared       via NuGet
```

And this is particularly significant:

```PowerShell
dotnet restore --locked-mode
```

succeeded. That means the project declarations and the committed/resolved dependency graph currently agree.

You should commit both `packages.lock.json` files. They're now part of the evidence required to reproduce the build.

One nuance: we haven't made the **entire build environment reproducible**. An agent would still have to provide at least the required .NET SDK and an operating environment capable of executing the tools. What we've done is make those requirements much easier to discover and enforce.

## Next: define the artifact boundary

We've established:

```
Restore ✓
   ↓
Compile ✓
   ↓
Unit Tests ✓
```

Now we need:

```
Publish
   ↓
Package
   ↓
Artifact
```

This is where your earlier decision to make the application self-contained becomes concrete.

For this lab, I suggest we target:

```
Configuration: Release
Runtime:       win-x64
Deployment:    self-contained
Format:        ZIP
```

That gives us a clear artifact contract:

> **PolicyService is distributed as a self-contained Windows x64 ZIP produced only after restore, compilation, and unit tests succeed.**

There's an intentional tradeoff here:

```
Self-contained
+ No separately installed .NET runtime required on target
+ More predictable runtime
+ Fewer deployment-machine prerequisites

- Larger artifact
- Runtime security updates require rebuilding/redeploying
- Artifact is platform-specific
```

That's good Week 2 material because we're making a choice rather than treating “self-contained” as universally better.

### Let's produce it manually first

From the repository root, create a clean artifact area:

```PowerShell
Remove-Item .\artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path .\artifacts\publish -Force
```

Then publish:

```PowerShell
dotnet publish .\src\PolicyService\PolicyService.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    --no-restore `
    -o .\artifacts\publish
```

Don't ZIP it yet.

Afterward, run:

```PowerShell
Get-ChildItem .\artifacts\publish
```

and:

```PowerShell
(Get-ChildItem .\artifacts\publish -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
```

I'm specifically interested in what files appear and how large the published output becomes.

### There is also one experiment I want you to try

From the `artifacts\publish` directory, run the generated executable directly:

```PowerShell
.\PolicyService.exe
```

Assuming the service starts normally, that demonstrates something useful:

```
Your source/build machine's globally installed runtime
                 ≠
runtime required by the deployed artifact
```

because the artifact contains the runtime it needs.

That gives us an actual test of the architectural decision rather than merely assuming `--self-contained true` did what we wanted.

Once you have the file listing and approximate size, then we'll deal with the more interesting problem: **how do we identify this artifact so that six months later we can prove exactly which Git commit produced it?** That takes us directly into the workbook's artifact provenance requirement.

* * *

Because your development machine already has .NET 9 installed, both of these could successfully run as `PolicyService.exe`:

```
Framework-dependent app
    └── finds installed .NET runtime → runs

Self-contained app
    └── uses runtime shipped beside the app → runs
```

So merely executing the `.exe` on your development machine does **not** distinguish between them.

### What would actually prove it?

The strongest test is to copy the published folder to a clean Windows x64 machine that **does not have .NET 9 installed** and run:

```PowerShell
.\PolicyService.exe
```

If it runs there, you've demonstrated that the artifact doesn't depend on a separately installed .NET runtime.

A Windows Sandbox or throwaway VM is ideal for this.

You can also gather supporting evidence from the publish directory. A self-contained .NET publish should contain runtime components such as:

```
PolicyService.exe
PolicyService.dll

coreclr.dll
clrjit.dll
hostfxr.dll
hostpolicy.dll

System.Private.CoreLib.dll
System.*.dll
Microsoft.*.dll
...
```

A framework-dependent deployment is much smaller because it expects those runtime components to already exist on the target machine.

So after:

```PowerShell
dotnet publish .\src\PolicyService\PolicyService.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o .\artifacts\publish
```

we can inspect:

```PowerShell
Get-ChildItem .\artifacts\publish |
    Where-Object Name -in @(
        "coreclr.dll",
        "hostfxr.dll",
        "hostpolicy.dll",
        "System.Private.CoreLib.dll"
    )
```

If those are there, that's strong evidence that the runtime has been bundled.

### There's a useful Week 2 lesson hiding here

This is actually a perfect example of the distinction we're studying:

> **Successful execution is not necessarily evidence that a dependency doesn't exist.**

On your development machine:

```
PolicyService.exe runs
```

only tells us:

> Everything required to execute the application was available somehow.

It doesn't tell us **where those requirements came from**.

That's analogous to the self-hosted-agent problem in Week 2: a build can work because the agent happens to have some undeclared software installed. The workbook specifically wants you to identify those implicit dependencies rather than equating “it works” with “it's reproducible.”

So for our lab, I'd use two levels of evidence:

```
Build-time evidence
    --self-contained true
    + runtime files present in publish output

Execution evidence
    Run artifact on clean win-x64 machine with no .NET installed
```

The second one is the actual demonstration that we've eliminated the target machine's .NET runtime as a deployment prerequisite.

* * *

There is one architectural detail I'd change, though.

`Directory.Build.props` applies to **every project underneath it**, which means your test project is also inheriting:

```XML
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<SelfContained>true</SelfContained>
```

The tests don't really need to be a self-contained deployable application. Those properties describe the **PolicyService artifact**, not necessarily every project in the repository.

I'd therefore keep the genuinely repository-wide properties centrally:

```XML
<Project>
  <PropertyGroup>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>
```

And put these in `PolicyService.csproj`:

```XML
<PropertyGroup>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <SelfContained>true</SelfContained>
</PropertyGroup>
```

That gives you a cleaner separation:

```
Directory.Build.props
│
├── All projects use net9.0
└── All projects use NuGet lock files

PolicyService.csproj
│
├── Deploys to win-x64
└── Ships self-contained

PolicyService.Tests.csproj
│
└── Normal net9.0 test project
```

There is a legitimate alternative: if you deliberately want **every executable in this repository** constrained to Windows x64, keeping the RID centrally could be a conscious choice. But right now, that seems broader than the requirement we're trying to express.

And you've just uncovered a useful Week 2 principle:

> Put a dependency or constraint at the narrowest level where it is actually true.

Otherwise central configuration can itself become a hidden source of coupling.

### Our build dependency picture is now much better

We have:

```
Git source
   +
global.json
   → exact SDK expectation

Directory.Build.props
   → target framework
   → lock-file requirement

NuGet.Config
   → package sources

packages.lock.json
   → resolved dependency graph

PolicyService.csproj
   → runtime target
   → self-contained deployment
```

That is substantially more reproducible than where we started.

I'd make that small scoping change, run:

```PowerShell
dotnet restore
dotnet restore --locked-mode
dotnet build --no-restore
dotnet test --no-build
```

and, assuming they're clean, consider the **dependency-declaration portion of Week 2 complete**.

Then we're ready for the next piece: producing the actual Release artifact and giving it a **traceable identity tied to its Git commit**.


* * *
Next we’ll tackle **artifact creation + provenance**. This corresponds to the Week 2 requirements to stamp the artifact with a version traceable to the commit and keep environment-specific configuration out of it.

I want to do this manually once before we automate it in the pipeline.

### 1. Start from a known source revision

First, make sure your current Week 2 changes are committed:

```PowerShell
git status
```

If you still need to commit them, do that now. Then capture the revision:

```PowerShell
$commit = git rev-parse HEAD
$shortCommit = git rev-parse --short=8 HEAD

$commit
$shortCommit
```

You should get something like:

```
Full:  a42c7f9e0...
Short: a42c7f9e
```

The **full commit SHA is provenance**. It tells us exactly which source revision produced the artifact.

* * *

### 2. Give the application a version

For the exercise, let's separate the human-readable application version from the source identity:

```PowerShell
$version = "0.2.0"
$informationalVersion = "$version+$shortCommit"
```

Conceptually:

```
Application version:     0.2.0
Source revision:         a42c7f9e...
Informational version:   0.2.0+a42c7f9e
```

Why both?

`0.2.0` communicates the logical application version, while the Git SHA gives us exact provenance.

Two artifacts can both represent version `0.2.0`, but:

```
0.2.0+a42c7f9e
0.2.0+98ee20bc
```

clearly came from different source revisions.

* * *

### 3. Produce a Release publish

Clean the previous output:

```PowerShell
Remove-Item .\artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path .\artifacts\publish -Force
```

Then publish:

```PowerShell
dotnet publish .\src\PolicyService\PolicyService.csproj `
    -c Release `
    --no-restore `
    -o .\artifacts\publish `
    -p:Version=$version `
    -p:InformationalVersion=$informationalVersion `
    -p:SourceRevisionId=$commit
```

Because you've already declared:

```XML
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<SelfContained>true</SelfContained>
```

in the application project, we don't have to repeat those properties on the command line.

That's useful in itself: the artifact contract lives in source control rather than in somebody's memory.

* * *

### 4. Verify that the compiled application carries that identity

Try:

```PowerShell
(Get-Item .\artifacts\publish\PolicyService.dll).VersionInfo |
    Format-List FileVersion,ProductVersion
```

I'm especially interested in `ProductVersion`. We want evidence that the source identity made it into the compiled output.

Depending on how .NET composes the informational version, you may see the commit represented there.

* * *

### 5. Add explicit provenance metadata

I also want the artifact to be understandable without inspecting a DLL.

Create a small manifest:

```PowerShell
$metadata = [ordered]@{
    artifact          = "PolicyService"
    version           = $version
    sourceRevision    = $commit
    targetFramework   = "net9.0"
    runtimeIdentifier = "win-x64"
    selfContained     = $true
    sdkVersion        = (dotnet --version)
}

$metadata |
    ConvertTo-Json |
    Set-Content .\artifacts\publish\artifact-manifest.json
```

Then:

```PowerShell
Get-Content .\artifacts\publish\artifact-manifest.json
```

You should end up with something roughly like:

```JSON
{
  "artifact": "PolicyService",
  "version": "0.2.0",
  "sourceRevision": "a42c7f9e...",
  "targetFramework": "net9.0",
  "runtimeIdentifier": "win-x64",
  "selfContained": true,
  "sdkVersion": "9.0.315"
}
```

Now the artifact can answer:

> What source produced me?

without requiring access to the original pipeline.

* * *

### 6. Package the artifact

Then create the ZIP:

```PowerShell
$artifactName = "PolicyService-$version-$shortCommit.zip"

Compress-Archive `
    -Path .\artifacts\publish\* `
    -DestinationPath ".\artifacts\$artifactName"
```

List it:

```PowerShell
Get-ChildItem .\artifacts
```

You'll have something like:

```
publish/
PolicyService-0.2.0-a42c7f9e.zip
```

Now our build product has a meaningful identity rather than just being called `output.zip`.

* * *

### 7. Fingerprint the exact artifact

Finally:

```PowerShell
Get-FileHash ".\artifacts\$artifactName" -Algorithm SHA256
```

This introduces an important distinction:

```
Git commit SHA
    ↓
identifies the source

Artifact SHA-256
    ↓
identifies the exact ZIP bytes
```

Those aren't interchangeable.

If someone modifies even one byte inside that ZIP, its SHA-256 changes.

That's where **immutability** starts to become concrete. Once this ZIP has been tested and accepted as the build artifact, we should not:

```
unzip it
change appsettings.json
replace a DLL
add another file
rezip it
```

and continue pretending it's the same artifact.

It would be a **different artifact**, even if somebody left the filename unchanged.

That's the central idea behind the eventual ADR-001.

Run through those steps and send me:

* the `FileVersion` / `ProductVersion`,
* the generated `artifact-manifest.json`,
* the ZIP filename,
* and its SHA-256.

Then we'll move into the really important Week 2 transition: taking this manual sequence—

```
Restore
→ Compile
→ Test
→ Publish
→ Package
→ Publish Artifact
```

—and expressing it as **pipeline-as-code**, with failure behavior at each stage.

* * *

You should now have a traceable artifact with both **source provenance** and **byte-level identity**, which is exactly where Week 2 is trying to get you before moving into CI. The workbook requires the artifact to be versioned back to the commit and later treated as immutable.

A few things stand out.

Your artifact identity is now:

```
Application version:
0.2.0

Source revision:
5cc85e2dd1547cf5f2ccfd0ab3445d27bcd09c1c

Artifact filename:
PolicyService-0.2.0-5cc85e2d.zip

Artifact SHA-256:
14628C400D740373EA669E7F534792C1FDB93CA294E7F8F5EE0F77DE3077EA89
```

That gives you three different concepts:

```
0.2.0
   ↓
logical application version

5cc85e2d...
   ↓
exact source revision

14628C40...
   ↓
exact packaged bytes
```

That separation is worth keeping.

Your separate:

```JSON
"sourceRevision": "5cc85e2dd1547cf5f2ccfd0ab3445d27bcd09c1c"
```

in `artifact-manifest.json` remains the clearest human-readable provenance record anyway.

## Now we're ready for pipeline-as-code

The manual process we've proven is:

```
Restore
  ↓
Compile
  ↓
Unit Tests
  ↓
Publish
  ↓
Create manifest
  ↓
ZIP
  ↓
Hash
  ↓
Publish Artifact
```

The important behavior we need to preserve is:

```
Restore fails
    → stop

Compile fails
    → stop

Tests fail
    → stop

Publish/package fails
    → stop

Only then
    → publish artifact
```

That directly satisfies the Week 2 requirement that failed tests prevent artifact publication.

I would now create:

```
/pipelines
    ci.yml
```

Since the portfolio exercise is meant to teach the workflow rather than tie you to your company's on-prem ADO constraints, we can write this as a clean Azure Pipelines YAML definition first.

A first version would look structurally like:

```YAML
trigger:
- main

pool:
  vmImage: windows-latest

steps:

- checkout: self

- task: UseDotNet@2
  displayName: Install .NET SDK

- script: dotnet restore --locked-mode
  displayName: Restore

- script: dotnet build --configuration Release --no-restore
  displayName: Compile

- script: dotnet test --configuration Release --no-build
  displayName: Unit Tests

- script: dotnet publish ...
  displayName: Publish

- powershell: |
    # create artifact-manifest.json
    # zip output
    # calculate SHA256
  displayName: Package Artifact

- task: PublishBuildArtifacts@1
  displayName: Publish Artifact
```

But before we write the final YAML, there’s one design decision worth making:

### What should the CI version be?

You currently used:

```
0.2.0
```

manually.

For CI, we need something repeatable and unique. Common options are:

```
0.2.0.<build-number>
```

or:

```
0.2.0+<commit>
```

or:

```
<build-number>-<commit>
```

For this lab, I would use:

```
0.2.<pipeline-run-number>
```

for the human-readable version, while keeping the full Git commit separately as provenance.

For example:

```
Version:
0.2.37

Commit:
5cc85e2dd1547...

Artifact:
PolicyService-0.2.37-5cc85e2d.zip
```

That gives you:

* a convenient monotonically increasing build identity,
* exact source traceability,
* and a unique artifact filename.

Next we'll write the actual `ci.yml` together and make sure each stage corresponds cleanly to the manual process you just proved.

* * *

Let's make the pipeline run ID the human-friendly build version while keeping the Git SHA as the exact source identity.

The Week 2 lab wants the workflow `Restore → Compile → Unit Tests → Package → Publish Artifact`, requires failed tests to prevent publication, and requires the artifact version to be traceable back to the commit.

Create:

```
/pipelines/ci.yml
```

with this first version:

```YAML
trigger:
  branches:
    include:
      - main

pool:
  vmImage: 'windows-latest'

variables:
  VersionPrefix: '0.2'

steps:
- checkout: self
  clean: true

# Use global.json as the source of truth for the SDK version.
- task: UseDotNet@2
  displayName: 'Install .NET SDK'
  inputs:
    packageType: 'sdk'
    useGlobalJson: true
    workingDirectory: '$(Build.SourcesDirectory)'

# Build.BuildId is the pipeline-run number we will use for the application version.
- powershell: |
    $ErrorActionPreference = 'Stop'

    $version = "$(VersionPrefix).$(Build.BuildId)"
    $commit = "$(Build.SourceVersion)"
    $shortCommit = $commit.Substring(0, 8)

    Write-Host "Application version: $version"
    Write-Host "Source revision: $commit"
    Write-Host "Short revision: $shortCommit"

    Write-Host "##vso[task.setvariable variable=AppVersion]$version"
    Write-Host "##vso[task.setvariable variable=ShortCommit]$shortCommit"
  displayName: 'Calculate Artifact Version'

- powershell: |
    dotnet restore --locked-mode

    if ($LASTEXITCODE -ne 0) {
        throw "Package restore failed."
    }
  displayName: 'Restore'

- powershell: |
    dotnet build `
      --configuration Release `
      --no-restore `
      -p:Version=$(AppVersion) `
      -p:SourceRevisionId=$(Build.SourceVersion)

    if ($LASTEXITCODE -ne 0) {
        throw "Compilation failed."
    }
  displayName: 'Compile'

- powershell: |
    dotnet test `
      --configuration Release `
      --no-build

    if ($LASTEXITCODE -ne 0) {
        throw "Unit tests failed."
    }
  displayName: 'Unit Tests'

- powershell: |
    $ErrorActionPreference = 'Stop'

    $publishPath = "$(Build.SourcesDirectory)\artifacts\publish"

    Remove-Item "$(Build.SourcesDirectory)\artifacts" `
      -Recurse `
      -Force `
      -ErrorAction SilentlyContinue

    New-Item `
      -ItemType Directory `
      -Path $publishPath `
      -Force | Out-Null

    dotnet publish `
      "$(Build.SourcesDirectory)\src\PolicyService\PolicyService.csproj" `
      --configuration Release `
      --no-build `
      --no-restore `
      --output $publishPath

    if ($LASTEXITCODE -ne 0) {
        throw "Application publish failed."
    }
  displayName: 'Publish Application'

- powershell: |
    $ErrorActionPreference = 'Stop'

    $publishPath = "$(Build.SourcesDirectory)\artifacts\publish"
    $artifactPath = "$(Build.ArtifactStagingDirectory)"

    New-Item `
      -ItemType Directory `
      -Path $artifactPath `
      -Force | Out-Null

    $metadata = [ordered]@{
        artifact          = "PolicyService"
        version           = "$(AppVersion)"
        pipelineRun       = "$(Build.BuildId)"
        sourceRevision    = "$(Build.SourceVersion)"
        sourceBranch      = "$(Build.SourceBranch)"
        targetFramework   = "net9.0"
        runtimeIdentifier = "win-x64"
        selfContained     = $true
        sdkVersion        = (dotnet --version)
    }

    $metadata |
      ConvertTo-Json |
      Set-Content "$publishPath\artifact-manifest.json"

    $artifactName = "PolicyService-$(AppVersion)-$(ShortCommit).zip"
    $zipPath = Join-Path $artifactPath $artifactName

    Compress-Archive `
      -Path "$publishPath\*" `
      -DestinationPath $zipPath

    $hash = Get-FileHash $zipPath -Algorithm SHA256

    "$($hash.Hash)  $artifactName" |
      Set-Content "$zipPath.sha256"

    Write-Host "Artifact: $artifactName"
    Write-Host "SHA256:   $($hash.Hash)"
  displayName: 'Package Artifact'

- task: PublishBuildArtifacts@1
  displayName: 'Publish Artifact'
  inputs:
    PathtoPublish: '$(Build.ArtifactStagingDirectory)'
    ArtifactName: 'PolicyService'
    publishLocation: 'Container'
```

A few design decisions here are intentional.

### Version identity

For pipeline run `37`, you'd get:

```
Application version
0.2.37

Git provenance
5cc85e2dd1547cf5f2ccfd0ab3445d27bcd09c1c

Artifact filename
PolicyService-0.2.37-5cc85e2d.zip
```

That means the pipeline run number and commit have different jobs:

```
0.2.37
  ↓
Convenient build/version identity

5cc85e2d...
  ↓
Exact source identity

SHA-256 of ZIP
  ↓
Exact artifact identity
```

### `global.json` remains authoritative

Notice that I didn't write:

```YAML
version: '9.0.315'
```

into the pipeline.

Instead:

```YAML
useGlobalJson: true
```

makes your existing `global.json` the source of truth.

That's preferable to declaring `9.0.315` twice:

```
global.json = 9.0.315
pipeline    = 9.0.315
```

because two declarations can eventually drift.

### Why Compile and Publish are separate

We're deliberately maintaining:

```
Compile
   ↓
Test compiled code
   ↓
Publish those binaries
```

rather than having `dotnet publish` silently become another compilation step.

That's why publish uses:

```PowerShell
--no-build
--no-restore
```

The artifact should emerge from the code that already passed the earlier gates.

And because the version properties are supplied during the **Compile** step:

```PowerShell
-p:Version=$(AppVersion)
-p:SourceRevisionId=$(Build.SourceVersion)
```

the binaries being tested already contain the identity that ultimately goes into the artifact.

### Most importantly: publication is downstream of tests

The dependency chain is:

```
Restore ✓
   ↓
Compile ✓
   ↓
Tests ✓
   ↓
Publish ✓
   ↓
Package ✓
   ↓
PublishBuildArtifacts
```

Azure Pipelines normally runs subsequent steps only when preceding steps succeed.

So if this happens:

```
Unit Tests
3 passed
1 failed
```

execution stops there.

There should be **no `PolicyService` build artifact published for that run**.

That is the behavior we're going to deliberately test later, because Week 2 explicitly asks you to break compilation, a test, and package restoration and record where each failure surfaces.

One thing to check before committing this: if your default branch isn't actually named `main`, replace that trigger branch with whatever you're using.

Otherwise, put this in `pipelines/ci.yml`, commit it, and run it. Don't troubleshoot ahead of time. If it fails, **the failure itself is part of the Week 2 exercise**—send me the stage and error output, and we'll diagnose which boundary failed and why.

* * *
For this Week 2 exercise, GitHub Actions is a good option. The workbook is testing whether you can build a **pipeline-as-code CI workflow** with restore, compile, tests, packaging, provenance, and artifact publication; it does not require Azure DevOps specifically.

In fact, using GitHub Actions will let you complete the lab without waiting on permissions.

If the repository is not already hosted on GitHub, create a GitHub repository and push your existing repo there. Then instead of:

```
/pipelines/ci.yml
```

GitHub expects workflows under:

```
/.github/workflows/ci.yml
```

Your pipeline maps almost one-to-one:

```
Azure Pipelines          GitHub Actions
─────────────────────────────────────────
checkout                 actions/checkout
UseDotNet                actions/setup-dotnet
Build.BuildId            github.run_number
Build.SourceVersion      github.sha
Build.SourceBranch       github.ref
PublishBuildArtifacts    actions/upload-artifact
```

For our lab, I'd use the following workflow.

```YAML
name: PolicyService CI

on:
  push:
    branches:
      - main
  pull_request:
    branches:
      - main

jobs:
  build:
    runs-on: windows-latest

    env:
      VERSION_PREFIX: "0.2"

    steps:
      - name: Checkout source
        uses: actions/checkout@v4

      - name: Install .NET SDK
        uses: actions/setup-dotnet@v4
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
        uses: actions/upload-artifact@v4
        with:
          name: PolicyService
          path: artifacts/package/
```

The versioning scheme now works like this:

```
GitHub Actions run #17
        │
        ├── Version
        │   0.2.17
        │
        ├── Commit
        │   5cc85e2dd1547...
        │
        └── Artifact
            PolicyService-0.2.17-5cc85e2d.zip
```

And you still retain the three identities we've been discussing:

```
0.2.17
   │
   └── human-friendly build identity

5cc85e2dd154...
   │
   └── exact source identity

SHA-256
   │
   └── exact artifact-byte identity
```

One reason GitHub Actions is particularly useful for this lab is that `windows-latest` gives us a relatively clean hosted build machine for every job. That actually gives you a nice comparison with the self-hosted-agent topic in the workbook:

```
Your workstation
    ↓
Has accumulated software and configuration

GitHub-hosted runner
    ↓
Fresh standardized runner for the job
```

Your repository still has to declare what it needs—`global.json`, `NuGet.Config`, lock files, project configuration—but you aren't relying on a long-lived company build server that may have accumulated undocumented dependencies.

Later, for the Week 2 stretch goal, you could attach a **GitHub self-hosted runner** and run essentially the same workflow against it. Then we could inventory everything that machine needs and compare the hosted versus self-hosted behavior. That would satisfy the workbook's stretch exercise very naturally.

For now, create `.github/workflows/ci.yml`, push it to GitHub, and let it run. If it fails, send me the failed step and its output rather than changing things immediately. The failure diagnosis itself is part of what we're trying to practice this week.

* * *

Since your workflow currently has only `push` and `pull_request` triggers, there are two ways to run it.

If you pushed the workflow file to `main`, it should already have triggered automatically because of:

```YAML
on:
  push:
    branches:
      - main
```

Go to your GitHub repository and open the **Actions** tab. You should see a workflow named **PolicyService CI** and a run corresponding to your recent push.

If you pushed to a branch other than `main`, it won't run yet unless you open a pull request into `main`.

For this lab, I recommend also enabling **manual execution**, because we'll intentionally rerun the pipeline several times while breaking restore, compilation, and tests.

Add `workflow_dispatch`:

```YAML
name: PolicyService CI

on:
  push:
    branches:
      - main

  pull_request:
    branches:
      - main

  workflow_dispatch:
```

Commit and push that change.

After GitHub receives it, go to:

**Repository → Actions → PolicyService CI**

You should now see a **Run workflow** button on the right side. Click it, choose the branch, then click **Run workflow**.

So you'll now have three ways to execute CI:

```
Push to main
      │
      ▼
Automatic CI run


Pull request → main
      │
      ▼
Automatic CI run


Actions → Run workflow
      │
      ▼
Manual CI run
```

For Week 2, manual execution is particularly convenient because we'll shortly do the workbook's required experiments:

```
Normal build       → artifact published

Broken restore     → stops at Restore
Broken compilation → stops at Compile
Broken unit test   → stops at Unit Tests
```

and verify that none of the failed runs publish the application artifact.

If you've already pushed to `main`, I'd first open **Actions** before changing anything—you may already have your first CI run waiting for you.

* * *

If you see a warning, it is probably coming from one or more of the GitHub Actions we pinned to older major versions. GitHub has moved its JavaScript action runtime from Node 20 to Node 24, and the current official versions of the actions we’re using support Node 24. [GitHub+2GitHub+2](https://github.com/actions/checkout/blob/main/CHANGELOG.md?utm_source=chatgpt.com)

Update these three lines:

```YAML
- uses: actions/checkout@v4
```

to:

```YAML
- uses: actions/checkout@v7
```

Update:

```YAML
- uses: actions/setup-dotnet@v4
```

to:

```YAML
- uses: actions/setup-dotnet@v6
```

And update:

```YAML
- uses: actions/upload-artifact@v4
```

to:

```YAML
- uses: actions/upload-artifact@v7
```

Those current versions use the Node 24 action runtime. [GitHub+2GitHub+2](https://github.com/actions/checkout/blob/main/action.yml?utm_source=chatgpt.com)

So the relevant pieces of your workflow become:

```YAML
steps:
  - name: Checkout source
    uses: actions/checkout@v7

  - name: Install .NET SDK
    uses: actions/setup-dotnet@v6
    with:
      global-json-file: global.json

  # ... restore/build/test/package ...

  - name: Publish Artifact
    uses: actions/upload-artifact@v7
    with:
      name: PolicyService
      path: artifacts/package/
```

Because you’re using GitHub-hosted `windows-latest` runners, you shouldn’t need to do anything else. The Node 24-based action releases require a sufficiently recent Actions runner, and GitHub manages that runner version for hosted runners. That minimum runner requirement mainly matters later if we experiment with a self-hosted runner. [GitHub+1](https://github.com/actions/checkout/blob/main/README.md?plain=1&utm_source=chatgpt.com)

I would **not** add `actions/setup-node` here. Your application doesn’t depend on Node; Node is just the internal runtime GitHub uses to execute these actions. Updating the action versions is the appropriate fix.

Commit those three version changes and manually run the workflow once. If the Node 20 warning disappears and the artifact still publishes successfully, we can move on to the Week 2 failure-injection portion.

* * *

Yes. At this point, you’ve essentially completed Week 2 lab steps 1–4: the CI workflow exists, failed upstream steps block publication by default, the artifact is traceable to the pipeline run and commit, and environment-specific configuration is not being baked into the artifact. The next required lab step is to **intentionally break package restoration, compilation, and one unit test, then record where and how each failure surfaces**.

I’d do these as three controlled experiments, one at a time, and revert each change before moving to the next.

### Experiment 1: Break package restoration

This one is valuable because it proves that your build fails at the **Restore** boundary rather than later during compilation.

Temporarily edit one package reference in a `.csproj` to a version that does not exist. For example, in the test project:

```XML
<PackageReference Include="MSTest" Version="999.999.999" />
```

Commit and push that change to the branch your workflow runs against, or use a temporary test branch plus manual workflow dispatch.

Expected behavior:

```
Checkout        ✓
Setup .NET      ✓
Version         ✓
Restore         ✗
Compile         skipped
Unit Tests      skipped
Publish         skipped
Package         skipped
Publish Artifact skipped
```

In GitHub Actions, open the failed run and inspect the **Restore** step. You should see NuGet report that it cannot resolve the requested package/version.

What I want you to record for the Week 2 lab is something like:

```
Failure injected:
Invalid NuGet package version

Failure stage:
Restore

Observed behavior:
dotnet restore --locked-mode failed before compilation.

Downstream behavior:
Compile, test, package, and artifact publication did not run.

Detection:
GitHub Actions marked the Restore step and job as failed.
```

There’s a second thing I’m particularly interested in because we’re using lock files. Since you changed a package reference without updating `packages.lock.json`, `--locked-mode` may actually complain about the **lock file being inconsistent with the project** before it even gets as far as discovering that `999.999.999` doesn’t exist.

If that happens, don’t “fix” it. That is actually better evidence of the reproducibility control we added.

Conceptually:

```
Project dependency changed
          ↓
packages.lock.json no longer matches
          ↓
--locked-mode detects unexpected dependency change
          ↓
RESTORE FAILS
```

That demonstrates why the lock file exists.

### What to inspect in GitHub

After the failure, check two things:

1. The **Restore** step should be the first meaningful failure.
2. At the bottom of the run, there should be **no newly published `PolicyService` artifact** for that failed run.

The second point matters because the workbook specifically requires failed work to prevent artifact publication.

Once you’ve run this first experiment, check the relevant Restore error. We’ll classify exactly what failed and why, then revert the package change and move to the **intentional compile failure**.

* * *

Because you have:

```XML
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
```

any successful NuGet restore that reevaluates the project is allowed to update `packages.lock.json`. VS Code's .NET tooling can trigger background/design-time restores after a `.csproj` change, so it may appear that VS Code itself is updating the lock file.

But your CI result tells us something more specific: the lock file that GitHub actually received did **not** match the changed project.

The key part is:

```
Lock file's package references:
Microsoft.NET.Test.Sdk:[17.12.0, )

project's package references:
Microsoft.NET.Test.Sdk:[17.12.0, ),
MSTest:[999.999.999, )
```

So from GitHub's perspective:

```
.csproj
MSTest = 999.999.999
        │
        │ does not match
        ▼
packages.lock.json
MSTest not represented as expected
```

and `--locked-mode` stopped immediately.

That is exactly what we wanted the control to do.

### Why might you have seen the lock file change locally?

A few possibilities are consistent with what happened:

* VS Code triggered a background restore and modified the file, but the invalid package prevented a complete successful update.
* The lock file changed temporarily but wasn't saved or committed in the state you expected.
* The background restore changed some contents but couldn't produce a valid graph for `MSTest 999.999.999`.
* You have two lock files, and the one you noticed wasn't the test project's lock file that GitHub evaluated.

You can verify exactly what was committed with:

```PowerShell
git status
```

and:

```PowerShell
git diff HEAD -- tests/PolicyService.Tests/packages.lock.json
```

Or to see the lock file in the commit GitHub built:

```PowerShell
git show HEAD:tests/PolicyService.Tests/packages.lock.json
```

### More importantly: this experiment succeeded

We were trying to test the Week 2 requirement to intentionally break package restoration and record where the failure surfaces.

Your observation is:

```
Injected failure:
Changed MSTest dependency to 999.999.999

Detected at:
Restore

Detection mechanism:
NuGet locked-mode validation

Error:
NU1004 — project dependencies and packages.lock.json are inconsistent

Downstream result:
Compile skipped
Tests skipped
Publish skipped
Package skipped
Artifact publication skipped
```

That's actually a more interesting result than simply receiving "package 999.999.999 not found."

You demonstrated that your CI system prevents an **unreviewed dependency-graph change** before it even attempts to compile the application.

The sequence was:

```
Developer changes dependency
        ↓
Project declaration differs
        ↓
Locked dependency graph no longer agrees
        ↓
Restore rejects the build
        ↓
No artifact exists
```

That's a real reproducibility control, not just a package download mechanism.

### One subtle distinction

Had you run locally:

```PowerShell
dotnet restore
```

without `--locked-mode`, NuGet would normally be allowed to reevaluate and update the lock file if it could resolve the new dependency.

CI deliberately does:

```PowerShell
dotnet restore --locked-mode
```

which means:

> Don't silently change dependency resolution during this build. The checked-in dependency graph must already agree with the project.

That's exactly the behavior we want from CI.

For your Week 2 notes, I would consider **package restoration failure complete**.

Now revert the intentionally bad package change and make sure your known-good pipeline is green again. After that, we'll perform the second experiment: **a compilation failure**, which should get past Restore and fail specifically at Compile.

* * *

The second required Week 2 experiment is to **break compilation and verify that the failure is detected specifically at the Compile stage**, after Restore has succeeded.

First, make sure you reverted the bad MSTest version and that your dependency files are back to their known-good state. Ideally, you should have a green run before introducing the next failure.

For a clean, controlled compile failure, I’d avoid modifying existing application logic. Add a temporary file such as:

```
src/PolicyService/CompileFailure.cs
```

with:

```C#
namespace PolicyService;

public class CompileFailure
{
    public void BreakCompilation()
    {
        int value = "this cannot be assigned to an int";
    }
}
```

This is useful because the failure is unambiguous:

```
string
  ↓
assigned to int
  ↓
compiler error
```

You should get an error similar to:

```
CS0029: Cannot implicitly convert type 'string' to 'int'
```

Commit and push that change, then run the workflow.

### Expected behavior

This time the pipeline should look like:

```
Checkout            ✓
Install .NET SDK     ✓
Calculate Version    ✓
Restore              ✓
Compile              ✗
Unit Tests           skipped
Publish Application  skipped
Package Artifact     skipped
Publish Artifact     skipped
```

The important contrast with Experiment 1 is:

```
Experiment 1
Dependency graph invalid
        ↓
Restore fails


Experiment 2
Dependency graph valid
        ↓
Restore succeeds
        ↓
Source cannot compile
        ↓
Compile fails
```

That demonstrates that the pipeline is detecting the problem at the correct boundary rather than simply reporting “the build failed” somewhere downstream.

For your Week 2 notes, capture it in roughly this form:

```
Failure injected:
Invalid C# type assignment

Failure stage:
Compile

Observed behavior:
Restore completed successfully.
dotnet build failed with compiler error CS0029.

Downstream behavior:
Unit tests, publish, package, and artifact publication did not execute.

Detection:
GitHub Actions marked the Compile step and build job as failed.
```

Also check that the failed run has **no PolicyService artifact attached**. That gives us additional evidence that an invalid build cannot accidentally produce something that appears releasable.

Once you've run it, check the Compile error and whether the later steps were skipped. Then we'll revert this file and move to the third experiment: **a failing unit test**, which is the most important one for proving that “compiled successfully” does not mean “eligible to become an artifact.”

* * *

By this point you have now demonstrated:

```
Restore   ✓
Compile   ✗  CS0029
Tests     skipped
Publish   skipped
Package   skipped
Artifact  skipped
```

The failure was detected at the correct stage, and the compiler gave you a precise source-level error. That satisfies the Week 2 requirement to intentionally break compilation and record where/how it surfaces.

For your notes, I’d record it like this:

```
Failure injected:
Invalid assignment of a string to an int.

Failure stage:
Compile

Observed behavior:
Restore completed successfully.
dotnet build failed with CS0029:
"Cannot implicitly convert type 'string' to 'int'."

Downstream behavior:
Unit tests, publish, packaging, and artifact publication did not run.

Detection:
GitHub Actions marked the Compile step and workflow job as failed.
```

Now remove `CompileFailure.cs`, commit that cleanup, and make sure the workflow is green again.

Then we can do the third experiment: **a failing unit test**.

The goal this time is to prove something different:

```
Restore   ✓
Compile   ✓
Tests     ✗
Publish   skipped
Package   skipped
Artifact  skipped
```

That distinction matters because this experiment demonstrates that:

> successfully compiled code is still not eligible to become a releasable artifact if verification fails.

Add a temporary failing test to `PolicyService.Tests`. For example:

```C#
[TestMethod]
public void IntentionalFailure()
{
    Assert.AreEqual(1, 2);
}
```

Commit and push it, then run the workflow.

What I expect is a failure in the **Unit Tests** step, with the compile step still green and no artifact attached to the failed run.

Check the test failure output once it runs.

* * *

Pipeline should have failed on Unit Test step and not publish artifact. 

If so, the third experiment has also behaved exactly as intended.

You now have the full Week 2 failure matrix the workbook asks for: intentionally breaking package restoration, compilation, and a unit test, and observing where each failure surfaces.

For the unit-test experiment, I’d record:

```
Failure injected:
Intentional failing unit test.

Failure stage:
Unit Tests

Observed behavior:
Restore completed successfully.
Compilation completed successfully.
dotnet test executed 4 tests:
- 3 passed
- 1 failed

Failure:
Assert.AreEqual failed. Expected:<1>. Actual:<2>.

Downstream behavior:
Publish, package, and artifact publication did not execute.

Detection:
GitHub Actions marked the Unit Tests step and workflow job as failed.
No PolicyService artifact was produced.
```

The three experiments now show three distinct boundaries:

```
1. Dependency problem
   Restore ✗

2. Invalid source
   Restore ✓
   Compile ✗

3. Behavior verification failure
   Restore ✓
   Compile ✓
   Tests ✗
```

That distinction is important. A generic “pipeline failed” message tells you much less than knowing **which transformation or verification boundary rejected the change**.

And the third experiment proves an especially important property of your CI design:

```
Compiles successfully
        ≠
Approved build artifact
```

The code must also satisfy its verification requirements before it is packaged and published.

### Before moving on

Remove the intentional failing test, commit it, and get one final green CI run with an artifact published. That gives you a clean baseline after the experiments.

At that point, the required hands-on portion through step 5 is complete. The workbook has one remaining optional/stretch lab item:

> Run the same pipeline on a self-hosted agent and inventory everything the agent requires.

Given the Week 2 learning objective around **self-hosted agent dependencies and risks**, I think this one is worth doing, but we don't need to turn it into a major infrastructure project.

We can install a GitHub self-hosted runner on your Windows development machine, point the same workflow at it temporarily, and answer:

```
What does this machine need for the build to work?

Declared by repository:
- SDK version requirement
- NuGet sources
- dependency graph
- target framework
- RID
- build/test instructions

Still supplied by agent:
- Windows x64
- PowerShell
- Git
- network access
- filesystem space/permissions
- GitHub runner software
- ability to install/use .NET SDK
```

Then we can compare that with `windows-latest`.

After that, we’ll have all the evidence we need to write **ADR-001: Build Artifacts Are Immutable**, followed by the Week 2 reflection questions and knowledge check.

At this point you have demonstrated:

* `Restore → Compile → Unit Tests → Publish → Package → Publish Artifact`
* locked NuGet dependency resolution
* a pinned .NET SDK
* a self-contained `win-x64` artifact
* versioning with `0.2.<pipeline-run-number>`
* Git commit provenance
* an artifact manifest
* SHA-256 artifact identity
* failed restore prevents artifact creation
* failed compilation prevents artifact creation
* failed tests prevent artifact creation
* a working GitHub Actions CI workflow

So the major remaining portfolio deliverable is **ADR-001: Build Artifacts Are Immutable**.

## ADR-001: Build Artifacts Are Immutable

I would put this at:

```
docs/adrs/ADR-001-build-artifacts-are-immutable.md
```

Here is a draft based specifically on what you implemented:

```Markdown
# ADR-001: Build Artifacts Are Immutable

## Status

Accepted

## Context

PolicyService requires a repeatable way to turn a specific source revision into
a tested and traceable deployable artifact.

If an application is rebuilt independently for each environment, the resulting
outputs may differ because of changes in source, SDK versions, dependencies,
package resolution, build configuration, or build-machine state. This weakens
the ability to determine whether the software deployed to one environment is
the same software that was previously tested.

The CI workflow currently performs the following stages:

1. Restore dependencies in locked mode
2. Compile the application
3. Run unit tests
4. Publish the application
5. Package the published output
6. Publish the build artifact

If restore, compilation, or unit testing fails, no artifact is published.

The resulting PolicyService artifact is a self-contained `win-x64` ZIP file.
Each artifact contains provenance metadata including the application version,
pipeline run number, source revision, target framework, runtime identifier, and
SDK version.

The artifact filename also includes the application version and shortened Git
commit, and a SHA-256 hash is generated for the final ZIP.

Environment-specific configuration is not considered part of the immutable
application artifact.

## Decision

A successfully published build artifact will be treated as immutable.

Once the CI workflow publishes an artifact, its contents will not be modified.
The exact artifact that passes the build and verification process is the
artifact that should later be promoted through deployment environments.

Each artifact will have:

- A unique application version derived from the CI pipeline run number
- The Git commit SHA that produced it
- Build provenance recorded in `artifact-manifest.json`
- A SHA-256 hash identifying the exact packaged bytes

The current version format is:

`0.2.<pipeline-run-number>`

The artifact filename follows the form:

`PolicyService-<version>-<short-commit>.zip`

Environment-specific configuration will be supplied separately during
deployment rather than modifying the published artifact.

## Alternatives

### Rebuild the application for each environment

DEV, QA, and other environments could independently build the application from
source.

This was rejected because separate builds can produce different outputs due to
changes in dependencies, SDK versions, package sources, build configuration, or
build-machine state. A successful QA test would therefore not necessarily
validate the exact artifact later deployed elsewhere.

### Modify the artifact for each environment

A single build could be produced and then configuration files or other contents
inside the artifact could be modified before deployment to each environment.

This was rejected because modifying the package changes the artifact that was
originally tested. The original artifact hash and provenance would no longer
identify the deployed bytes.

### Promote the same immutable artifact

A single artifact can be built and tested once, then promoted unchanged through
successive environments while environment-specific configuration is supplied
externally.

This approach was selected because it provides the strongest traceability
between source, verification, and deployed software.

## Consequences

### Positive

- The exact artifact that passed CI can be identified later.
- Source code can be traced from the artifact using the Git commit SHA.
- The SHA-256 hash can identify whether the artifact contents have changed.
- Failed restore, compilation, or test stages cannot produce a releasable
  artifact.
- Promotion between environments does not introduce differences caused by
  rebuilding.
- Environment-specific configuration remains separate from application build
  output.
- Build provenance is available without relying exclusively on CI history.

### Negative

- Changes to application contents require a new build and a new artifact.
- Even small fixes cannot be applied directly to an existing published package.
- Self-contained publishing produces a larger artifact than a
  framework-dependent deployment.
- The current artifact targets `win-x64`, making it platform-specific.
- Updating the bundled .NET runtime requires rebuilding and republishing the
  application.

## Result

A published artifact represents a specific, verified output of a specific
source revision. If its contents change, it is a new artifact and must receive
a new identity and pass the CI process again.