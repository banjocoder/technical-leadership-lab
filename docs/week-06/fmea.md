# PolicyService Failure Mode and Effects Analysis

- Status: Draft
- Date: YYYY-MM-DD
- Workflow and environment: [scope]
- Application artifact/source revision: [identity]
- Analyst/reviewer: [name or role]
- Assumptions and limitations: [scope of evidence]

## Failure Modes

| ID | Component | Failure | Effect | Detection | Recovery | Severity | Occurance | Detection | RPN |
| --- | --- | --- | --- | --- | --- | --: | --: | --: | --: |
| F01 | API host/process | API process down | Callers cannot reach policy endpoints; they may see a timeout or connection error. | [signal, observer, cadence] | [action, owner, verification] |3|3|3|27|
| F02 | Database | Database unavailable | Policy creation may fail at the dependency check. Existing in-memory policies may still be retrievable. | [detection] | [recovery] |4|2|3|24|
| F03 | Application/database contract | Schema mismatch | A previously created policy is lost; GET may return 404 even though the API is responding. | [detection] | [recovery] |5|3|2|30|
| F04 | Configuration | Invalid configuration | The connectivity check may pass without proving that database-backed policy operations would work. | [detection] | [recovery] |4|4|1|16|
| F05 | External dependency/client | Dependency timeout | [effects] | [detection] | [recovery] ||||
| F06 | Partial-workflow failure ||||||||

*S, O, and D are scaled from 1 - 5, with 1 being low and 5 being high*

*RPN is the Risk Priority Number where RPN = S × O × D*

## Risk Ranking

[Scale, scores, rationale, uncertainty, and links to experiment evidence.]

## Cross-cutting Observations

[Common dependencies, detection blind spots, recovery ownership, and tradeoffs.]

## Verification

[Links to predictions, observations, improved detection, and recovery evidence.]
