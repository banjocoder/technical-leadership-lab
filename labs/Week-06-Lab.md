# Week 6 Lab: Reliability Engineering and Failure Modes

Weeks 1-5 established a PolicyService delivery process, immutable artifacts, independent readiness validation, architecture decisions, and telemetry that explains failed requests.

Week 6 asks a different question:

> When a component fails, what happens to the user's workflow, how quickly can someone detect it, and can another engineer restore useful service?

You will create a **Failure Mode and Effects Analysis (FMEA)**, predict six failures, inject them in a controlled environment, compare predictions with evidence, and improve detection. You will then produce a one-page **Reliability Improvements** backlog ranked by risk. Follow the Week 5 pattern: reason first, make a small change, inspect evidence, revise, and reflect. This walkthrough supplies scaffolds and review criteria; your predictions, implementation decisions, and findings remain your work.

**Workbook alignment:** Technical Leadership Study Workbook, Week 6, pages 19-21. Suggested time: **5-6 hours**. The appendix maps this walkthrough to the workbook requirements.

## Learning outcomes

By the end of this lab, you should be able to:

- Use failure-mode analysis to reason systematically.
- Differentiate availability, resilience, and recovery.
- Design detection and recovery alongside normal functionality.
- Practice failure injection in a controlled environment.

## Before you begin

This lab assumes you completed Weeks 1-5 in your own `technical-leadership-lab` repository. Have these available:

- PolicyService, its test project, the pinned SDK, and locked dependencies.
- An identified published artifact and its manifest.
- The health/readiness checks and deployment configuration from Week 3.
- Your Week 4 architecture views and ADRs.
- The Week 5 issuance workflow, Observability Contract, and exported logs, metrics, and traces.
- A disposable database, its setup/reset script, and known-good configuration.
- A way to start and stop the lab application and a local dependency stub.

Work with your actual routes, providers, and paths. Do not assume your API persists policies or already calls an external service. Step 3 supplies minimal extensions where needed.

Use local development or a disposable DEV/QA environment. Local experiments with an identified published artifact are sufficient; a long-lived hosted environment is unnecessary. Capture evidence before ephemeral runners disappear. No unhealthy deployment needs to be promoted to complete this lab.

Run one fault at a time. Keep a known-good configuration outside version control and record the reset action before injecting each fault. Stop an experiment if it affects anything outside the disposable environment, prevents your documented reset, or exceeds your chosen time limit.

**Scope:** This week requires analysis, controlled experiments, detection improvements, recovery verification, and a risk-ranked backlog. Implement one small partial-failure behavior where feasible. A production failover platform, centralized alerts, Cloudflare R2, and an SLO program are optional extensions. Week 7 develops SLI/SLO and error-budget concepts further.

## How to work through the week

| Work block | Suggested time | Result |
| --- | --- | --- |
| Required reading and workflow boundaries | 100 minutes | Reading notes and reliability vocabulary |
| Draft FMEA, experiment setup, and predictions | 45 minutes | Six analyzed failures and reset plans |
| Five required failure experiments | 90 minutes | Prediction/observation comparisons and recovery evidence |
| Partial failure and graceful degradation | 40 minutes | Explicit reduced-function behavior and verification |
| Detection revision, risk ranking, and review | 40 minutes | Reviewed FMEA and one-page backlog |
| Knowledge check, reflection, and closeout | 25 minutes | Completed portfolio record |

This is a planning guide. Additional schema/dependency setup may take longer. Checkpoints occur at larger milestones rather than after every small edit.

## Required reading and tutorial selections

Use the three resources named by the workbook:

