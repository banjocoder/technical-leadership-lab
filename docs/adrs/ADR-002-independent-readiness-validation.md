# ADR-002: Readiness Validation Is Independent of Deployment Execution

- Status: Accepted
- Date: 2026-09-25

## Context

The ultimate goal of the delivery process is to provide software in an environment that an end user can actually use. A successful build or deployment alone does not establish that outcome.  
The delivery process must provide meaningful evidence at each boundary: that the application built successfully, that the intended artifact was deployed, that the application is accessible, and that the environment is usable and ready for its intended purpose. Without separate evidence for these boundaries, a successful deployment could be incorrectly treated as a usable environment, making failures harder to identify and address.

## Decision

After deployment, the system will separately execute readiness validation steps. The checks that determine whether an environment is Ready or NotReady will be configured independently from deployment execution. Readiness validation will include application smoke tests and required dependency checks.  
Deployment success confirms that the intended artifact was deployed; readiness confirms that the environment is usable for its intended purpose. If an environment is NotReady, it will not be promoted to the next environment or accepted for use until the failed checks are addressed.

## Alternatives Considered

### 1. Treat successful deployment as proof of readiness

Successful deployment as evidence is faster, more simple, and easier to maintain. It does not, however, communicate whether or not the app is actually ready to use. 

### 2. Embed validation inside deployment with a single combined result

Embedded readiness validation couples the readiness test to the deployment process. Again, this adds simplicity but obfuscates the separation of checks. It also ties the readiness check to share the same toolset and timeline as the deploy process. What if we need to run a readiness check without redeploying?

### 3. Perform validation only during the build

Validation only during the build can verify the source code and artifact before deployment, but it cannot validate the target environment. It cannot detect deployment-time configuration errors, unavailable dependencies, or whether the deployed application is actually accessible and usable.

## Consequences

### Positive

Positive: We have stronger, explicit evidence that the app is running, available, and executing as expected. This process can also be rerun without needing to redeploy the environment. 

### Negative

Negative: Additional resources and complexity added to our deployment process, extra configuration required and more of a learning curve. 

## Responsibilities

Responsibilities: Someone (likely the environment owner) must take responsibility for deciding and configuring which checks are required for a successful deployment and respond to environment ready failures.