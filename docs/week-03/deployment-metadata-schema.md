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

```json
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

````

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