1. [Azure Well-Architected Framework - Reliability](https://learn.microsoft.com/en-us/training/modules/azure-well-architected-reliability/) - allow the workbook's **1 hour 29 minutes** for the structured module. Record notes about business requirements, resilience, recovery, operations, and simplicity.
2. [Reliability quick links](https://learn.microsoft.com/en-us/azure/well-architected/reliability/) - use the hub to find failure-mode analysis, testing, recovery, and reliability targets. Select the guidance relevant to your workflow.
3. [Reliability design patterns](https://learn.microsoft.com/en-us/azure/well-architected/reliability/design-patterns) - consult this catalog **after your initial failure analysis**. Record one candidate pattern and why it fits or does not fit an observed problem.

Save the reading record in `docs/week-06/README.md`: resource, section completed, date, and one implication for PolicyService. The Azure material provides a reasoning framework; you can complete the experiments locally without provisioning Azure resources.

## Portfolio artifacts

Follow your repository's existing conventions; these paths are suggested:

| Path | Purpose |
| --- | --- |
| `docs/week-06/README.md` | Reading record, setup, run/reset instructions, and artifact index |
| `docs/week-06/fmea.md` | **Required deliverable:** Failure Mode and Effects Analysis |
| `docs/week-06/reliability-improvements.md` | **Required deliverable:** one-page backlog ranked by risk |
| `docs/week-06/experiments.md` | Predictions, actual behavior, detection changes, and recovery results |
| `docs/week-06/evidence/` | Sanitized probe records, telemetry, request results, and startup/process evidence |
| `docs/week-06/retrospective.md` | Knowledge-check answers, reflections, and weekly retrospective |
| Existing source/test/script paths | Minimal schema fixture, dependency stub, detection or degradation changes |
| `docs/week-05/observability-contract.md` | Updated contract when Week 6 changes promised telemetry |

Screenshots can supplement evidence. Keep searchable exports or text records so a reviewer can investigate without relying on pictures alone.

---

## Step 1: Define useful service and its failure boundaries

Describe the current issuance workflow in `README.md`. Reuse Week 5's actual boundaries: HTTP request, validation, database query, and declared creation result. Add external dependencies only where they really exist.

### A. Identify what users need

Complete this table before choosing a reliability pattern:

| Capability | Required for useful service? Why? | Dependencies | Acceptable reduced behavior | Unacceptable result |
| --- | --- | --- | --- | --- |
| Accept and validate an issuance request | To decide | To identify | To define | To define |
| Check required database state | To decide | To identify | To define | To define |
| Create the declared policy result | To decide | To identify | To define | To define |
| Optional workflow step, if present | To decide | To identify | To define | To define |

If creation is synthetic/in-memory, state that explicitly. Do not claim that the lab proves durable issuance or recovery of persisted policies.

### B. Separate three reliability questions

Write your own short explanations, using one example from the workflow:

- **Availability:** Over what period, and for which capability, can users obtain useful service?
- **Resilience:** What useful behavior can the system preserve while a component is malfunctioning?
- **Recovery:** How does the system return to its normal operating state, and how will you verify that return?

Prompts:

- Can `/health` respond successfully while issuance fails?
- Can a service remain partially useful without satisfying every user need?
- What state, dependency, configuration, or human action might restoration require?

Keep these explanations provisional. Revisit them after observing failures.

## Step 2: Draft the FMEA before injecting faults

Create `fmea.md` with at least the workbook's five columns: **Component, Failure, Effect, Detection, Recovery**. Give each failure a stable ID so evidence and backlog items can refer to it.

Use this scaffold. The rows name required failures; they deliberately leave your analysis unfinished.

```markdown
# PolicyService Failure Mode and Effects Analysis

- Status: Draft
- Date: YYYY-MM-DD
- Workflow and environment: [scope]
- Application artifact/source revision: [identity]
- Analyst/reviewer: [name or role]
- Assumptions and limitations: [scope of evidence]

## Failure Modes

| ID | Component | Failure | Effect | Detection | Recovery |
| --- | --- | --- | --- | --- | --- |
| F01 | API host/process | API process down | [user and downstream effects] | [signal, observer, cadence] | [action, owner, verification] |
| F02 | Database | Database unavailable | [effects] | [detection] | [recovery] |
| F03 | Application/database contract | Schema mismatch | [effects] | [detection] | [recovery] |
| F04 | Configuration | Invalid configuration | [effects] | [detection] | [recovery] |
| F05 | External dependency/client | Dependency timeout | [effects] | [detection] | [recovery] |
| F06 | [partial-workflow component] | [specific partial failure] | [affected and unaffected capabilities] | [detection] | [recovery] |

## Risk Ranking

[Scale, scores, rationale, uncertainty, and links to experiment evidence.]

## Cross-cutting Observations

[Common dependencies, detection blind spots, recovery ownership, and tradeoffs.]

## Verification

[Links to predictions, observations, improved detection, and recovery evidence.]
```

For each row, explain effects on the user, dependent components, and correctness. “The API errors” is too vague: which operation fails, what remains available, and could a caller wrongly believe issuance succeeded?

In Detection, name a real signal, the observer, and how often it is evaluated. An application log is insufficient when the application is stopped. In Recovery, name who acts, what they need, and the behavior that demonstrates success. “Restart it” is incomplete without a verification step.

You may add columns or linked detail for prediction, owner, risk, or evidence, but preserve the five required columns.

## Step 3: Prepare a repeatable baseline and test fixtures

Use the smallest setup that can exercise all six failures honestly.

### A. Record the baseline

Start the identified application under known-good configuration. Save:

- Artifact version, source revision, environment, and process launch command.
- A valid synthetic issuance request and its expected response.
- `/health` and `/readiness` responses, including per-check details where available.
- A successful trace and related logs/metric changes.
- Exact application, database, and dependency start/stop/reset instructions.

Use the same valid request shape for the required failure experiments so invalid input does not bypass the failed component. Use unique synthetic references if your persistence model requires them.

### B. Make schema mismatch testable

If issuance only executes `SELECT 1`, it cannot reveal a table/column mismatch. Add a tiny disposable schema fixture and a query that actually depends on it. For example, create a lab-only product table, seed a synthetic product, and have the issuance workflow query a required column before creation.

Use your actual provider and naming conventions. Save scripts that create, seed, deliberately alter, and reset the fixture. Keep the query parameterized where it accepts input. Do not alter a shared database or unrelated tables.

The baseline must prove the schema-dependent query succeeds. During F03, change the expected table/column in the fixture while leaving connectivity intact. A deliberately invalid connection string tests another failure, not a schema mismatch.

### C. Make a real dependency timeout testable

Reuse an external call in the business workflow if it exists. Otherwise add a small local HTTP stub for a required teaching operation, such as retrieving a synthetic eligibility decision before creation. Keep this scope explicit: it is a lab dependency, not a real underwriting service.

Give the stub normal and delayed-response modes. Configure an explicit client timeout and propagate request cancellation. Choose concrete lab values; for example, a normal response under one second, a two-second client timeout, and a five-second delayed response. Record the values you actually use.

During F05, the stub must accept the connection and delay long enough for the client deadline to expire. A closed port produces a connection failure and does not establish this timeout behavior. Capture both the caller outcome and evidence that the stub accepted the request.

Avoid an unbounded wait. Do not add retries before measuring the first timeout. If you later choose retries, reason about duplicate creation and the total elapsed-time budget.

### D. Prepare external observation

Run a probe from a separate terminal/process so it continues when PolicyService stops. Poll `/health` and `/readiness` at a documented interval, such as two seconds, with a per-request timeout. Save UTC time, route, HTTP status or transport-error category, and elapsed time.

Use your existing tooling or a small script. This PowerShell 7 pattern illustrates one observation; wrap it in your own bounded polling loop and save the results:

```powershell
# Set these to your actual local service address and evidence destination.
$baseUri = 'http://localhost:5000'
$probeTimer = [System.Diagnostics.Stopwatch]::StartNew()
$status = $null
$failureType = $null

try {
    $response = Invoke-WebRequest -Uri "$baseUri/health" `
        -TimeoutSec 2 -SkipHttpErrorCheck
    $status = [int]$response.StatusCode
}
catch {
    # HTTP error responses and transport errors are different observations.
    $failureType = $_.Exception.GetType().Name
}
finally {
    $probeTimer.Stop()
}

[pscustomobject]@{
    observedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    route = '/health'
    statusCode = $status
    transportErrorType = $failureType
    durationMs = $probeTimer.Elapsed.TotalMilliseconds
} | ConvertTo-Json -Compress
```

`-SkipHttpErrorCheck` requires PowerShell 7. Adapt for your installed version or use another HTTP client. This probe is experiment evidence; it does not create a production alerting service. Retain response bodies separately when per-check detail is needed, after reviewing them for sensitive values.

### E. Verify ordinary checks

After intentional source/package changes, run your existing checks with actual paths:

```powershell
dotnet restore TechnicalLeadershipLab.sln --locked-mode
dotnet build TechnicalLeadershipLab.sln --configuration Release --no-restore
dotnet test TechnicalLeadershipLab.sln --configuration Release --no-build
```

If adding a package, select an explicit compatible version, deliberately update the lock file, and then verify locked restore. Publish a new identified artifact after changing application code. Reuse that artifact for configuration/dependency experiments; record configuration changes separately.

**Checkpoint: analysis and baseline.**

- All six FMEA rows have initial effects, detection, and recovery analysis.
- The healthy issuance path, schema query, and normal stub call work.
- External probes continue independently of the API process.
- Reset instructions are ready, and baseline evidence is saved.
- You know which outcomes represent useful service and which represent incomplete or failed work.

## Step 4: Write all predictions before the first fault

Create one experiment record for F01-F06 in `experiments.md`. Fill the prediction and setup sections for all six before injecting the first fault. For F06, select the partial failure described in Step 6 now.

```markdown
## Fxx - [Failure name]

### Before Injection

- Artifact, environment, and configuration identity:
- Healthy preconditions and baseline evidence:
- Exact fault action and affected boundary:
- Maximum experiment duration and stop condition:
- Exact reset action:
- Predicted user/HTTP outcome:
- Predicted health and readiness outcomes:
- Predicted logs, metrics, spans, or external evidence:
- Predicted affected and unaffected capabilities:
- Expected detection delay and recovery effort:
- Prediction recorded at (UTC):

### Observation

- Fault introduced at (UTC):
- First failed user request at (UTC), outcome, and trace ID if available:
- First detection by the chosen observer at (UTC):
- Detection signal and evidence reference:
- Actual health/readiness and user outcomes:
- Actual affected/unaffected capabilities:
- Detection delay and measurement limitations:
- Difference from prediction and explanation:

### Detection Improvement and Repeat

- Gap or ambiguity found:
- Small change and rationale:
- New artifact/configuration identity if applicable:
- Repeated fault result and supporting evidence:
- Before/after detection comparison:

### Recovery

- Action, responsible role, and prerequisite knowledge:
- Recovery started/completed at (UTC):
- Restored probes and business-request evidence:
- State/correctness check, where applicable:
- Remaining risk or undocumented dependency:
```

Do not rewrite an original prediction after seeing the result. Record corrections in the comparison section.

**Timing convention:** Detection delay is the elapsed time from fault introduction to the first qualifying observation by your chosen detector. Also record when you personally noticed it if that is later. Use a consistent clock and report polling/export intervals, uncertainty, and any restart-related gaps. A request duration is not a detection delay. If no detector notices the fault within the bounded window, record **not detected within the observation window** rather than assigning zero delay.

## Step 5: Inject the five required failures

For each experiment: confirm healthy preconditions, inject the single fault, issue the valid business request, inspect probes and telemetry, compare with your prediction, then reset and verify recovery. Save first-run evidence before improving detection in Step 7.

### F01 - API process down

Stop only the identified lab API process while leaving the external observer running. Use the recorded process ID or its dedicated terminal; do not kill all `dotnet` processes.

Investigate:

- Do clients receive an HTTP error response, connection refusal, or a timeout?
- How long does the external probe take to detect the failure?
- Can telemetry emitted by the stopped application tell you it is currently down?
- What happens to in-flight work and buffered telemetry? What remains uncertain?

Restart the same artifact with known-good configuration. Verify health, readiness, and a valid issuance request. A listening port alone does not prove recovery.

### F02 - Database unavailable

Stop the dedicated database or point issuance at an unavailable local database endpoint with bounded connection/command timeouts. Keep the API running and required configuration present. Record any application restart needed to load settings.

Investigate:

- Was a database operation actually attempted?
- What happened to issuance, liveness, and readiness separately?
- Did logs/traces identify the database boundary and a safe error category?
- Did metrics count the failed attempt without recording a successful issue?

Restore the database/settings and repeat issuance. If the application uses connection pooling, verify that recovery actually occurs; do not assume restoring a setting repairs a running process that has not loaded it.

### F03 - Schema mismatch

Use the disposable fixture from Step 3. Alter or remove the table/column expected by the application, with the database otherwise reachable. Save the exact schema change and reset script.

Investigate:

- Does a basic connectivity probe still pass while the business query fails?
- Can the evidence distinguish a schema-contract failure from an unavailable database?
- Does readiness test the needed schema compatibility or merely connectivity?
- What is the compatibility relationship between application version and schema version?

Restore the fixture and verify the actual schema-dependent issuance query. Record what recovery would require if a real migration had already changed data. Reverting application binaries does not automatically revert a database migration. This small fixture experiment does not prove a production migration/rollback strategy.

### F04 - Invalid configuration

Choose a required setting whose validity affects startup or issuance. Examples are an empty required display name or a malformed required dependency URI. Keep artifact identity and unrelated settings valid.

State which configuration rule you violate and when validation is expected: startup, readiness, or request handling. A syntactically valid setting aimed at a stopped dependency belongs to F02/F05 rather than proving invalid-configuration detection.

Investigate:

- Does the process fail at startup, remain alive but NotReady, or fail only on use?
- If startup fails, what evidence exists outside normal runtime telemetry?
- Can a responder identify the setting name and validation rule without seeing a secret value?
- Does the delivery process detect the issue before treating the environment as usable?

Restore the setting and restart/reload as required. Verify startup, readiness, and issuance. Record whether recovery depends on knowing configuration precedence or hidden machine-specific settings.

### F05 - Dependency timeout

Switch the required local stub to delayed-response mode, keeping the database and schema healthy. Verify that the stub accepts the request and delays beyond the configured client deadline.

Investigate:

- What response does the caller receive, and within what total duration?
- Does the evidence distinguish dependency timeout from caller cancellation and connection refusal?
- Is the timed-out operation required for issuance, and does creation correctly stop if it is?
- Does `/health` remain responsive while the request waits?
- Is readiness checking the same dependency boundary? What does a passing result omit?

Restore normal stub behavior and verify a successful issuance request. Confirm that slow outstanding calls do not keep consuming resources indefinitely. If a timeout could occur after a real side effect committed, record the uncertain result and retry risk; the lab stub alone cannot establish exactly-once behavior.

**Checkpoint: five required experiments.**

Each required fault has an original prediction, actual observations, a comparison, detection timing or an explicit blind spot, and verified recovery. Schema mismatch and timeout were exercised at their real boundaries. Preserve missing evidence as a finding rather than guessing expected output.

## Step 6: Add a partial-workflow failure and graceful degradation

The workbook requires **one additional failure that affects only part of the user workflow**, and a graceful-degradation design if possible. Give this its own F06 record; do not reuse F05 under a second label.

### A. Select a genuinely optional capability

Reuse an optional workflow dependency if one exists. Otherwise add a small lab-only enrichment call, such as attaching a friendly product description to an issuance response. Use a separate stub route/mode so it can fail while the required eligibility operation from F05 remains healthy.

For this teaching example, the description is optional and does not determine eligibility, price, coverage, persistence, or whether issuance is valid. Keep those boundaries explicit. If the candidate dependency is essential to correct issuance, choose another partial failure; bypassing it is not a safe reduced mode.

Predict F06 before injecting it. Prove a normal response includes enrichment, then inject a deterministic error on only that optional stub route. Keep the application, database, schema, configuration, and required dependency healthy.

### B. Design the reduced behavior

Complete this decision table before implementing fallback:

| Decision | Your policy and reason |
| --- | --- |
| Capability lost | To define |
| Useful behavior preserved | To define |
| Why the dependency is optional | To justify |
| Reduced response and explicit indication of missing information | To define |
| Correctness/security conditions that must still hold | To define |
| Time/resource bound for optional work | To define |
| Logs, trace outcome, and bounded metric dimension | To define |
| Readiness impact and reason | To define |
| Return to normal after dependency recovery | To define |

Implement the smallest useful behavior where feasible. For example, omit the optional description, return an explicit enrichment status, and emit a correlated warning/failure span while preserving the valid issuance result. Do not invent missing business data or report an essential failed operation as successful.

Keep successful issuance, required-operation failure, and optional degradation distinguishable in telemetry. Update your counting policy so optional enrichment failure does not falsely count as a failed issuance or double-count an attempt. Use a bounded outcome/category, not policy IDs or exception messages as metric labels.

If degradation would violate the selected workflow's correctness, explain why, design an explicit partial capability boundary, and preserve a deliberate failure for the essential operation. The workbook allows degradation **if possible**; a clear infeasibility decision needs evidence. If using the optional-description scaffold, reduced behavior should be feasible and verified.

### C. Verify containment and recovery

Compare normal and degraded responses and traces. Show that the required issuance path still reaches its declared success boundary, the optional failure is visible, and an unaffected capability still works. Restore the optional stub and verify normal enrichment resumes.

A simulated response-only example does not prove durable notification delivery or background retry. If you choose post-issuance notifications instead, explicitly address the committed operation, caller-visible result, and duplicate/retry risks before claiming recovery.

## Step 7: Improve detection and repeat the experiments

The workbook asks you to improve detection after comparing predicted and actual behavior. Create a row for **each of F01-F06**:

| Failure ID | Observed gap or ambiguity | Small detection improvement | Repeated evidence | Before/after result |
| --- | --- | --- | --- | --- |
| F01-F06, one row each | To identify | To implement | To link | To assess |

Possible directions, to evaluate against your own evidence:

- External observation when the API cannot emit telemetry.
- A dependency-specific failure category instead of an undifferentiated server error.
- A schema-aware check instead of a connectivity-only result.
- Configuration validation that reports the invalid setting name before a business request.
- Timeout evidence that separates deadline expiry from other cancellation.
- An explicit degraded outcome instead of silently missing enrichment.

Choose useful changes, not a new monitoring platform. A startup check, a probe, a structured field, or a more precise existing check can be sufficient. If detection already worked, refine clarity, timing, or the responder's lookup instructions and demonstrate the improvement. Explain why it helps.

Publish a new identified artifact for application changes. Repeat all six faults one at a time under the improved setup, restoring healthy behavior between them. You may batch code revisions before these repeats, but retain a distinct evidence record for each fault.

Update the Observability Contract if event fields, outcome definitions, metric semantics, or collection behavior changed. Required-check policy should continue to reflect what useful service actually requires. Do not loosen QA promotion criteria merely to make an experiment appear successful.

**Checkpoint: improved detection and recovery.**

Every FMEA row has evidence of a useful detection refinement and a repeated experiment. The final healthy run succeeds, temporary fault modes are off, and all recovery instructions have been exercised. Detection improvements may leave known limits; document them plainly.

## Step 8: Rank risk and write the one-page improvements backlog

Finalize the FMEA using observed effects, detection delay, and recovery effort. Keep original predictions in the experiment log; the final FMEA should describe what you now know.

### A. Choose a small, explicit ranking method

The workbook requires risk ranking but does not prescribe a scoring formula. Use this starter method or document your alternative:

- **Impact (1-3):** 1 = optional feature loss with clear reduced behavior; 2 = a core capability fails for some users/requests; 3 = broad core-service loss or incorrect/uncertain business results.
- **Likelihood (1-3):** 1 = unusual under the stated operating assumptions; 2 = plausible recurring change/dependency risk; 3 = frequent or repeatedly evidenced in the intended setting.
- **Risk score = Impact x Likelihood.** Use observed detection delay, recovery difficulty, and correctness risk to explain tie-breaks.

Record the intended operating context and confidence behind likelihood. Deliberately injecting a failure once does not measure its real-world frequency. Use assumptions where operational history is unavailable. Correctness/data-loss risks can override numerical order if you explain why.

Score all six failures in the FMEA. Separate the initial risk from residual risk after an implemented mitigation. Do not claim the remaining risk is zero because a local experiment passed.

### B. Create the required one-page backlog

Save `reliability-improvements.md` as a compact standalone document: a brief scope/ranking note, about 5-8 concise rows, and a short tradeoff note. Markdown has no fixed page size; keep it to roughly 400-500 words so it fits approximately one printed page under ordinary formatting. Put detailed analysis in the FMEA and link to it.

```markdown
# Reliability Improvements

Scope: [workflow/environment]. Ranked by [risk method]; assumptions: [brief].
Evidence and detailed scoring: [relative link to FMEA/experiments].

| Rank | Failure/risk | Score and reason | Improvement | Owner role / effort | Acceptance evidence |
| --- | --- | --- | --- | --- | --- |
| 1 | [Fxx and user impact] | [score; tie-break if needed] | [specific change] | [role; S/M/L] | [repeatable behavior that proves value] |
| 2 | [Fxx] | [score; reason] | [change] | [role; effort] | [verification] |

Tradeoff: [benefit, cost/complexity, and accepted limitation].
Next action: [first item and prerequisite].
```

Keep this a backlog of remaining work. Record completed detection improvements in the experiment/FMEA history. Include all six analyzed risks through backlog rows or a compact statement linking already-addressed risks to their evidence; do not manufacture extra work to fill a table.

Each item needs a concrete outcome and proposed responsible role. Avoid assigning commitments to people who have not agreed. Examples of acceptance evidence include an externally detectable outage within a stated lab bound, a schema incompatibility caught before promotion, or verified recovery without undocumented settings. Select your own thresholds and justify them.

### C. Explain at least one tradeoff

Record a choice you actually made: startup rejection versus remaining alive but NotReady, schema-check coverage versus maintenance/cost, optional feature availability versus response completeness, or timeout length versus false failures. State the benefit, consequence, and why it fits this lab. Use the pattern catalog to assess a possible next improvement, not to add every pattern to the service.

## Step 9: Review the deliverables independently

Have a reviewer, coach, or independent self-review select one full outage, one contract/configuration failure, and F06. Supply the FMEA, backlog, runbook, and evidence without verbally revealing the injected cause.

Ask the reviewer to establish:

- What failed and what the user experienced.
- What remained useful and why that result was acceptable or unacceptable.
- Which evidence detected the fault, its timing, and its limitations.
- The recovery action and evidence that useful service returned.
- Why the top backlog item takes priority over another item.

If an answer depends on your memory, add the missing record or runbook step. If recovery relies on an undocumented secret location, configuration precedence, reset script, or permission, record that dependency and propose an owner/action.

Reconcile a Week 4 architecture view if you introduced a real dependency or changed recovery responsibilities. Create/update an ADR only if the consequences merit a durable decision record.

Set the FMEA status to Reviewed after the evidence and instructions support the review. This reviews a lab implementation; it does not certify production resilience.

---

## Step 10: Knowledge check

Answer the workbook's six questions without consulting its answer key. Save your original answers in `retrospective.md` before comparing them with the Week 6 key.

1. What is resilience?
2. How is resilience different from availability?
3. Why is recovery part of architecture?
4. What is graceful degradation?
5. Why should teams intentionally test failure conditions?
6. Why is simplicity a reliability technique?

After answering, record any correction in your own words and connect it to an observed experiment or design choice. Preserve the original answer so your learning is visible.

## Step 11: Reflection prompts

Use all three workbook prompts:

**R1. Which failure surprised you most?**

Compare your original prediction with evidence from that fault. Explain which assumption changed and how that affects the FMEA or backlog.

**R2. Which failure had the longest detection delay?**

Compare measured delays using the timing convention from Step 4. Distinguish fault onset, signal availability, and human recognition. If a fault was undetected, explain the observation-window limit rather than assigning it a measured delay. Identify what the detection revision changed.

**R3. Which recovery depends on undocumented human knowledge?**

Name the hidden prerequisite or judgment, who currently knows it, and what a new engineer would need. Link to the runbook addition or backlog item that reduces the dependency. If you found none, explain how the independent review tested that conclusion and what remains unverified.

## Step 12: Weekly retrospective

Answer the same four questions used in the earlier weeks:

1. **What became clearer this week?**
2. **What was harder than expected?**
3. **What would I do differently in a production system?**
4. **What artifact from this week best demonstrates growth?**

Include an observation from a failed prediction, detection refinement, or recovery drill. Keep the retrospective separate from the FMEA so the analysis remains usable by someone who did not participate in your learning process.

## Step 13: Repository closeout

Update the root README's weekly roadmap and artifact index with links to the Week 6 overview, FMEA, and improvements backlog. Verify relative links from their actual repository locations.

### Completion checklist

The first six items preserve the workbook's completion requirements; the remaining items make the hands-on coverage and review evidence explicit.

- [ ] I completed the required reading/tutorial selections.
- [ ] The lab runs end-to-end and I can explain the result.
- [ ] The required deliverable is committed to the repository.
- [ ] I can explain at least one tradeoff I made.
- [ ] I completed the knowledge check without consulting the answer key.
- [ ] I wrote a brief weekly retrospective.
- [ ] The FMEA includes Component, Failure, Effect, Detection, and Recovery.
- [ ] It includes API process down, database unavailable, schema mismatch, invalid configuration, and dependency timeout.
- [ ] A sixth failure affects only part of the workflow; reduced behavior is designed and tested where feasible, or infeasibility is justified.
- [ ] All six predictions were recorded before the first fault injection.
- [ ] All six failures were injected at their actual boundaries and compared with predictions.
- [ ] Detection was improved for each failure, and repeat evidence shows the effect.
- [ ] Detection delays, blind spots, polling/export limits, and recovery results are documented.
- [ ] Each fault was removed and useful business behavior was verified after recovery.
- [ ] Final evidence and telemetry definitions distinguish success, essential failure, and optional degradation.
- [ ] The one-page Reliability Improvements backlog is ranked by risk with rationale, proposed owners, and acceptance evidence.
- [ ] All six knowledge questions, three reflection prompts, and four retrospective prompts have responses.
- [ ] Locked restore, build, and existing tests pass after source changes.
- [ ] Temporary fault settings are off; evidence contains no credentials, raw connection strings, or real customer data.
- [ ] Documentation links work, and both required deliverables are included in the intended commit.

Review before staging:

```powershell
git status
git diff --check
git diff
```

Stage your actual files deliberately: Week 6 documents, minimal application/test/stub/reset-script changes, applicable package/lock changes, README updates, and revised observability/architecture documents. Review `git diff --cached`; keep private settings and unrelated work out of the commit.

```powershell
# Run after staging the intended files and reviewing the staged diff.
git commit -m "Complete week 6 reliability and failure modes lab"
git push
```

If you maintain weekly milestone tags, add `week-06` after checking that it does not already exist. A tag is optional; the committed FMEA and one-page backlog are required.

**Week 6 is complete when another engineer can use your analysis and evidence to explain six failures, identify what remains useful, follow verified recovery instructions, and understand which remaining reliability improvement deserves attention first.**

---

## Appendix: Workbook coverage review

This walkthrough was checked against the supplied workbook's **Week 6 pages 19-21**. This table checks coverage of the instructions; it does not certify that a reader has completed the exercises.

| Workbook requirement | Walkthrough coverage | Evidence the reader must produce |
| --- | --- | --- |
| Use failure-mode analysis systematically | Steps 2, 4, 8 | FMEA, preserved predictions, observed revisions |
| Differentiate availability, resilience, recovery | Steps 1, 5-6, 10 | Own-word distinctions tied to capability and failure evidence |
| Design detection and recovery alongside functionality | Steps 2-4, 7, 9 | Signals, observers, timing, run/reset instructions, review |
| Controlled failure injection | Steps 3-7 | Disposable scope, single faults, bounded execution, verified reset |
| Reliability training module, 1 hr 29 min | Required reading section | Completion notes and implications |
| Reliability quick links reference | Required reading section | Selected analysis/testing/recovery guidance notes |
| Patterns reference after own analysis | Required reading section; Step 8 | Candidate pattern and fit/tradeoff rationale |
| Lab 1: FMEA with Component, Failure, Effect, Detection, Recovery | Step 2 | Required five-column table |
| Lab 2: five named failure modes | Steps 2, 5 | F01-F05 analysis and actual fault evidence |
| Lab 3: predict each failure before injection | Step 4 | Timestamped original predictions for F01-F06 |
| Lab 4: inject, observe, compare, improve detection | Steps 5-7 | Per-fault comparison, implemented refinement, repeat evidence |
| Lab 5: additional partial-workflow failure; degradation if possible | Steps 4, 6-7 | F06 prediction, containment, reduced-mode design/test or justified infeasibility |
| Required FMEA deliverable | Steps 2, 8-9, 13 | Reviewed analysis committed to repository |
| Required one-page Reliability Improvements backlog ranked by risk | Steps 8-9, 13 | Compact backlog, ranking method, rationale, acceptance evidence |
| Completion: readings/tutorials complete | Reading section; Step 13 | Reading record and checklist |
| Completion: end-to-end run and explanation | Steps 3-9, 13 | Baseline, faults, revisions, recovery, independent review |
| Completion: required deliverable committed | Step 13 | Commit containing both required documents |
| Completion: explain at least one tradeoff | Steps 6, 8, 13 | Implemented choice and its consequence |
| Completion: knowledge check without answer key | Step 10 | Six original answers before comparison |
| Completion: brief weekly retrospective | Step 12 | Four retrospective responses |
| Reflection: most surprising failure | Step 11, R1 | Prediction/evidence comparison |
| Reflection: longest detection delay | Step 11, R2 | Comparable timings and measurement limits |
| Reflection: recovery relying on undocumented human knowledge | Steps 9, 11 R3 | Hidden-prerequisite analysis and follow-up |
| Knowledge: resilience | Step 10, Q1 | Reader's answer |
| Knowledge: resilience versus availability | Step 10, Q2 | Reader's answer |
| Knowledge: recovery as architecture | Step 10, Q3 | Reader's answer |
| Knowledge: graceful degradation | Step 10, Q4 | Reader's answer |
| Knowledge: intentional failure testing | Step 10, Q5 | Reader's answer |
| Knowledge: simplicity | Step 10, Q6 | Reader's answer |
| Retrospective: clearer, harder, production differences, growth artifact | Step 12 | All four prompts answered |

**Review outcome:** Every Week 6 requirement from pages 19-21 has a corresponding exercise or closeout item. Schema fixtures, local stubs, external probes, detection timing, and recovery checks make the required experiments observable and repeatable. Additional production infrastructure remains optional.
