1) What became clearer this week?
The distinction between implicit and explicit dependencies became much clearer, particularly how build agents interact with both. I also developed a better understanding of dependency locking, the different failure boundaries within a CI pipeline, and how intentionally injecting failures can verify that the pipeline stops at the correct stage and prevents invalid artifacts from being published.

2) What was harder than expected?
Identifying all of the implicit dependencies involved in a build and determining how to make them explicit was less straightforward than I expected.

3) What would I do differently in a production system?
I would introduce stronger security and supply-chain controls around the build, including authentication/authorization for the application, dependency and static-analysis scanning, code review, and dependency-license review. I would also more rigorously document or provision the build-agent environment so the build did not depend on undocumented machine state. For the service itself, I would evaluate controls such as rate limiting and encryption based on its production requirements.

4) What artifact from this week best demonstrates growth?
The CI pipeline, explicit dependency configuration and locking, and the three documented failure experiments best demonstrate my growth. Together they show not only that I can create a working build, but that I can reason about its dependencies, provenance, and failure behavior.