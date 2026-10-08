# Week 8 Lab: Environment Certification Service

Weeks 1–7 established an identified build artifact, artifact promotion, independent readiness validation, architecture decisions, observable policy issuance, failure-mode analysis, and reliability objectives.

Week 8 brings those capabilities together around one question:

> Can the same validator establish whether an environment is usable, regardless of how its application was deployed?

Build a small Environment Certification Service, collect evidence from the resulting environment, and preserve the result as a historical record. Follow the Week 5 pattern: reason first, build a small increment, inspect the evidence, revise it, and then reflect. This walkthrough provides starter material and review criteria; implementation decisions and investigation answers remain your work.

**Workbook alignment:** Technical Leadership Study Workbook, Week 8, printed pages **25–27**. Suggested time: **7–8 hours**. The final appendix maps the workbook requirements to exercises and reader-produced evidence.

## Learning outcomes

By the end of this lab, you should be able to:

- Design an independent environment certification model.
- Create health checks spanning application, data, configuration, and synthetic workflows.
- Emit machine-readable certification results.
- Keep validation loosely coupled to deployment execution.

## Before you begin

This lab assumes you completed Weeks 1–7 in your `technical-leadership-lab` repository. Have these available:

- PolicyService, its test project, and the pinned SDK from `global.json`.
- Locked package dependencies and the existing build/test/package workflow.
- An identified published artifact and its manifest, version, source commit, and SHA-256.
- DEV/QA health and readiness behavior from Week 3.
- Architecture views and ADR-001 through ADR-003 from Week 4.
- Week 5 policy issuance workflow, telemetry, and Observability Contract.
- Week 6 failure-mode analysis and recovery notes.
- Week 7 reliability scorecard and toil backlog.
- A disposable lab database and controllable lab dependency.

Inspect your actual repository first. The suggested project names and routes below are **new scaffolding**, not claims about code that already exists. Adapt them to your conventions and document the changes.

Run experiments in local development or disposable DEV/QA environments. If your runners are ephemeral, save history and telemetry before teardown. A persistent application host, an Azure subscription, and access to a workplace deployment system are not prerequisites.

Week 4 selected R2 for delivery evidence. Reuse it if already implemented. Otherwise, a durable local history directory is sufficient for this prototype; document the divergence from the intended architecture and its limits. A CI upload may preserve run records, but history must remain retrievable across separate certification runs.

**Scope:** Build the certification prototype and ADR-004. A dashboard, production service hosting, deployment-platform migration, and an enterprise schema-comparison engine are optional extensions.

## How to work through the week

| Work block | Suggested time | Result |
| --- | --- | --- |
| Reading and initial certification contract | 100 minutes | Requirements, boundary, and readiness policy |
| Runner, input contract, and first checks | 65 minutes | Independent executable capability |
| Seven checks and synthetic workflow | 105 minutes | Evidence from the actual environment |
| Result history and trigger integration | 70 minutes | Retrievable records and two invocation modes |
| Faults, recovery, and deployment independence | 65 minutes | Verified behavior across deployment paths |
| ADR, knowledge check, reflection, and closeout | 60 minutes | Reviewed portfolio deliverables |

Total: **465 minutes**, approximately 7 hours 45 minutes. This is a planning guide; unfamiliar database setup may require additional time.

Checkpoints occur at larger milestones. Resolve missing evidence before adding infrastructure.

## Required reading and tutorial selections

Use both resources named by the workbook:

