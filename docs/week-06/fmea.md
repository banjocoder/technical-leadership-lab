# PolicyService Failure Mode and Effects Analysis

- Status: Draft
- Date: YYYY-MM-DD
- Workflow and environment: [scope]
- Application artifact/source revision: [identity]
- Analyst/reviewer: [name or role]
- Assumptions and limitations: [scope of evidence]

## Failure Modes

| ID | Component | Failure | Effect | Detection | Recovery | 
| --- | --- | --- | --- | --- | --- |
| F01 | API host/process | API process down | Callers cannot reach policy endpoints; they may see a timeout or connection error. | Currently, when a user attempts to reach the api if it is down, they will not be able to access it. The health check and readiness check not responding will expose this issue. They only run once when the application is deployed and after it is started. <br/> We could set up a process or system that periodically pings these endpoints and alerts on a failure. | A basic recovery step at the moment is to start/restart the application service.  |
| F02 | Database | Database unavailable | Policy creation may fail at the dependency check. Existing in-memory policies may still be retrievable. | Readiness check will fail. Attempts to create a policy exposes this as a response and log. | Restart DB service and test policy issuance. |
| F03 | Application/database contract | Schema mismatch | A previously created policy is lost; GET may return 404 even though the API is responding. | Readiness check will fail. Currently when a user attempts to create a policy, if there is a mismatch they will receive an error. | Sync app and DB schema. |
| F04 | Configuration | Invalid configuration | The connectivity check may pass without proving that database-backed policy operations would work. | Readiness check will fail. When a user attempts to create a policy, they will receive a failure message. | Fix configuration issues and restart application. |
| F05 | External dependency/client | Dependency timeout | *Not Yet Implemented* A policy requires a risk scoring service in order to issue. When the service fails, the user is not able to issue the policy.  | Currently no implementation exists to detect this. | Recovery may require a vendor fix. |
| F06 | Partial-workflow failure (Optional post-issuance marketing email integration)| Email service rejects the request, becomes unavailable, or times out. | Policy issuance succeeds, and the caller can retrieve the policy. Promotional email delivery fails or remains uncertain; the caller does not receive an email-related error. | *Not Yet Implemented* Internal log or work record links the policy ID, stable email-request GUID, and outcome. Record explicit failure as failed and an ambiguous timeout as unknown. | Restore the email dependency, then retry or reconcile the email request using the same GUID. The email service uses that GUID to prevent duplicate processing. Verify the policy remains retrievable and the email outcome is resolved. |

*S, O, and D are scaled from 1 - 5, with 1 being low and 5 being high*

*RPN is the Risk Priority Number where RPN = S × O × D*

## Risk Ranking

[Scale, scores, rationale, uncertainty, and links to experiment evidence.]

## Cross-cutting Observations

[Common dependencies, detection blind spots, recovery ownership, and tradeoffs.]

## Verification

[Links to predictions, observations, improved detection, and recovery evidence.]
