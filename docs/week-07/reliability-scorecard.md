# PolicyService Reliability Scorecard

**Draft as of:** 2026-10-29 | **Environment:** Local lab; actual QA not measured  
**Artifact / commit:** [record version and commit] | **Run:** [UTC start/end]  
**Owner:** Lab maintainer | **Next review:** After evidence reconciliation  
**Definitions:** [SLI definitions](sli-definitions.md) | **Evidence:** [Experiment record](experiments.md)

| SLI / measurement | Target | Window / failure rule | Data source | Observed / assessment |
|---|---|---|---|---|
| Restart survival: identity-verified policies / committed policies selected before restart | 100% of tested cohort | Bounded restart experiment; retry GETs for 1 minute after readiness; unresolved results are not passes | Issuance responses, restart/readiness timestamps, GET results | Earlier experiment: **10/10 (100%)**, user-reported. Fresh retained run: [verified/eligible; unresolved]. |
| Eventual issuance: completed eligible logical issuances / eligible logical issuances | 99.5% | Proposed rolling 30 days; commit issued and independently retrieved within two hours of first attempt | Postman attempts grouped by Idempotency-Key | Fresh run reported as passing; exact counts/times pending reconciliation. **30-day result not measured.** |
| Timely issuance: eligible logical issuances completing successfully in <10 seconds / eligible logical issuances | 95% | Proposed rolling 30 days; retries do not reset clock; errors and late successes are not timely | Client timestamps and response durations | **10/10 (100%)** Fault/recovery may pass eventual issuance while missing latency. |
| QA deployment validation: successful deployments passing every required check / QA deployment attempts | 95% | Proposed rolling 30 days; all checks within 5 minutes after deployment completes; execution failures count as bad | QA deployment records and linked validation evidence | **No data for actual QA.** Local service experiments are separate evidence. |

## Error Budget and Response

For eventual issuance, allowance = **0.005 × eligible logical issuances**; consumed = failed logical issuances / allowance. Exact local counts are pending, so consumption is **not yet calculated**. Each SLO has its own budget; issuance allowance does not permit loss of committed policies.

Below 95% QA deployment success: notify QA, Development, Operations, and the next leadership level. Operations leads, with Dev/QA contributing; assessment, corrective-action ownership, and a plan are due within **3 business days**. Normal deployments continue subject to their individual validation gates; additional review may apply based on risk.

## Next Action and Limits

Reconcile the fresh Postman evidence, calculate service ratios, and retain fault/recovery and restart records. Alert the internal team when newly issued policy retrieval remains unverified after one minute (proposed response, not confirmed implemented). Toil candidate: **not yet selected**.

A successful local restart test supports application-process survival, not database-disaster durability. Small synthetic samples do not establish 30-day reliability. Identity checks cover PolicyID, TermNumber, and PolicyHolderName; they do not establish full business correctness.

*Draft placeholders and relative evidence links must be completed before portfolio closeout.*

