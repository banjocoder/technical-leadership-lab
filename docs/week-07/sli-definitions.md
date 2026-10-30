# PolicyService SLI Definitions

**Status:** Draft for review  
**As of:** 2026-10-29  
**Scope:** PolicyService issuance and retrieval, SQL Server persistence, and proposed QA deployment validation  
**Related:** [Reliability scorecard](reliability-scorecard.md), [experiment record](experiments.md)

## Scope and Evidence Status

The business requirement is that issued policies are durably recorded and remain accessible. PolicyService now writes policies to SQL Server through a repository and stored procedure. A caller-supplied Idempotency-Key identifies one intended issuance; the database retains it as RequestID. A matching retry returns the same policy; different issuance inputs with the same key are a conflict.

An earlier local experiment issued ten policies, restarted the application, and retrieved all ten. This is a user-reported 10/10 observation. A fresh reproducible local run has also been reported as passing with response evidence and timings retained, but its exact records have not been reviewed for these drafts. Do not assume it has the same count as the earlier run.

All measured evidence is local. Actual QA deployment reliability and production 30-day reliability remain unmeasured. An expected error during fault injection can mean the test behaved correctly while still being a bad HTTP attempt or a missed latency objective.

## Shared Counting and Evidence Rules

- **Consumer perspective:** client-observed logical issuance, not only server-received HTTP requests. Capture transport failures as well as responses.
- **Eligibility:** valid, business-eligible synthetic issuance inputs. Determine eligibility before examining the result. Correctly rejected invalid inputs are excluded and counted separately. Technical faults and incorrect rejections of valid inputs remain eligible failures.
- **Identity:** one Idempotency-Key is one logical issuance. Retries use unchanged business inputs and the same key. New policies use new keys. A deliberately conflicting retry is a separate negative contract test, not a new valid issuance.
- **Attempts:** retain every HTTP attempt; aggregate by key for logical-issuance SLIs. Server counters alone do not establish unique logical operations or client attempts that never reach the service.
- **Time:** retain UTC timestamps and client durations. Elapsed logical-operation time starts at the first attempt and includes retry delays; do not sum only HTTP durations. Use a consistent client clock, preferably monotonic elapsed timing where available.
- **Boundaries:** health/readiness traffic is excluded from issuance counts. A replay of an already completed operation does not create another policy or reset its original timing.
- **Cohorts:** label baseline, deliberate-fault, and recovery traffic before running. Show the full exercise and separate cohort results; never remove failures after seeing them.
- **Unknowns:** no-data and unresolved outcomes remain visible. Missing evidence never proves success. Reconcile pending outcomes before publishing a final ratio.
- **No data:** a zero denominator is reported as No data, not 100%.
- **Sample sufficiency:** minimum sample rules have not yet been agreed. Until they are, publish counts and label results descriptive local samples; do not claim statistical support for the proposed operating objectives.
- **Ownership:** lab maintainer owns local definitions and calculations. Production issuance ownership and alert response ownership require assignment; Operations owns the proposed QA breach investigation.

### Evidence Record

For each attempt, retain observation ID, UTC timestamp, environment, artifact version/commit, input eligibility/cohort, Idempotency-Key, status or transport error, request duration, returned policy identity, and evidence reference. Avoid placing policy IDs or request GUIDs in metric labels.

For each logical issuance, derive first-attempt time, successful-response time, known commit evidence, completion deadline, attempt count, elapsed time, and final classification. A timeout after insertion is an ambiguous client outcome until reconciled by a same-key retry or other evidence.

For restart checks, retain selected policy identities, restart timestamps, readiness time, GET attempts, and verification time. For QA, retain deployment/run ID, deployment start/completion, execution status, each validation outcome, and validation completion time.

Suggested storage: `evidence/[run-date]-local-run/`, indexed by `experiments.md`. Evidence links in these drafts are planned repository paths, not claims that the files already exist.

## SLI 1: Policy Identity Verified After Application Restart

