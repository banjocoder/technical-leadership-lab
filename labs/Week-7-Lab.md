# Week 7 Lab: SLI, SLO, Error Budgets, and Toil

Weeks 1-6 established a PolicyService delivery process, independent readiness validation, architecture decisions, observable workflows, and failure-mode analysis.

Week 7 asks two connected questions:

> What service behavior matters enough to measure, and which repetitive work should we invest in removing?

You will define measurable reliability objectives, calculate results from evidence, reason about error budgets, and rank operational work. Follow the Week 5 pattern: reason first, build a small increment, inspect the evidence, revise it, and reflect. This walkthrough provides starter material and review criteria; selecting targets, interpreting results, and choosing an automation candidate remain your work.

**Workbook alignment:** Technical Leadership Study Workbook, Week 7, printed pages 22-24. Suggested time: **5-6 hours**. The appendix maps every requirement to its exercise and expected evidence.

## Learning outcomes

By the end of this lab, you should be able to:

- Define meaningful SLIs and SLOs.
- Explain why 100% reliability is usually the wrong target.
- Use error budgets conceptually to balance change and reliability.
- Identify and prioritize operational toil.

## Before you begin

This lab assumes you completed Weeks 1-6 in your own `technical-leadership-lab` repository. Have these available:

- PolicyService, its test project, pinned SDK, locked dependencies, and build/test workflow.
- The actual issuance route and synthetic request body used in Week 5.
- Your Observability Contract and saved logs, metrics, traces, and deployment identity.
- Week 3 health/readiness checks and DEV/QA deployment evidence.
- Week 6 failure analysis, recovery observations, and reliability-improvement backlog.
- A disposable environment and the instructions needed to restore normal operation.

Inspect your actual code and evidence before deciding how to measure it. Do not assume that a deployment evidence bucket, a telemetry dashboard, or a long-lived QA environment has already been implemented.

If issuance creates a synthetic in-memory policy after a connectivity query, keep that boundary explicit. It does not establish durable issuance, transaction correctness, or production reliability.

**Scope:** The required work is defining and measuring SLIs, producing the two portfolio documents, and choosing an automation candidate. Implementing the chosen automation, centralized telemetry storage, alert delivery, R2 integration, or the Week 8 certification service is optional. A small local measurement exercise is enough to demonstrate the lab end-to-end.

## How to work through the week

| Work block | Suggested time | Result |
| --- | --- | --- |
| Reading and service-boundary review | 75 minutes | Reading notes and user-oriented questions |
| SLI definitions and target selection | 60 minutes | Three service SLIs and one delivery SLI |
| Measurement and error-budget exercise | 75 minutes | Reproducible results and policy scenarios |
| Toil inventory and prioritization | 60 minutes | Ten scored tasks and ranked backlog |
| Scorecard, knowledge check, reflection, closeout | 60 minutes | Reviewed portfolio artifacts |

This is a planning guide. Checkpoints occur at larger milestones; save evidence before moving on.

## Required reading selections

Use both resource collections named by the workbook. Focus on these selections rather than attempting to read either entire book:

