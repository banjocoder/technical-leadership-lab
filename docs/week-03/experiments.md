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