- **Consumer and question:** policy users and maintainers need issued records to survive application-process replacement.
- **Measurement:** identity-verified policies after restart / all confirmed committed policies selected before restart × 100. Unit: percentage of selected records.
- **Good event:** an independent GET after restart returns the expected PolicyID, TermNumber, and PolicyHolderName before the verification deadline.
- **Target:** 100% of the tested cohort. Confirmed loss of a committed policy is unacceptable. This is a scoped restart-survival requirement, not a universal guarantee of data durability.
- **Window:** bounded experiment with recorded UTC start/end; fixed cohort declared before restart. Verification allows retries, including 404s, for one minute after the restarted service is ready.
- **Failure Criteria:** mismatched identity is a failed check. Records not verified by the deadline remain unresolved and count as non-good for deadline assessment; investigate whether the cause is access failure, missing data, or something else. Do not claim data loss from an API error alone.
- **Data Source:** original issuance response/commit evidence, restart and readiness records, independent GET responses and timestamps; linked from `experiments.md`.
- **Owner:** lab maintainer; proposed internal-team alert on verification timeout. Production responder not yet assigned.
- **Minimum sample / no-data behavior:** the initial completed experiment had ten selected policies. Ten is an exercise size, not a statistical sufficiency threshold. Fresh-run size must be recorded. Zero selected records means No data.
- **Known limitations:** identity-only assertions do not validate LOB, coverage dates, or full business correctness. Application restart does not exercise database restart, storage loss, backup restoration, or disaster recovery. Immediate retrieval alone is not this restart-survival check.

Report verified, mismatched, and unresolved counts alongside the ratio. Unresolved records stay in the cohort denominator; do not silently omit them. A deadline miss and confirmed record loss must be distinguished in the narrative.

## SLI 2: Eventual Logical Issuance Success

- **Consumer and question:** can a user complete a valid policy issuance despite recoverable technical failures?
- **Measurement:** eligible logical issuances with a committed policy and successful issuance response within two hours / all eligible logical issuances × 100. Unit: percentage of logical operations. An issuance is successful only when the policy is committed and independently retrieved with matching PolicyID, TermNumber, and PolicyHolderName.
- **Target:** proposed 99.5% over rolling 30 days. This allows five failed operations per 1,000, without allowing loss of policies already committed.
- **Window:** rolling 30-day cohort, assigned by first-attempt UTC time. Draft reporting convention: at evaluation time R, finalize operations first attempted in [R minus 30 days minus 2 hours, R minus 2 hours). This gives every included operation its full deadline; newer operations are reported separately as pending. This two-hour-lag convention needs review. Both issuance and retrieval must complete within two hours of the first valid attempt. Retries do not reset the clock.
- **Failure Criteria:** no successful completion by two hours is bad. Valid-input network, schema, database, server, and unexpected business-rule failures remain eligible. Matching retries may recover the same operation before the deadline. Later recovery is recorded but does not rewrite its missed deadline as success. No verified retrieval by that deadline counts as failed workflow completion; it does not, by itself, prove data loss.
- **Data Source:** Postman request ledger grouped by Idempotency-Key; successful response and policy identity, corroborated by stored-procedure behavior and independent retrieval evidence. Where insertion succeeded but response was lost, reconcile via same-key retry.
- **Owner:** lab maintainer for the experiment; operating service owner to be assigned.
- **Minimum sample / no-data behavior:** no agreed statistical threshold; publish exact counts and label small runs descriptive. Zero eligible operations means No data.
- **Known limitations:** a two-hour eventual-success measure can conceal unacceptable delays. Read alongside latency and restart survival. Commit time is not independently measured by the existing Postman evidence; if definitive commit-only timing is required, add suitable durable-write evidence.

### Independent Retrieval Boundary

Independent retrieval should also complete within two-hour issuance deadline.

## SLI 3: Timely Logical Issuance

- **Consumer and question:** does an eligible issuance complete quickly enough for normal use?
- **Measurement:** eligible logical issuances completing successfully in strictly less than 10 seconds / all eligible logical issuances × 100.
- **Target:** proposed 95% over rolling 30 days. Occasional slowness is tolerated; client-observed network latency remains included.
- **Window:** rolling 30 days by first-attempt UTC time. For direct reconciliation with eventual success, use the same finalized cohort as SLI 2; bounded local runs report actual UTC bounds separately.
- **Failure Criteria:** elapsed time of 10 seconds or more, unsuccessful issuance, or no successful result within 10 seconds is non-good. Fast errors do not count as timely. Retries keep the first-attempt clock; pauses between attempts count.
- **Data Source:** first-attempt timestamp and successful-response completion time per key, plus individual Postman durations and outcomes. A recovered operation's final HTTP duration alone is insufficient.
- **Owner:** lab maintainer; operating service owner to be assigned.
- **Minimum sample / no-data behavior:** publish counts; no statistical minimum yet agreed. Zero eligible operations means No data.
- **Known limitations:** measures response completion, not full business correctness or post-restart durability. Network delays are part of this client-facing SLI. A logical operation may pass SLI 2 while failing this SLI.

