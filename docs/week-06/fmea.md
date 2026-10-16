# PolicyService Failure Mode and Effects Analysis

- Status: Final
- Date: 2026-10-16
- Analyst/reviewer: Architect

## Failure Modes

| ID | Component | Failure | Effect | Detection | Recovery | 
| --- | --- | --- | --- | --- | --- |
| F01 | API host/process | API process down | Callers cannot reach policy endpoints; they may see a timeout or connection error. | Currently, when a user attempts to reach the api if it is down, they will not be able to access it. The health check and readiness check not responding will expose this issue. They only run once when the application is deployed and after it is started. <br/> We could set up a process or system that periodically pings these endpoints and alerts on a failure. | A basic recovery step at the moment is to start/restart the application service.  |
| F02 | Database | Database unavailable | Policy creation may fail at the dependency check. Existing in-memory policies may still be retrievable. | Readiness check will fail. Attempts to create a policy exposes this as a response and log. | Restart DB service and test policy issuance. |
| F03 | Application/database contract | Schema mismatch | A previously created policy is lost; GET may return 404 even though the API is responding. | Readiness check will fail. Currently when a user attempts to create a policy, if there is a mismatch they will receive an error. | Sync app and DB schema. |
| F04 | Configuration | Invalid configuration | The connectivity check may pass without proving that database-backed policy operations would work. | Readiness check will fail. When a user attempts to create a policy, they will receive a failure message. | Fix configuration issues and restart application. |
| F05 | External dependency/client | Dependency timeout | A policy requires a risk scoring service in order to issue. When the service fails, the user is not able to issue the policy.  | The user receives a 503 error and the logs reveal further detail about this issue. | Recovery may require a vendor fix. |
| F06 | Partial-workflow failure (Optional post-issuance marketing email integration)| Email service rejects the request, becomes unavailable, or times out. | Policy issuance succeeds, and the caller can retrieve the policy. Promotional email delivery fails or remains uncertain; the caller does not receive an email-related error. | In this scenario, only a manual review of the logs will reveal an issue with the email service. | Restore the email dependency, then retry or reconcile the email request using the same GUID. The email service uses that GUID to prevent duplicate processing. Verify the policy remains retrievable and the email outcome is resolved. |


## Risk Ranking

| ID | Impact | Likelihood | Score
|--|:--:|:--:|:--:|
|F01| 3 | 2 |6|
|F02| 2 | 1 |2|
|F03| 2 | 2 |4|
|F04| 2 | 3 |6|
|F05| 2 | 2 |4|
|F06| 1 | 2 |2|

*Impact and Likelihood are scaled from 1 - , with 1 being low and 3 being high. Score = Impact * likelihood*

