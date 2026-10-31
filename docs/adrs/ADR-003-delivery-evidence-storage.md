# Delivery Evidence Is Stored Independently of CI/CD Execution

- Status: Accepted
- Date: 2026-09-25

## Context

Decision makers must be able to quickly find and understand the inputs and results for each stage of a delivery, including the artifact and source commit, target environment, deployment result, readiness result, and individual checks. This evidence supports informed decisions about promotion, remediation, and next steps.  
The results must also persist as a long-lived record that can be evaluated, measured, and audited for reporting and assessment purposes.

## Decision

GitHub Actions will produce structured JSON evidence for each delivery and store it as a new immutable record in an external Cloudflare R2 bucket. R2 is the durable source of record; GitHub Actions will display the relevant current results in the workflow summary. A separate historical dashboard is deferred, but remains possible because the evidence is retained in an accessible, structured store.
Evidence for the pipeline will include the following: 
- Artifact version and SHA-256; source commit
- environment
- timestamp
- workflow run
- deployment results
- readiness result
- per-check details

## Alternatives Considered

1) Github actions as the evidence store
GitHub Actions does retain run information, but its history is coupled to platform retention and workflow access. It is not an independent source of delivery evidence.

2) A JSON file stored with the deployed environment
It provides useful current-state information, but history is distributed across environments and can be lost or overwritten when an environment is redeployed, rebuilt, or unavailable.

3) An application database table for deployment evidence
It is structured and queryable, but it couples delivery evidence to the application/database being evaluated. A database outage or failed deployment could prevent recording the exact failure evidence needed.

## Consequences

This enables reliable persistence and review of delivery evidence independently of the target application and deployed environments. The drawbacks are added complexity, a new Cloudflare R2 dependency to operate, and less convenient querying. Ownership is required for the R2 integration, JSON schema, access controls, retention, and failed-write handling.