The lower target reflects a deliberate tolerance for occasional slow experience, not an exclusion of inconvenient latency.

## SLI 4: QA Deployment Validation Success

- **Consumer and question:** can QA begin testing a successfully deployed and verified environment?
- **Measurement:** QA deployment attempts that complete execution successfully and pass all required validation / all QA deployment attempts × 100.
- **Target:** proposed 95% over rolling 30 days. Remaining budget never authorizes bypassing an individual deployment gate.
- **Window:** rolling 30 days by QA deployment-start UTC time; unique deployment ID prevents double counting. Validation must finish no later than five minutes after deployment completion. Deployment execution timeout and treatment of still-running attempts require a defined operational limit before final reporting.
- **Required checks:**
  1. Initial synthetic issuance completes successfully within 10 seconds.
  2. Same-key retry returns the same policy without inserting a duplicate.
  3. After the scoped environment restart, independent retrieval verifies expected policy data.
  4. Required database schema checks pass.
- **Failure Criteria:** deployment execution failure, any required check failure, or validation exceeding the five-minute limit is bad. A later repair or successful rerun is retained as recovery evidence without erasing the original result. Missing validation is not a pass. Skipped QA jobs are not deployment attempts. Cancellation before deployment starts is excluded; cancellation after start is retained as a non-good attempt (draft convention).
- **Data Source:** actual QA deployment records and validation results linked by deployment ID, environment, and artifact version. Current local Postman records cannot establish actual QA deployment success.
- **Owner:** Operations leads investigation and coordination; Development and QA contribute.
- **Minimum sample / no-data behavior:** actual QA is currently No data. Publish counts beside percentages; no minimum statistical sample yet agreed.
- **Known limitations:** a synthetic workflow does not establish all business functionality. Restart scope, expected-data assertions, schema contract, and deployment execution timeout still need specification. Safely scoped restarts belong in a disposable lab or coordinated QA procedure.

### QA Breach Response

When rolling success falls below 95%:

- Notify QA, Development, Operations, and the next leadership level.
- Operations establishes an initial assessment, corrective-action owner, and plan within three business days of detection.
- Normal deployments generally continue, each subject to its own gate.
- Additional review may be required where evidence indicates elevated deployment risk.

Exception approver, additional-review criteria, and operating business-day calendar remain to be assigned. This is a proposed response policy, not evidence that alerting or enforcement is implemented.

## Calculations and Error Budgets

For each ratio:

```text
Observed percentage = good / eligible × 100
Allowed bad events = (1 - target fraction) × eligible
Budget consumed (%) = bad / allowed bad events × 100
Budget remaining = allowed bad events - bad
```

Keep fractional allowances; do not round them up to grant additional failures. Consumption is undefined when allowance is zero. The 100% restart-survival target permits no non-good events; report counts and investigation status instead of dividing by zero.

Use finalized outcomes, and show pending/unresolved counts. For ongoing windows, unknown future volume is not an already available budget. Calculate budgets separately for issuance, latency, and QA deployment; do not pool them or convert request counts into downtime minutes.

## Local Evidence and Review Checklist

- [ ] Record run ID, UTC bounds, environment, artifact version, commit, and repeatable commands or collection.
- [ ] Link raw attempts, response evidence, timings, keys, and restart records.
- [ ] Reconcile logical operations and individual HTTP attempts.
- [ ] Report baseline, fault, recovery, excluded invalid-input, and unresolved counts.
- [ ] Calculate fresh-run success, latency, and restart-survival results from evidence.
- [ ] Keep the earlier reported 10/10 experiment separate from the fresh run.
- [ ] Label actual QA and 30-day results unmeasured.
- [ ] Set minimum-sample rules and resolve draft reporting conventions.
- [ ] Assign operating owners and remaining QA response-policy details.
- [ ] Retain identity-check limits; specify broader QA data/schema assertions separately.
- [ ] Ensure evidence contains synthetic data and no credentials.