1. [Azure Well-Architected — Operational Excellence](https://learn.microsoft.com/en-us/training/modules/azure-well-architected-operational-excellence/) — allow the workbook's **1 hour 22 minutes**. Focus on observability, automation, standards, supply chain, and safe deployments. Apply the principles to your existing environment; Azure hosting is not required.
2. [OpenTelemetry .NET reference](https://opentelemetry.io/docs/languages/dotnet/) — revisit the Week 5 resource, correlation, instrumentation, and export choices relevant to certification.

Record completion in `docs/week-08/README.md`. For each resource, note one design choice it informed and one operational responsibility it exposed. Keep certification records distinct from diagnostic telemetry: telemetry explains a run; a certification record preserves its checks, identity, policy, and outcome.

## Portfolio artifacts

Follow your repository's conventions; these paths are suggested:

| Path | Purpose |
| --- | --- |
| `docs/week-08/README.md` | Setup, readings, run instructions, and artifact index |
| `docs/week-08/certification-contract.md` | Inputs, expectations, check catalog, aggregation, freshness, and ownership |
| `docs/week-08/experiments.md` | Healthy baseline, faults, recovery, and deployment-path comparison |
| `docs/week-08/evidence/` | Sanitized representative JSON records and linked telemetry |
| `docs/week-08/retrospective.md` | Knowledge answers, reflections, and weekly retrospective |
| `docs/adr/ADR-004-validation-independent-of-deployment.md` | **Required ADR:** Validation Is Independent of Deployment Execution |
| `src/EnvironmentCertification/` | **Required working prototype:** independently runnable certification capability |
| `tests/EnvironmentCertification.Tests/` | Meaningful aggregation, failure, and record-contract tests |
| `scripts/certify-environment.ps1` | Shared invocation wrapper, if useful |
| Existing deployment/workflow paths | Thin post-deployment invocation and evidence capture |

Runtime history belongs in your configured history store. Commit selected sanitized evidence, not every generated record. Exclude private target settings and credentials from Git.

---

## Step 1: Define what certification promises

Create the Week 8 documentation directory and a draft contract before implementation.

### A. Identify the decision and boundary

Write brief answers:

1. Who uses the result: a QA approver, deployment pipeline, implementation engineer, or another role?
2. What decision does a PASS enable in your lab?
3. Which application, database, configuration profile, dependency, and business workflow are inside the certification boundary?
4. Which user needs are outside that boundary?
5. What failure from Week 6 would this capability detect? What toil from Week 7 could it reduce?

Distinguish artifact validity, deployment execution, and environment usability. Certification observes the resulting environment; it does not build, copy, migrate, restart, repair, or roll back it.

### B. Separate expected and observed state

Expected values must come from a trusted manifest or reviewed certification profile. Observed values must come from the running application or its actual dependencies.

Do not read a version from the invocation arguments and return it as “observed.” Do not treat a successful deployment log as evidence that the target is reachable. A profile that declares the expected schema cannot also be the evidence that the database contains that schema.

Record an authoritative source for each expectation:

| Expectation | Source to choose | Observation source to implement |
| --- | --- | --- |
| Artifact version and commit | Identified artifact manifest | Metadata from the running application |
| Schema contract | Versioned schema specification | Read-only database inspection |
| Configuration profile | Reviewed, versioned expectations | Effective safe runtime settings or behavior |
| Critical dependency behavior | Contract/profile | Request to the configured lab dependency |
| Synthetic issuance outcome | Workflow specification | Real issuance response and relevant evidence |

A manifest's archive checksum identifies the package. It does not prove the currently running files match that archive unless you implement an independent verification mechanism. State the strength and limits of your version check.

### C. Starter Certification Contract

Save and fill this scaffold incrementally:

```markdown
# PolicyService Environment Certification Contract

- Status: Draft
- Contract/profile version: [value]
- Certification owner: [role]
- Environment owner: [role]
- Decision enabled: [purpose]

## Inputs and Target Resolution
[Environment, expected deployment identity, profile, trigger metadata.]
[How environment maps to trusted target addresses and secret references.]

## Expected Versus Observed State
[Authoritative expectation sources and independent observation routes.]

## Check Catalog
[Seven check IDs, evidence, blocking/advisory policy, timeout, side effects.]

## Overall Result and Caller Policy
[Aggregation, missing checks, timeouts, runner/store failure, exit codes.]

## Synthetic Transaction
[Test data, success criteria, persistence scope, isolation, cleanup.]

## History and Freshness
[Store, record identity, retrieval, retention, concurrency, invalidation.]

## Observability and Ownership
[Correlation, telemetry, diagnosis, escalation, operation of the validator.]

## Verification
[Baseline, failure, recovery, and deployment-independence evidence.]

## Tradeoffs and Deferred Work
[Implemented choices and practical limits.]
```

### D. Choose aggregation and caller policy

Every required check produces **PASS or FAIL**. Use separate reason codes for timeout, exception, mismatch, and inability to execute. An unexecuted check must not silently pass.

One starter policy to evaluate:

- Overall PASS requires all seven required check records and PASS on every blocking check.
- An advisory FAIL remains visible and may coexist with overall PASS; set a separate `degraded` flag and list advisory failures.
- A missing required result, incomplete run, or inability to persist required history prevents usable certification.
- Rejected input or validator unavailability produces an invocation error, never an invented environment PASS.

Decide whether you accept this policy or choose a stricter one. Record your reasoning. For the lab, prefer blocking promotion when usable certification is absent; address possible risk-based exceptions in ADR-004 rather than implementing an undocumented bypass.

**Freshness:** Certification is a point-in-time observation, not a permanent property. Define a maximum age and invalidate applicability when deployment, configuration, schema, target, or profile changes. A caller must match the record to its current target and intended deployment, not just select the last PASS filename.

## Step 2: Build an independent certification runner

For the smallest prototype, create a .NET console application that performs certification and writes JSON. A separately hosted HTTP service is also acceptable. An independently executable capability satisfies this lab's service boundary; document that it is a CLI prototype if you choose that form.

Use your pinned framework, provider, package management, and naming conventions. For a new console project:

```powershell
# Choose the framework matching your pinned SDK and solution.
$targetFramework = "net9.0" # Adjust if your repository has intentionally changed.
dotnet new console --name EnvironmentCertification `
  --output src/EnvironmentCertification --framework $targetFramework
dotnet sln TechnicalLeadershipLab.sln add `
  src/EnvironmentCertification/EnvironmentCertification.csproj
```

Apply the Week 2 lock-file discipline to the new project. Add only necessary, explicitly versioned dependencies; commit project and lock changes together. The certifier may share result contracts, but should not require the deployment tool's internal libraries or import the deployment script to run checks.

### Input contract

Define the equivalent of:

```text
certify --environment QA
        --expected-manifest <manifest-file>
        --profile <profile-file>
        --history-directory <durable-directory>
        --trigger manual|post-deploy
        --deployment-source <provenance-label>
```

This is the interface you will implement, not an existing command. Require environment and expected deployment identity. Validate the manifest and profile before running checks. Keep credentials outside command-line arguments and result records.

Resolve the environment through controlled configuration to application address, database, and dependency target. Record sanitized target identifiers. This prevents accidentally certifying a different environment under a QA label.

Choose and document exit codes, for example `0` for persisted overall PASS, `1` for persisted overall FAIL, and `2` for invocation, execution, or persistence errors that prevent usable certification. Advisory failure handling must agree with your aggregation policy.

### Runner shape

Implement these boundaries using your own types:

```text
Validate input and resolve trusted target/profile
Create unique run ID and record expected identity
Observe target identity at start
For each of the seven configured checks:
    Start timer and diagnostic span
    Execute a bounded probe
    Convert mismatch, timeout, or exception into FAIL with reason
    Retain sanitized observed evidence and duration
Observe target identity again at end
Detect identity change or disagreement across checked instances
Aggregate complete results using the selected policy
Persist one finalized immutable record
Return record location/result and documented exit code
```

Continue safe independent probes after a failure so one broken dependency does not hide the whole picture. If a prerequisite makes another check impossible, include that check as FAIL with an explicit `prerequisite_failed` reason. Never execute a state-changing probe when target identity or test isolation cannot be established.

Use per-check timeouts and an overall deadline. Ensure the underlying database/HTTP operation honors cancellation and provider timeouts; a wrapper timeout alone may leave work running. Start with sequential checks for clear evidence and controlled side effects. Document any retries and retain attempt details so a transient failure does not disappear from history.

**Checkpoint: boundary and initial runner.**

- You can invoke certification without performing deployment.
- Inputs select the intended target and independent expectations.
- The application-reachability check runs against the real target.
- Failure produces a bounded, structured result and documented caller behavior.

## Step 3: Implement all seven checks

Give each check a stable ID. The observations below are starting points; refine them for your actual implementation.

| Required check | Probe to implement | Evidence to retain | False confidence to avoid |
| --- | --- | --- | --- |
| Application reachable | Call the existing liveness/health route and validate the defined response | Sanitized target, HTTP status, duration, response assertion | A listening port alone does not prove application behavior |
| Database reachable | Open the intended lab DB and execute a real bounded read, such as `SELECT 1` | DB target identity, query outcome, duration, safe failure category | An application health response need not establish database access |
| Expected schema | Inspect a small, versioned schema contract in the actual DB | Expected/observed tables, columns, types, or version plus structural assertions | A reachable DB or unchecked migration marker is insufficient |
| Expected configuration | Compare effective safe runtime values/behavior to reviewed expectations | Setting names, safe values or comparison outcomes, profile version | Seeing expected values only in the runner's config proves nothing about the app |
| Expected deployment version | Query the running app's metadata and compare with the manifest | Expected/observed version and commit; artifact reference | Caller-supplied expected version is not an observation |
| Critical dependency | Probe the actual configured lab dependency and assert a minimal required behavior | Target identity, response/assertion, duration, error category | A generic root-page 200 may not prove the needed capability |
| Synthetic policy transaction | Call the actual issuance workflow with isolated valid test input | Request/trace reference, HTTP outcome, result assertions, cleanup outcome | Calling only `/health` is not a business transaction |

### A. Application and database

Reuse Week 3 probes where appropriate, but retain separate named results. A single `/readiness` boolean is not a replacement for all seven records. Record which network vantage point the runner uses. Database access from the runner and from PolicyService can differ; the synthetic workflow should verify the application path too.

### B. Expected schema

Choose a minimal contract supporting issuance. With SQL Server, read metadata such as `INFORMATION_SCHEMA.COLUMNS` or `sys` catalog views and compare selected names/types/nullability to versioned expectations. Use your provider's equivalent if different.

If Week 5 used only `SELECT 1`, add a tiny disposable lab schema and a read-only schema check. Document whether issuance uses that schema. A checked marker table plus at least one structural assertion makes schema drift demonstrable; do not describe it as comprehensive compatibility validation.

Certification **inspects** schema. Migration or repair remains a separate deployment/maintenance action.

### C. Effective configuration and running identity

If the app lacks safe metadata, add a narrowly scoped lab endpoint, such as `/certification/metadata`, that reports build identity and allowlisted non-secret effective settings. Use existing runtime identity from Week 5 and inspect configuration after normal provider precedence is applied.

Expose only what the checks need. Never return connection strings, credentials, arbitrary environment variables, or an entire configuration dump. If settings cannot be exposed safely, use behavior-based assertions or an appropriately restricted observation route.

Build/package a new identified artifact after app changes. Do not change application bytes while retaining the previous artifact identity. Observe identity before and after checks; fail applicability when it changes. Document the limitation of load-balanced or rolling environments where separate requests may reach different instances.

### D. Critical dependency

Reuse the existing external dependency where possible. If it is uncontrollable, use a small local stub with healthy and failed modes; mark the evidence as a lab dependency simulation. Exercise a response the application actually relies on and use a bounded timeout.

Keep the required critical-dependency check blocking when its failure invalidates issuance. For the later advisory experiment, add an optional non-critical dependency/check rather than relabeling a necessary dependency to obtain PASS.

## Step 4: Design the synthetic policy transaction

Reuse the Week 5 issuance route. Send valid, fictitious input that reaches validation, database access, and creation. Define success beyond a 2xx response: assert a non-empty identifier and relevant result fields, and verify persistence only if your implementation promises it.

Document the workflow's exact scope. If creation is synthetic/in-memory after `SELECT 1`, the probe establishes that teaching workflow; it does not prove durable policy issuance. State that limitation in the contract and ADR.

Before automating the probe, complete:

| Question | Decision to make |
| --- | --- |
| Can this operation write data or emit side effects? | List DB writes, messages, emails, billing actions, and audit events actually present |
| How is test data isolated? | Dedicated lab data/tenant and a recognizable synthetic run reference |
| How are repeat runs handled? | Unique run references or intentional idempotency policy |
| How is cleanup performed and verified? | Explicit cleanup steps and retained cleanup result |
| What happens if cleanup fails? | Visible failure/reason and an owner; decide whether it blocks overall PASS |
| When must the probe refuse to execute? | Wrong target, missing isolation, or unsupported environment |

Do not use real customer data. Keep schema/config/version checks read-only. A transaction rollback cannot undo external messages or email; choose isolation that matches the actual side effects.

Capture the synthetic request's trace ID or other verified lookup reference using the Week 5 contract. Tag synthetic activity so Week 7 indicators can include or exclude it deliberately; avoid per-run metric labels with unbounded cardinality.

**Checkpoint: complete check catalog.**

All seven checks execute against real lab targets, each has a defined assertion and timeout, and synthetic success includes the declared creation boundary. Check records expose specific failures and side effects are controlled.

## Step 5: Emit machine-readable results and preserve history

Implement a versioned JSON record. The following is a **shape example**, not an actual PASS record. It shows one check for brevity; every finalized real run must contain all seven required check IDs.

```json
{
  "recordSchemaVersion": "1",
  "certificationRunId": "<unique-run-id>",
  "environment": "QA",
  "targetId": "<sanitized-resolved-target>",
  "expectedDeployment": {
    "version": "<manifest-version>",
    "sourceCommit": "<manifest-commit>",
    "artifactSha256": "<manifest-sha256>"
  },
  "observedDeploymentStart": {
    "version": "<runtime-version>",
    "sourceCommit": "<runtime-commit>"
  },
  "observedDeploymentEnd": {
    "version": "<runtime-version>",
    "sourceCommit": "<runtime-commit>"
  },
  "profileVersion": "<reviewed-profile-version>",
  "validatorVersion": "<validator-build-version>",
  "trigger": "manual",
  "deploymentSource": "simulated-manual",
  "startedAtUtc": "<ISO-8601-UTC>",
  "completedAtUtc": "<ISO-8601-UTC>",
  "durationMs": 0,
  "overallStatus": "<PASS-or-FAIL>",
  "degraded": false,
  "checks": [
    {
      "id": "application-reachable",
      "blocking": true,
      "status": "<PASS-or-FAIL>",
      "durationMs": 0,
      "reasonCode": "<stable-code>",
      "evidence": { "httpStatus": 200 },
      "traceId": "<observed-trace-id-if-present>"
    }
  ]
}
```

Add schema/config observations, attempt details, cleanup results, evidence references, and safe diagnostic summaries as needed. Measure durations with a monotonic timer; use UTC timestamps for correlation. Never substitute prose logs for structured check results.

### History implementation

For a local prototype:

1. Configure a history directory outside runner teardown and build cleanup.
2. Store one uniquely named finalized JSON file per run, organized by environment if useful.
3. Write to a temporary file and atomically finalize on the same filesystem. Avoid overwriting an existing run ID.
4. Keep rejected/partial execution diagnostics separate from complete certification; a partial file must not look like PASS.
5. Provide a documented retrieval command or script that filters by environment and deployment identity and lists timestamps/statuses.
6. Run twice and prove that both records remain after stopping/restarting the certifier.

Local append-only behavior is a prototype convention, not tamper-proof storage. If using R2, preserve separate run keys and describe access, retention, and overwrite protections actually configured.

Make persistence part of successful completion. If writing required history fails, return the documented invocation error and prevent the caller from treating a transient console PASS as usable certification. Retain what diagnostics you can without claiming a saved record exists.

**Checkpoint: result and history.**

A simple JSON consumer can identify all seven checks and the overall outcome. Two distinct runs survive restart and can be retrieved by target and deployment identity. The records show policy and validator versions, not just a green badge.

## Step 6: Trigger manually and after deployment

Create one wrapper or executable entry point used by both modes. Keep deploy-specific code limited to resolving inputs, invoking certification, consuming the outcome, and retaining evidence.

### A. Manual invocation

Document exact prerequisites, configuration, command, expected output location, and exit codes. Run against the already-deployed application without rebuilding or redeploying it.

A PowerShell wrapper around a published validator could follow this shape after you implement the CLI from Step 2:

```powershell
& $certifierExe --environment $environment `
  --expected-manifest $manifestPath `
  --profile $profilePath `
  --history-directory $historyPath `
  --trigger manual `
  --deployment-source simulated-manual
$certificationExitCode = $LASTEXITCODE
if ($certificationExitCode -ne 0) {
    throw "Certification did not produce an acceptable result; code $certificationExitCode."
}
```

Set every variable to a documented real path/value first. This fragment is not a complete deployment script. For a hosted service, implement the equivalent client behavior, including unavailable-service handling.

### B. Post-deployment invocation

Integrate the same wrapper after the existing deployment step while the target is still running. Set `trigger` to `post-deploy` and record deployment provenance. Keep deployment execution outcome and certification outcome separately visible.

Publish/retain failed JSON and diagnostic evidence even when certification blocks the job. Cleanup runs after evidence capture. When testing unavailable targets after a failed deployment, label the deployment failure explicitly; do not misattribute it to an otherwise successful deployment.

A PASS may enable the next promotion/QA action according to your policy. Certification itself should not execute that promotion. Another caller owns the decision.

## Step 7: Prove deployment independence

The workbook names **Azure DevOps deployment, PowerShell deployment, or simulated manual deployment**. Your current project may use GitHub Actions; retain it and test the relevant paths without making an Azure DevOps migration a prerequisite.

Use at least **two genuinely different available deployment paths** to make independence observable:

- Existing GitHub Actions or Azure DevOps deployment, if available.
- Standalone PowerShell deployment of the same identified artifact.
- Simulated manual deployment: manually extract/copy the same package into a disposable target directory and start it with documented environment settings.

Complete all three named workbook paths when available. If Azure DevOps is unavailable, include its thin caller mapping in the notes and label it **designed, not executed**. A provenance label or copied YAML fragment is not proof of an Azure DevOps execution.

For each executed path:

1. Deploy the same identified PolicyService package to the lab target.
2. Run the same validator build, check implementations, and certification profile.
3. Change only target/trigger/provenance inputs when necessary.
4. Save the deployment execution evidence and finalized certification JSON separately.
5. Manually rerun certification without another deployment.
6. Compare observed checks and result semantics across paths.

| Path | Actually executed? | Artifact identity | Validator/profile identity | Deployment result | Certification result | Evidence |
| --- | --- | --- | --- | --- | --- | --- |
| Existing pipeline | To record | To record | To record | To observe | To observe | To link |
| Standalone PowerShell | To record | To record | To record | To observe | To observe | To link |
| Simulated manual | To record | To record | To record | To observe | To observe | To link |
| Azure DevOps, if different/unavailable | Executed or designed only | To record | To record | To label | To label | Run or mapping |

Answer: Which code changed when the deployment mechanism changed? If check logic required rewriting, identify the coupling and refactor before accepting the demonstration.

## Step 8: Exercise failures, recovery, and policy

Capture a healthy baseline first. Use one reversible fault at a time, retain the expected identity, and restore the environment between cases. Give each experiment a run ID and evidence links.

| Case | Reversible setup | Behavior to verify |
| --- | --- | --- |
| Healthy baseline | Correct target, identity, schema, config, and dependencies | All required checks PASS; history persisted |
| Application unreachable | Stop only the disposable application | Reachability FAIL; dependent probes explicitly accounted for; bounded completion |
| Database unavailable | Stop dedicated lab DB or use unavailable endpoint with bounded timeout | DB FAIL and honest schema/synthetic outcomes |
| Schema mismatch | Change one expected column/type in a copied profile or alter disposable schema and restore | Schema FAIL with expected/observed difference |
| Configuration mismatch | Change one effective safe app setting and restart | Configuration FAIL; unchanged artifact identity if bytes did not change |
| Deployment version mismatch | Supply a deliberately different expected manifest | Version FAIL; expected and observed values remain distinct |
| Critical dependency unavailable | Switch controllable dependency to failed/unavailable mode | Critical dependency FAIL; correct blocking policy |
| Synthetic failure | Use a controllable issuance failure with otherwise healthy probes | Synthetic FAIL despite health PASS; no invented creation success |
| Non-critical degradation | Fail an additional advisory dependency | Visible advisory FAIL; overall result follows declared policy |
| Certifier/history unavailable | Fail invocation or use a controlled unwritable/unavailable history destination | Caller detects absent usable certification and applies declared policy |
| Recovery | Restore normal settings/processes | New PASS record; failed historical record still retrievable |

For schema mismatch, identify whether you tested the comparison using a changed expectation or observed actual target drift; do not confuse the two. For synthetic failure, ensure valid input reaches the intended workflow boundary. Invalid input rejected before database access proves a different case.

Select a case where **deployment execution succeeded but certification failed**. It may be wrong effective configuration, schema drift, or dependency failure after a completed deployment. Preserve both results so another engineer can explain why they agree about different things.

For each experiment record:

- Artifact, environment, profile, validator, and run identity.
- Fault/setup and bounded restoration procedure.
- Predicted per-check and overall outcomes before running.
- Actual JSON/evidence references and caller behavior.
- Detection time, relevant telemetry, and unexpected gaps.
- Recovery observation and cleanup result.

Use Week 5 telemetry to follow a failed synthetic request to its component, deployment, and duration. Distinguish the certification's probe time from full request time and from the user's Week 7 SLI window.

### Revise from evidence

Create a gap table, make the smallest useful correction, and rerun affected cases. Build a new identified artifact if app code changes, or a new validator/profile version if those change. Preserve before/after records.

Meaningful tests should verify aggregation with advisory/blocking failures, missing required checks, timeout/exception conversion, expected-versus-observed mismatch, and history persistence failure. Integration evidence verifies real probes; a unit test returning seven hard-coded PASS results does not.

**Checkpoint: verified prototype.**

Another engineer can run the healthy path, reproduce a deployment-success/certification-failure case, retrieve history, and explain advisory and validator-unavailable behavior. Every required check has been exercised with a discriminating failure or mismatch, and recovery is observed.

## Step 9: Write ADR-004

Create **ADR-004: Validation Is Independent of Deployment Execution**. Week 4's ADR-002 already established independent readiness validation. Reference and extend that decision: explain what the certification model adds, including its interface, cross-mechanism reuse, per-check evidence, history, and caller policy. State whether ADR-002 remains in force or is superseded in part; do not leave conflicting decisions.

Use your existing ADR format. Starter sections:

```markdown
# ADR-004: Validation Is Independent of Deployment Execution

- Status: [Proposed/Accepted]
- Date: YYYY-MM-DD
- Related: ADR-002; ADR-003

## Context
[Problem, user decision, current constraints, evidence from experiments.]

## Decision
[Independent boundary, stable inputs/results, trigger model, history,
 blocking/advisory policy, and unavailable-certification behavior.]

## Alternatives Considered
[Deploy-tool-specific validation; app readiness only;
 independent validator; other justified alternative.]

## Consequences
[Benefits, operating cost, credentials/network access, history ownership,
 freshness, synthetic side effects, and limitations.]

## Verification and Rollout
[Executed deployment paths, failure evidence, acceptance criteria,
 owner, incremental adoption, and deferred work.]
```

Reasoning prompts:

- Why is extending only the deployment script insufficient for your portability goal?
- Which application probes are reusable, and why is independent orchestration still valuable?
- What does the runner need access to, and who operates it when it fails?
- What happens during a deployment-platform migration?
- Which checks are blocking, which are advisory, and who may change that policy?
- What tradeoff did you make between diagnostic completeness, runtime, cost, and side effects?
- What evidence makes the decision more than an architectural preference?

Revise the relevant Week 4 architecture view to include the certifier and history boundary. Preserve the difference between the application under test, deployment execution, validation, and result consumption. A new diagram is optional if an existing view can communicate the final design clearly.

## Step 10: Review the contract and prototype

Replace placeholders with actual decisions, commands, and evidence.

Review in three passes:

**Identity and scope:** Expected and observed identity are distinct; the target is correctly resolved; required checks are complete; point-in-time freshness and concurrent-change limits are explicit.

**Decision and diagnosis:** All statuses are machine-readable; aggregation and caller behavior agree; a failed synthetic request has usable evidence; advisory failures remain visible; history failure cannot masquerade as successful certification.

**Operation and ownership:** Timeouts are bounded; synthetic side effects and cleanup are controlled; history survives separate runs; the certifier has telemetry and an owner; reuse across deployment paths is demonstrated without rewriting checks.

Ask a reviewer, coach, or independent self-review to select one failed record and explain its deployment identity, failed condition, overall decision, next action, and limits. Record the result and mark the contract Reviewed when the acceptance checks pass.

---

## Step 11: Knowledge check

Answer all six workbook questions **before consulting its answer key**. Save your original answers in `retrospective.md`.

1. If a deployment system reports SUCCESS and certification reports FAIL, can both be correct? Explain.
2. Why is validation independence valuable during a deployment-platform migration?
3. What makes a synthetic transaction different from a simple health endpoint?
4. What information should a certification record retain?
5. What should happen if the certification system itself is unavailable?
6. Which checks should be blocking versus advisory?

After answering, compare with the workbook's Week 8 answer key on printed pages 39–40. Record corrections in your own words and connect each correction to a decision or observed record. Do not replace your explanation with copied definitions.

## Step 12: Reflection prompts

Use all three workbook prompts:

**R1. What is the minimum evidence required to call an environment READY?**

Identify the intended user action, required checks, identity match, evidence completeness, and acceptable age. Explain one thing your current PASS does not prove and distinguish point-in-time certification from meeting a long-term SLO.

**R2. Which checks are destructive or state-changing?**

Name the actual side effects in your implementation. Explain isolation, repetition, cleanup, and what happens when cleanup fails. If creation is only in-memory, state that scope while considering how the design changes for durable issuance.

**R3. How would certification behave if one non-critical dependency is degraded?**

Use your advisory experiment. Cite its check result, overall status, degraded indicator, and caller response. Explain why accepting that degradation does or does not preserve the intended use, and who owns the policy.

## Step 13: Weekly retrospective

Answer the workbook's four questions:

1. **What became clearer this week?**
2. **What was harder than expected?**
3. **What would I do differently in a production system?**
4. **What artifact from this week best demonstrates growth?**

Include an observation from deployment independence or a misleading initial check that you improved. Keep the retrospective separate from the operating contract.

## Step 14: Repository closeout

Update the root README's roadmap and documentation index with links to the Week 8 overview, run instructions, and ADR-004. Verify relative links from their actual locations.

### Completion checklist

The first six items reproduce the workbook's completion requirements. The remaining items verify the hands-on work and learning outcomes.

- [ ] I completed the required reading/tutorial selections.
- [ ] The lab runs end-to-end and I can explain the result.
- [ ] The required deliverable is committed to the repository.
- [ ] I can explain at least one tradeoff I made.
- [ ] I completed the knowledge check without consulting the answer key.
- [ ] I wrote a brief weekly retrospective.
- [ ] The working certification prototype accepts environment and deployment identity.
- [ ] Application reachability, database reachability, expected schema, expected configuration, expected deployment version, critical dependency, and synthetic policy transaction are implemented.
- [ ] Each required check emits machine-readable PASS/FAIL with evidence and duration.
- [ ] Overall status follows documented blocking/advisory policy and accounts for missing checks.
- [ ] Expected values are compared with independent observations of the actual target.
- [ ] Unique finalized history records survive separate runs and can be retrieved.
- [ ] Manual certification works without deployment.
- [ ] Post-deployment certification uses the same validator and records evidence on failure.
- [ ] The same certification is demonstrated across available deployment paths; unexecuted Azure DevOps integration is labeled honestly.
- [ ] Deployment SUCCESS with certification FAIL is demonstrated and explained.
- [ ] Every required check has failure/mismatch evidence, and the environment is restored.
- [ ] Advisory degradation and unavailable-certification/history behavior are verified.
- [ ] Synthetic isolation, scope, side effects, and cleanup are documented and verified.
- [ ] Certification telemetry is correlated with the Week 5 workflow evidence.
- [ ] ADR-004 records the decision, alternatives, consequences, and its relationship to ADR-002/003.
- [ ] Freshness, ownership, and at least one practical limitation are explicit.
- [ ] All six knowledge questions, three reflection prompts, and four retrospective questions are answered.
- [ ] Locked restore, build, relevant tests, and diff checks pass after implementation changes.
- [ ] Saved evidence and staged changes contain no secrets or real customer data.

Review the actual solution and changes:

```powershell
dotnet restore TechnicalLeadershipLab.sln --locked-mode
dotnet build TechnicalLeadershipLab.sln --configuration Release --no-restore
dotnet test TechnicalLeadershipLab.sln --configuration Release --no-build
git status
git diff --check
git diff
```

Stage the intended code, package/lock changes, documentation, representative evidence, and trigger integration deliberately. Review `git diff --cached` before committing. Exclude private profiles, credentials, temporary faults, and unrelated work.

```powershell
# After staging and reviewing the intended files:
git commit -m "Complete week 8 environment certification lab"
git push
```

If you maintain weekly milestone tags, add `week-08` only after confirming it does not already exist. A tag is optional; the working prototype and ADR-004 must be committed.

**Week 8 is complete when another engineer can run the same certification independently of deployment, inspect the evidence behind its decision, retrieve earlier results, and explain the behavior when the environment or the certifier fails.**

---

## Appendix: Workbook coverage review

This walkthrough was checked against **Week 8, printed pages 25–27**, and its six knowledge questions. This table verifies instructional coverage; the reader must still perform the exercises and produce evidence.

| Workbook requirement | Walkthrough coverage | Evidence the reader must produce |
| --- | --- | --- |
| Design independent environment certification | Steps 1–2, 7, 9 | Contract, independent invocation, ADR-004 |
| Checks span application, data, configuration, synthetic workflows | Steps 3–4 | Real probe results and workflow assertions |
| Machine-readable certification results | Step 5 | Complete JSON record and consumer behavior |
| Validation loosely coupled to deployment | Steps 2, 6–7, 9 | Shared validator/checks and thin callers |
| Operational Excellence resource, 1 hr 22 min | Required reading section | Reading record and applied observation |
| OpenTelemetry .NET reference; reuse Week 5 model | Reading section; Steps 4, 8, 10 | Correlated certification and synthetic telemetry |
| Hands-on 1: service accepts environment and deployment identity | Steps 1–2 | Validated input contract and actual invocation |
| Hands-on 2: application reachable | Steps 3, 8 | Healthy and unreachable evidence |
| Hands-on 2: database reachable | Steps 3, 8 | Real query and failed reachability evidence |
| Hands-on 2: expected schema | Steps 3, 8 | Expected/observed schema and mismatch |
| Hands-on 2: expected configuration | Steps 3, 8 | Effective runtime comparison and mismatch |
| Hands-on 2: expected deployment version | Steps 1, 3, 8 | Manifest/runtime comparison and mismatch |
| Hands-on 2: critical dependency | Steps 3, 8 | Capability assertion and failure evidence |
| Hands-on 2: synthetic policy transaction | Steps 4, 8 | Real workflow result, failure, and side-effect policy |
| Hands-on 3: per-check PASS/FAIL plus overall status | Steps 1, 5, 8 | Seven check records, aggregation, and policy cases |
| Hands-on 4: persist history | Step 5 | Two retrievable records surviving restart |
| Hands-on 5: manual and after-deployment triggers | Step 6 | Both invocation modes with saved results |
| Hands-on 6: same certification after ADO, PowerShell, or simulated manual deployment | Step 7 | Executed path comparison; unavailable paths explicitly labeled |
| Deliverable: working certification prototype | Steps 2–8, 10, 14 | Runnable committed capability and end-to-end evidence |
| Deliverable: ADR-004, Validation Is Independent of Deployment Execution | Step 9 | Committed decision with evidence and consequences |
| Completion: readings/tutorials | Reading section; Step 14 | Completion record |
| Completion: end-to-end run and explanation | Steps 6–8, 10, 14 | Healthy, failure, recovery, and reviewer explanation |
| Completion: deliverable committed | Step 14 | Repository commit containing prototype and ADR |
| Completion: explain at least one tradeoff | Steps 1, 5, 9–10, 14 | Decision and operational consequence |
| Completion: knowledge check before answer key | Step 11 | Six original answers and subsequent corrections |
| Completion: brief weekly retrospective | Step 13 | Four responses |
| Reflection: minimum evidence for READY | Step 12, R1 | Decision boundary, identity, completeness, freshness |
| Reflection: destructive/state-changing checks | Steps 4, 12, R2 | Side-effect inventory and cleanup evidence |
| Reflection: non-critical degraded dependency | Steps 8, 12, R3 | Advisory result, overall status, and caller action |
| Knowledge questions 1–6 | Step 11 | All six prompts preserved without supplied answers |
| Weekly retrospective: clearer, harder, production changes, growth artifact | Step 13 | All four prompts preserved |

**Coverage review:** Every Week 8 requirement is addressed. The local CLI, minimal schema contract, durable local history option, and alternative deployment paths make the prototype achievable without workplace-system access. They carry explicit limits; simulated or designed integrations must not be presented as executed platform validation.
