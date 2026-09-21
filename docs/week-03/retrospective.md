Week 3 Retrospective

A. What became clearer this week?

Artifact promotion and environment readiness became much clearer to me. It was especially helpful to see these concepts working in GitHub Actions and to observe how the same artifact could be promoted through multiple environments while receiving different environment-specific configuration and validation results.

B. What was harder than expected?

The amount of setup and configuration involved in the CI/CD pipeline was more extensive than I expected. None of the individual pieces were overly complicated, but coordinating artifact handling, environment configuration, validation, provenance, deployment gates, and failure handling required more work than I anticipated.

C. What would I do differently in a production system?

In a production system, I would add a rollback strategy for deployments that fail readiness validation. I would also consider rolling or incremental deployment strategies to reduce deployment risk. Environment-specific credentials and other sensitive configuration would be stored using protected environment secrets rather than directly in the pipeline definition.

D. What artifact best demonstrates growth?

The “Deployment Success vs. Environment Readiness” diagram best demonstrates my growth because it clearly communicates the distinction between successful builds, successful deployment execution, and a ready environment, including cases where deployment succeeds but environmental validation fails.