1. [Google Site Reliability Engineering: table of contents](https://sre.google/sre-book/table-of-contents/)
   - Chapter 4, [Service Level Objectives](https://sre.google/sre-book/service-level-objectives/): indicators, objectives, agreements, and choosing measurements.
   - Chapter 5, [Eliminating Toil](https://sre.google/sre-book/eliminating-toil/): identifying repetitive operational work.
   - Selected sections of Chapters 6, 7, and 15: monitoring, automation, and learning from incidents.
2. [Google SRE Workbook: table of contents](https://sre.google/workbook/table-of-contents/)
   - Chapter 2, [Implementing SLOs](https://sre.google/workbook/implementing-slos/): defining and reviewing objectives.
   - Selected sections of Chapter 4, Monitoring: connecting measurements to useful decisions.
   - Appendix B, [Example Error Budget Policy](https://sre.google/workbook/error-budget-policy/): an example to critique when drafting your own policy.

Record completed chapters/sections in `docs/week-07/README.md`. For each collection, write one idea you will apply and one assumption you need to verify in PolicyService. The workbook specifies reading selections, not a separate mandatory tutorial.

## Portfolio artifacts

Follow your repository's existing conventions; these paths are suggestions:

| Path | Purpose |
| --- | --- |
| `docs/week-07/README.md` | Reading record, setup, reproducible commands, and artifact index |
| `docs/week-07/sli-definitions.md` | Measurement rules, targets, windows, failure criteria, data sources, and limitations |
| `docs/week-07/experiments.md` | Baseline, fault/recovery, calculations, and error-budget scenarios |
| `docs/week-07/evidence/` | Sanitized source records and measurement outputs |
| `docs/week-07/reliability-scorecard.md` | **Required deliverable:** one-page Reliability Scorecard |
| `docs/week-07/toil-reduction-backlog.md` | **Required deliverable:** ten scored tasks, ranking, and chosen automation candidate |
| `docs/week-07/retrospective.md` | Knowledge-check answers, reflections, and weekly retrospective |
| Existing script/configuration paths | Any small measurement helper or optional automation |

The scorecard is the concise decision view. Put detailed definitions and raw evidence in supporting files so the scorecard remains readable on one page.

---

## Step 1: Start with users and boundaries

Create the Week 7 documentation directory and review Weeks 3, 5, and 6 before choosing metrics.

### A. Identify the experience you intend to protect

Complete these statements in your own words:

- A PolicyService user needs to ______ successfully within ______.
- A QA engineer receiving a deployment needs to ______ before beginning testing.
- A healthy process does not establish ______.

Use actual behavior. A successful health endpoint is useful infrastructure evidence, but does it establish that issuance works? What can fail while the process stays alive?

### B. Describe what the lab can observe

Record the issuance success boundary, database dependency, deployment identity, environment, and measurement vantage point. Distinguish server-observed requests from client attempts that never reach the server.

Identify one Week 6 failure mode that each proposed measurement could reveal. Also identify one it could miss. Do not use the risk score from your failure analysis as an SLI; a risk ranking and an observed service measurement serve different purposes.

### C. Keep the concepts distinct

Create a short own-word comparison of SLI, SLO, SLA, error budget, and toil, each with a PolicyService example. Leave the formal knowledge-check answers for Step 10.

Prompts:

- Which statement describes observed behavior, and which describes a desired target?
- Which objective is an internal learning proposal rather than an agreement with a customer?
- Which repeated activity produces enduring improvement, and which simply restores yesterday's state?

## Step 2: Define three service SLIs and one deployment SLI

The workbook requires three SLIs for the sample service and a deployment-oriented SLI. To cover both requirements explicitly, define **three service SLIs plus a fourth delivery SLI**. Keep the delivery result separate from the user-facing service results.

Use this candidate set as a starting point, then adapt it to your actual service:

| Candidate | Question to answer | Boundary to investigate |
| --- | --- | --- |
| Successful policy issuance | Can a user complete the workflow? | Eligible issuance attempt to declared policy result |
| Timely policy issuance | Can the user complete it quickly enough? | Entire issuance request, with a chosen duration threshold |
| Issuance result correctness | Does a successful response contain the expected result? | Response invariants; stored state only if persistence exists |
| QA deployment health validation | Does a QA deployment produce an accessible service? | Deployment attempt to first health validation within a deadline |

You may replace a service candidate if you explain why the replacement better represents meaningful behavior. Preserve at least one user-facing workflow SLI and the deployment-oriented SLI.

### A. Required definition fields

For **each of the four SLIs**, record these exact workbook fields in `sli-definitions.md`:

| Field | What you must specify |
| --- | --- |
| Measurement | Formula, unit, numerator, denominator, and observation boundary |
| Target | Proposed SLO, including the success percentage and any latency threshold |
| Window | Duration, rolling/calendar/run-based convention, timezone, and event inclusion rule |
| Failure Criteria | Conditions counted as bad events, exclusions, timeouts, and unknown outcomes |
| Data Source | Actual records/metric names, retrieval method, identity filters, and evidence location |

Also add a user/consumer, owner, target rationale, minimum sample rule, and known blind spot.

Starter definition, to repeat and complete for each SLI:

```markdown
## SLI: [name]

- Consumer and question: [who needs this behavior and why]
- Measurement: [good events / eligible events; units and boundary]
- Target: [objective and rationale; label provisional lab choices]
- Window: [duration, convention, timezone, inclusion rule]
- Failure Criteria: [bad events, eligibility, exclusions, unresolved events]
- Data Source: [actual source, retrieval, filters, supporting evidence]
- Owner: [reviewer and responder]
- Minimum sample / no-data behavior: [rule]
- Known limitations: [what this cannot establish]
```

### B. Make the denominator defensible

Resolve these questions before counting:

- Are intentionally invalid inputs outside the issuance SLI? If excluded, define a stable rule before looking at results and retain their count separately.
- Does an unexpected rejection of valid input count as failure?
- Do dependency errors, server errors, client-observed timeouts, and failed response assertions count as bad events?
- Do retries count as separate attempts or one logical operation? Explain the vantage point and avoid mixing both.
- Are `/health` and `/readiness` requests excluded from issuance measurements?
- What happens to a request that started within the window but has not completed? Set a deadline or show it as unresolved; do not silently remove it.
- Is a zero denominator reported as **No data**, and what sample size is too small to support a reliability claim?

Do not exclude real failures because they came from a dependency, maintenance, or an inconvenient experiment. You may separate deliberate fault traffic from ordinary traffic using predeclared cohort rules, but show the full exercise result and the separate cohorts explicitly.

### C. Define latency and correctness honestly

For a ratio-based latency SLI, choose a threshold and count eligible attempts that **both succeed and finish within it**. This keeps a fast failure from improving the result. If you choose successful-request-only latency instead, document its denominator and read it alongside the success SLI.

Use the full client-request or HTTP-server duration, according to your declared vantage point. A database-span duration alone does not measure the whole issuance experience. Do not infer the number of requests below an arbitrary threshold from a histogram that cannot resolve that threshold.

For correctness, define observable invariants, such as a non-empty policy identifier and the expected product in the response. Decide whether the denominator is all eligible attempts or only successful responses; the latter is a conditional check that must be read beside issuance success. A toy response assertion does not establish production business correctness.

### D. Define delivery counting rules

For the deployment-oriented SLI, specify:

- When a QA deployment attempt enters the denominator. A build that never begins QA deployment is not a QA deployment attempt.
- The validation deadline, health success condition, and treatment of deployment execution failures.
- Whether the first validation or a retry determines the result. Preserve the initial failure even if later recovery succeeds.
- How cancellations and missing validation evidence are reported.
- Which run/deployment ID prevents the same attempt being counted twice.

You may use health alone for the workbook's example, provided its limits are explicit. If you include readiness, name the additional conditions. Keep an aggregate delivery SLO separate from the existing per-deployment promotion gate: remaining error budget does not authorize bypassing readiness requirements.

**Checkpoint: measurement definitions.** Another engineer can classify a success, failure, slow response, invalid request, missing record, and retried deployment using your rules. All four definitions include the five required fields.

## Step 3: Choose provisional SLOs and explain the tradeoff

Choose a target and evaluation window for every SLI. Use the baseline evidence, user needs, and the effort implied by a stricter objective. Avoid choosing a familiar percentage without justification.

For a short lab run, use an explicitly bounded run window with recorded UTC start/end times. You may separately propose a production window, such as rolling 30 days. Do not present a 20-request lab run as proof of a month-long production SLO.

Record:

1. Why this target is useful to the consumer.
2. What improving it would cost in engineering effort, complexity, or operating expense.
3. What a 100% target would imply for experiments, releases, maintenance, or improvement work.
4. Who would need to agree to a production target and when it should be reviewed.

Targets in this lab are **proposals**, not customer commitments. Keep the SLA distinction explicit without inventing an agreement.

**Mini-review:** If you doubled the target's strictness, which architecture or operational decision might change? Record at least one specific tradeoff you accepted.

## Step 4: Run a measurement exercise from source to result

Demonstrate that your definitions can actually be applied. Saved evidence is acceptable when it contains the required fields. A new disposable local run is useful when you need to fill a gap.

### A. Plan the evidence capture

Record the artifact version, source commit, environment, UTC window, exact request command, timeout, expected response assertions, and capture method in `README.md`.

Create a compact request ledger or equivalent query result with fields such as:

| Field | Purpose |
| --- | --- |
| Observation ID and timestamp | Window membership and deduplication |
| Version and environment | Cohort/identity selection |
| Input class and eligibility | Stable denominator |
| HTTP result or transport error | Observed outcome |
| Full request duration | Latency classification |
| Response assertion result | Correctness classification |
| Trace ID / evidence reference | Connection to supporting records |

These are suggested evidence fields, not new high-cardinality metric dimensions. Reuse Week 5's allowed metric dimensions and correlation policy.

For exported counters, determine whether values are cumulative or interval-based. Use appropriate deltas, handle process resets, and avoid adding repeated cumulative snapshots. For sampled traces, explain why the sample cannot establish complete request counts. A known-attempt client ledger can provide a complete denominator for this small synthetic exercise.

### B. Capture baseline, failure, and recovery

1. Start the identified service in its normal configuration. Confirm health/readiness and issuance baseline behavior.
2. Send a recorded batch of valid synthetic issuance requests. Choose and document a small count, such as 10-20, to validate calculation mechanics.
3. Send one deliberately invalid request and record its classification separately.
4. In the disposable environment, repeat a reversible Week 5/6 dependency fault, or reuse saved fault records that satisfy your definitions. Send valid input that reaches the affected dependency.
5. Restore the environment and issue another valid request. Retain the recovery evidence.
6. Capture at least one duration observation and apply your latency rule. If none exceeds your threshold, show that result honestly; do not invent a slow request.
7. Apply the declared response assertions to the returned results. Record which checks were observable and which would require durable storage.

Preserve source records and the request ledger. If reusing historical records, retain their actual identities and time windows; do not imply that they all belong to one new run. An incomplete record is a measurement gap to fix or report.

### C. Measure the delivery SLI

Use actual QA deployment and validation records when available. Record the deployment IDs, initial outcomes, timestamps/deadlines, and recovery attempts separately. A skipped QA job supplies no deployed sample; an unobserved validation supplies no proven pass.

If no usable QA records exist, exercise a disposable local deployment/health-validation sequence and label the environment **QA simulation**. Preserve the actual command and validation result. This verifies the calculation method while leaving the actual-QA scorecard row **No data**. It does not justify reporting a real QA pass rate.

A hand-authored fixture can help check a parser or formula, but cannot substitute for executing a real service measurement or establishing actual QA reliability.

### D. Calculate and reconcile

For a good-event ratio:

```text
SLI percentage = good eligible events / total eligible events * 100
Bad events = eligible events - good events
```

Make unresolved events visible. Finalize outcomes using your deadline before publishing a definitive ratio, or mark the result provisional with the unresolved count.

Save a calculation table:

| SLI | Evidence/window | Eligible | Good | Bad | Excluded | Unresolved | Observed % | Assessment |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Service SLI 1 | To cite | To count | To count | To count | To count | To count | To calculate | To explain |
| Service SLI 2 | To cite | To count | To count | To count | To count | To count | To calculate | To explain |
| Service SLI 3 | To cite | To count | To count | To count | To count | To count | To calculate | To explain |
| Delivery SLI | To cite | To count | To count | To count | To count | To count | To calculate | To explain |

Use a short script, spreadsheet, query, or visible arithmetic. Retain enough instructions that a reviewer can reproduce the result. Use **No data** for zero eligible events and **Insufficient sample** where your minimum-sample rule applies. A perfect small sample does not demonstrate perfect reliability.

**Checkpoint: end-to-end measurement.** You can follow source evidence through eligibility and classification to each reported result. The real service exercised baseline, failure, and recovery, or your retained records establish those cases. Actual QA and simulated delivery results are clearly distinguished.

## Step 5: Calculate an error budget and rehearse decisions

Use at least the issuance-success objective. Optionally repeat for the other ratio objectives; keep each budget tied to its own definition and window.

For a completed request-based window, with target `T` expressed as a fraction:

```text
Allowed bad events = (1 - T) * eligible events
Budget consumed = observed bad events / allowed bad events * 100
Budget remaining = allowed bad events - observed bad events
```

Keep the unrounded allowance for assessment. Fractional allowances are mathematically meaningful: an allowance of 0.5 events means one bad event misses the objective. A zero allowance makes the consumption ratio undefined; a 100% target permits no bad events. Negative remaining budget indicates the objective was exceeded, rather than extra capacity to spend.

Illustrative arithmetic only: with a 99% objective and 1,000 eligible attempts, the allowance is 10 bad attempts. Three bad attempts consume 30% of that allowance. This is a calculation example, not a recommended PolicyService target or an observed result.

For an ongoing window, future request volume is unknown. Label projected totals as forecasts and distinguish them from the observed count. For a rolling window, older events expire rather than the entire budget resetting at midnight. Do not convert a request-based budget into minutes of downtime unless you separately define a time-based SLI.

### Draft a small decision policy

Complete these scenarios in `experiments.md`; choose your own thresholds and actions:

| Scenario | Evidence to inspect | Action and owner | Condition for resuming normal changes |
| --- | --- | --- | --- |
| Objective met with substantial budget remaining | To specify | To decide | To decide |
| Failures accelerate and budget is being consumed quickly | To specify | To decide | To decide |
| Budget exhausted / objective missed | To specify | To decide | To decide |
| Telemetry unavailable or sample insufficient | To specify | To decide | To decide |

Include how feature work, reliability improvements, emergency fixes, and risky releases are handled. Explain who can approve an exception and what evidence is retained. Budget is a decision aid, not a mandate to cause failures or a substitute for individual deployment gates.

Tie one response to your Week 6 backlog: which improvement addresses the observed failure mode? Identify at least one tempting action that could improve a number without improving the experience, such as excluding failed attempts after seeing the result.

## Step 6: Identify ten repetitive operational tasks

List **10 distinct tasks** from your lab or implementation workflow in `toil-reduction-backlog.md`. Write each as a specific action with a trigger and an outcome. Label its evidence as observed, estimated from experience, or hypothetical.

Possible prompts, to adapt rather than copy as established facts:

1. Repeatedly checking health after a deployment.
2. Collecting readiness evidence from workflow output.
3. Looking up the artifact version running in an environment.
4. Matching a failed request to its trace and deployment.
5. Comparing environment configuration values.
6. Recreating the same synthetic test inputs.
7. Assembling a routine deployment-status summary.
8. Rerunning known checks after an environment repair.
9. Cleaning up disposable lab resources.
10. Reviewing an ambiguous failure to decide whether to proceed.

You may identify tasks from workplace experience, but do not include confidential records. Keep production examples separate from implemented lab capabilities.

### Classify before scoring

For each task, ask whether it is manual, repetitive, automatable, reactive/tactical, grows with service volume, and provides little enduring value. Explain the classification: **toil**, **mixed work**, or **judgment-heavy operational work**.

The workbook asks for repetitive tasks, not a declaration that every repetitive activity is toil. Keep at least one frequent task requiring human judgment so you can reason about the third reflection prompt. Separate mechanical evidence gathering from a decision such as interpreting contradictory requirements or accepting release risk.

## Step 7: Score and rank the toil candidates

Record all five workbook dimensions for every task: **Frequency, Time, Human Judgment Required, Automation Potential, and Risk**. Define the scales before ranking.

Starter scoring rubric; adapt it and document your changes:

| Dimension | 1 | 3 | 5 |
| --- | --- | --- | --- |
| Frequency | Monthly or less | Several times per week | Many times per day |
| Time per occurrence | Under 5 minutes | 15-30 minutes | Over 60 minutes |
| Human Judgment Required | Fixed rule | Some contextual interpretation | Consequential ambiguous decision |
| Automation Potential | No stable repeatable rule | Mechanizable subset | Stable inputs and deterministic output |
| Automation Risk | Read-only and easy to verify | Reversible write with bounded impact | Destructive or broad operational impact |

Use 2 and 4 for intermediate cases. Also retain numeric occurrences/week and minutes/occurrence rather than relying on ordinal scores alone. Clarify that Risk here means risk **introduced by automating** the task. Add a separate field if risk of leaving it manual matters.

Suggested effort calculation:

```text
Weekly manual minutes = occurrences/week * minutes/occurrence
```

Rank eligible candidates using time saved, repeatability, judgment, risk, implementation cost, and maintenance burden. A suggested rule is to place high-judgment or high-risk items in **human decision / redesign first**, then rank viable automation work by achievable weekly savings and effort. You may use a weighted score, but publish the weights and explain at least one case where your judgment changed its ordering.

Starter backlog table:

| ID / task | Evidence basis / class | Occurrences per week | Minutes each | Frequency score | Time score | Judgment | Potential | Risk | Weekly minutes | Rank / disposition |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| T01: [action] | [basis/class] | [count] | [minutes] | [1-5] | [1-5] | [1-5] | [1-5] | [1-5] | [calculate] | [rank/reason] |

Add rows T02-T10, then order the ten tasks and explain ties or deferred items. Assign an owner and next action to each. Keep the Toil Reduction Backlog separate from Week 6's reliability backlog; link overlapping items without assuming that incident severity and automatable effort yield the same priority.

**Checkpoint: prioritization.** All ten tasks have all five scores, consistent scale meanings, a rank/disposition, and an evidence basis. One frequent judgment-heavy task remains under human control, with any automatable supporting steps identified.

## Step 8: Choose one task for automation

Select one viable candidate and write a small automation proposal beneath the ranked table. The workbook requires a choice; implementation is an optional extension.

```markdown
## Selected Automation Candidate

- Task ID and current manual sequence: [specific steps]
- Why selected: [benefit, confidence, risk, comparison with next candidate]
- Inputs and output: [sources and expected result]
- Trigger and scope: [when and where it runs]
- Automated steps: [deterministic actions]
- Human decision retained: [judgment boundary, if any]
- Failure behavior: [visible failure, no false success, recovery/fallback]
- Owner and maintenance: [who updates and reviews it]
- Acceptance criteria: [observable output and required failure handling]
- Estimate: [implementation effort, time saved, recurring maintenance]
- First verification: [how to compare it with the manual outcome]
```

Estimate achievable savings rather than total task time if only part is automatable. For a simple break-even estimate, divide one-time implementation hours by expected net hours saved per week; account for recurring maintenance and state the uncertainty.

Prefer a small candidate that fits your existing lab, such as producing a summary from captured validation evidence. Do not silently turn an evidence-collection task into automatic release approval or environment repair.

**Optional implementation:** Build a narrow prototype. Run it against a normal case and a missing/failed-input case, compare with the manual result, and record what time it actually saved. If it changes application or pipeline behavior, run the existing relevant restore/build/test checks. Do not describe an unimplemented proposal as measured savings.

## Step 9: Produce and review the one-page Reliability Scorecard

Create a compact document that a reader can scan without reading the investigation narrative. Include all four SLI rows; link to detail for definitions and evidence.

Suggested one-page scaffold:

```markdown
# PolicyService Reliability Scorecard

- As of: [UTC date/time]
- Scope: [environment, workflow, artifact version]
- Evidence: [window(s), source links, sample limits]
- Owner / next review: [role, date]

| SLI / measurement | Target | Window | Failure criteria | Data source | Observed / sample | Assessment |
| --- | --- | --- | --- | --- | --- | --- |
| [Service 1: formula] | [target] | [window] | [bad-event rule] | [link] | [good/eligible; %] | [status] |
| [Service 2: formula] | [target] | [window] | [bad-event rule] | [link] | [good/eligible; %] | [status] |
| [Service 3: formula] | [target] | [window] | [bad-event rule] | [link] | [good/eligible; %] | [status] |
| [QA delivery: formula] | [target] | [window] | [bad-event rule] | [link] | [good/eligible; %] | [status] |

## Budget and Decision

[One objective: allowance, observed bad events, remaining budget,
and recommended action. Label proposals, estimates, and unknowns.]

## Next Engineering Action

[Reliability improvement; chosen toil candidate; owner and next step.]

## Tradeoff and Limitation

[One accepted tradeoff and the most material measurement blind spot.]
```

Keep definitions concise in the table; link to full rules. Use statuses such as **Met in lab sample**, **Missed in lab sample**, **No data**, and **Insufficient sample**, as appropriate. Show counts beside percentages. Do not give the entire system a green status solely because `/health` returned 200.

Markdown has no fixed pagination. Review it in your repository's Markdown preview and, if available, print preview. Use one title, one compact table, and short decision notes so it fits approximately one page with normal margins and readable text. Move supporting narrative to the linked files instead of shrinking the text.

### Review in three passes

**Pass 1 - Measurement:** Can the reviewer reproduce each result from source evidence and determine its denominator, window, identity, and failure rule?

**Pass 2 - Decisions:** Are provisional targets justified? Does the error-budget action protect the actual workflow? Are data gaps and the limits of synthetic issuance visible?

**Pass 3 - Work prioritization:** Does the backlog contain ten consistently scored tasks? Is the automation choice defensible, and is human judgment retained where needed?

Have a reviewer, coach, or independent self-review reproduce one service calculation and challenge the top toil candidate. Record corrections before declaring the artifacts reviewed.

---

## Step 10: Knowledge check

Answer all six workbook questions without consulting its answer key. Save your original answers in `retrospective.md` before checking them.

1. What is an SLI?
2. What is an SLO?
3. What is an SLA?
4. Why should an SLO usually be below 100%?
5. What is an error budget?
6. What characteristics make operational work toil?

After answering, compare with the Week 7 answer key. Record any correction in your own words and connect it to your definitions, calculations, or backlog. Do not replace your explanation with copied definitions.

## Step 11: Reflection prompts

Use all three workbook prompts:

**R1. Which metric best represents the user experience rather than infrastructure health?**

Choose one of your service measurements. Explain the user outcome it captures, identify a failure it reveals while health remains good, and acknowledge its vantage-point limitation.

**R2. What behavior would a 100% SLO accidentally discourage?**

Use a concrete change, maintenance task, or controlled experiment. Explain the pressure a perfect target could create and how your objective/policy handles useful change responsibly.

**R3. Which toil item is frequent but should not be automated because it requires judgment?**

Identify the task ID, describe the ambiguity and consequence of a wrong decision, and separate any mechanical evidence gathering that can still be automated from the decision that should remain human.

## Step 12: Weekly retrospective

Answer the workbook's four questions:

1. **What became clearer this week?**
2. **What was harder than expected?**
3. **What would I do differently in a production system?**
4. **What artifact from this week best demonstrates growth?**

Include an observation from denominator selection, a data gap, error-budget reasoning, or a ranking decision. Keep learning notes outside the scorecard so the scorecard remains usable by readers who were not part of the exercises.

## Step 13: Repository closeout

Update the root README's roadmap and documentation index with links to the Week 7 overview, Reliability Scorecard, and Toil Reduction Backlog. Verify relative links from their actual repository locations.

### Completion checklist

- [ ] I completed and recorded selections from both workbook resource collections.
- [ ] I defined three sample-service SLIs and a separate deployment-oriented SLI.
- [ ] At least one SLI measures the user-facing issuance workflow.
- [ ] Every SLI records Measurement, Target, Window, Failure Criteria, and Data Source.
- [ ] Eligibility, retries, timeouts, missing data, and low sample volume have explicit rules.
- [ ] I ran the measurement path end-to-end and can explain its result.
- [ ] Results can be reproduced from evidence, with actual QA distinguished from simulation.
- [ ] I calculated an error budget and explained how it affects change/reliability decisions.
- [ ] I can explain why 100% is usually an unhelpful reliability target and at least one tradeoff I made.
- [ ] I identified ten repetitive operational tasks with evidence/estimate labels.
- [ ] Every task has Frequency, Time, Human Judgment Required, Automation Potential, and Risk scores.
- [ ] I ranked the candidates and chose one for automation with a concrete proposal.
- [ ] I identified a frequent task whose consequential judgment remains human.
- [ ] The Reliability Scorecard is concise enough for a one-page decision view.
- [ ] The scorecard and ranked backlog are complete and committed to the repository.
- [ ] Any fault settings were restored, and evidence contains no secrets or real customer data.
- [ ] Existing checks pass where code, dependencies, or workflow behavior changed.
- [ ] I completed all six knowledge questions before consulting the answer key.
- [ ] I answered all three reflection prompts and wrote the four-part weekly retrospective.
- [ ] Documentation links work and the evidence limitations are visible.

Review changes before staging:

```powershell
git status
git diff --check
git diff
```

Stage your actual files deliberately: Week 7 documents, sanitized evidence, README/index updates, and any measurement or optional automation changes. Review `git diff --cached` before committing. Do not stage private configuration or unrelated work.

```powershell
# Run after staging the intended files and reviewing the staged diff.
git commit -m "Complete week 7 SRE scorecard and toil backlog"
git push
```

If you maintain weekly milestone tags, add `week-07` after confirming it does not already exist. The tag is optional; the two committed deliverables are required.

**Week 7 is complete when another engineer can understand your reliability objectives, reproduce the reported results, follow the proposed error-budget response, and explain why the selected automation candidate deserves priority.**

---

## Appendix: Workbook coverage review

This walkthrough was checked against the supplied workbook's **Week 7 printed pages 22-24**. This review verifies instructional coverage, not that a reader has performed the exercises.

| Workbook requirement | Walkthrough coverage | Evidence the reader must produce |
| --- | --- | --- |
| Define meaningful SLIs and SLOs | Steps 1-3 | Four definitions and target rationales |
| Explain why 100% reliability is usually wrong | Steps 3, 5, 10-11 | Tradeoff reasoning and own-word answers |
| Use error budgets to balance change and reliability | Step 5 | Calculation and decision scenarios |
| Identify and prioritize operational toil | Steps 6-8 | Inventory, scores, ranking, choice |
| Google SRE selected chapters: monitoring, SLOs, automation, incident learning | Required reading | Chapter/section record and application notes |
| Google SRE Workbook SLO and monitoring guidance | Required reading | Reading record and applied idea |
| Hands-on 1: three SLIs for the sample service | Step 2 | Three service definitions |
| Hands-on 2: Measurement, Target, Window, Failure Criteria, Data Source for each | Steps 2, 9 | All five fields for all four SLIs |
| Hands-on 3: at least one user-facing workflow SLI | Steps 1-2, 4 | Issuance definition and observed evidence |
| Hands-on 4: deployment-oriented SLI | Steps 2, 4, 9 | QA definition; real or clearly limited simulation evidence |
| Hands-on 5: ten tasks scored on Frequency, Time, Human Judgment Required, Automation Potential, Risk | Steps 6-7 | Ten complete scored rows and rubric |
| Hands-on 6: rank candidates and choose one for automation | Steps 7-8 | Ordered backlog and selected proposal |
| Required one-page Reliability Scorecard | Step 9 | Reviewed concise scorecard |
| Required ranked Toil Reduction Backlog | Steps 6-8 | Complete ranked backlog |
| Completion: required reading/tutorial selections | Required reading, Step 13 | Reading record; no extra tutorial invented |
| Completion: lab runs end-to-end and result is explainable | Steps 4-5, 9 | Reproducible measurements and review |
| Completion: required deliverable committed | Step 13 | Commit containing both documents |
| Completion: at least one tradeoff explained | Steps 3, 8-9 | Concrete choice and consequence |
| Completion: knowledge check before answer key | Step 10 | Six original answers before corrections |
| Completion: brief weekly retrospective | Step 12 | Four retrospective responses |
| Reflection: user experience versus infrastructure | Step 11, R1 | Chosen metric and example |
| Reflection: behavior discouraged by 100% SLO | Step 11, R2 | Concrete behavior and policy response |
| Reflection: frequent task requiring judgment | Steps 6-8; Step 11, R3 | Task ID and retained human decision |
| Knowledge questions 1-6 | Step 10 | All six prompts preserved without supplied answers |
| Retrospective: clearer, harder, production changes, growth artifact | Step 12 | All four prompts preserved |

**Review outcome:** Every Week 7 requirement is covered. The fourth SLI removes ambiguity between service and deployment measurement. Evidence replay, a short local run, error-budget scenarios, and an automation proposal make the portfolio reviewable without requiring a production monitoring platform or implementation of the selected automation.
