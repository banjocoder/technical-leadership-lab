# Proposed Improvements

## F04
Configuration failures often happen due to manual updates or steps in the process. In order to reduce likelihood of configuration issues, automate the configuration and deployment steps involved. 

## F06
Currently email failures are silent and do not disrupt the system flow, but do have an impact on business concerns. We should eventually have a system that behaves in the following manner: 

Trigger: more than five failed or unknown email attempts in a rolling 24 hours.
Recipients: IT and Marketing, through an independent delivery path.
Accountable owner: IT acknowledges, investigates, and coordinates recovery.
Marketing: assesses campaign impact and whether delayed sends remain appropriate.

## Improvements by Priority: 

|Priority|Failure Step|Improvement|Rationale|Verification|
|--|--|--|--|--|
|1|F01|Durable policy storage|The whole reason a customer uses the system is to track policies. If all storage is in memory, then they lose everything as soon as the site goes down.|Create a policy, restart the API, then retrieve that same policy and confirm its ID and contents are unchanged.|
|2|F04|Automate Configuration Steps|Configuration should be consistent and stable, relying on manual steps introduces unneccisary faults. Detection and recovery are also slightly more difficult with this problem|Compare the percentage of deployments requiring configuration correction before and after automation: deployments requiring correction ÷ total deployments × 100. Track over comparable periods; establish a baseline before setting a reduction target.|
|3|F03|Gate promotion on the check passing in the target environment|Environment changes that cuase the schema to be out of sync should not be promoted into subsequent environments|Introduce an incompatible schema in the disposable environment and confirm the promotion gate blocks advancement. Restore compatibility and confirm the gate permits advancement.|
|4|F05| Retry 3rd party services | For 3rd party vendors, we should try at least 3 attempts for idempotent api calls (all recorded).  |A transient failure followed by success creates one policy.<br/>Three failed attempts return a controlled failure and create no policy.<br/>Retries stop within a defined overall time budget, with backoff between attempts.|
|5|F02|Alert and Recovery from DB outage|Detection: an external monitor alerts the responsible IT operator when the required database check fails.<br/> Recovery: attempt an automatic restart only when the database service is confirmed stopped, with bounded attempts and escalation if recovery fails.|Simulate a stopped service and confirm detection, bounded restart attempts, and restored readiness plus policy creation/retrieval. Separately confirm that an offline database produces the appropriate escalation.|
|6|F06|Email service failure persistence|Persist the email-request GUID, policy ID, and outcome so unresolved work survives an API restart.|Cause an email timeout, restart the API, and confirm the unresolved email record remains available with the same request GUID, policy ID, and unknown outcome.|