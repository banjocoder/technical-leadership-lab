## Reflection prompts:

R1. What conditions should block promotion?

Promotion should be blocked when required gates for the target environment fail. These may include approval, application accessibility, required configuration, dependency availability, and required smoke or readiness tests. The exact blocking conditions depend on the intended use and risk tolerance of the target environment.

R2. Which validation checks belong immediately after deployment?

Immediately after deployment, validation should check that the application or service started successfully, required endpoints are accessible, required configuration is valid, and critical dependencies are reachable. Where appropriate, a small smoke test can verify a critical workflow.

R3. Could your validation run even if a different deployment tool were used? Explain why or why not.

Yes. The readiness process is independent of the deployment tool because it evaluates the resulting environment rather than the deployment mechanism. Any deployment process could deploy and configure the application, then invoke the same readiness validation and record its results.

## Knowledge check:

1. Can a deployment technically succeed while the environment is unusable? Explain.

Deployment execution can succeed while the resulting environment is still unusable because of configuration, dependency, or accessibility failures.

2. What is artifact promotion?

The same previously built artifact moves through successive environments without being rebuilt.

3. What information establishes deployment provenance?

Deployment provenance is the information that lets us trace exactly what artifact was deployed, where it came from, when and where it was deployed, and by what deployment run or actor.

4. Should environment validation be part of the artifact build? Why or why not?

No, environment validation should not be part of the artifact build. These are two separate concepts that communicate different information according to a specific context. The artifact is prepared/built before a context is given.

5. Why might post-deployment validation be independent of the deployment tool?

Post-deployment validation should be independent of the deployment tool so the same validation can be reused across different deployment mechanisms and survive a future platform migration. It verifies the resulting environment, not the tool that created it.

6. What is a deployment gate?

A deployment gate is a rule, automated check, or manual approval that must pass before deployment or promotion is allowed to continue.