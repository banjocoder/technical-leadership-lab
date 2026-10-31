# Toil Reduction Backlog

This backlog contains the ten tasks, effort estimates, scores, priority ranking, and the detailed proposals for each item.

## Toil Point Record

| ID | Task / context | Frequency | Current effort per occurrence | Current weekly effort | Proposed automation and human work retained | Potential weekly savings |
| --- | --- | --: | --: | --: | --- | --: |
| T01 | Run local endpoint tests, including app startup and test-data preparation | 4/week | 10 min | 40 min | Script the settled procedure and assertions end to end | Up to 40 min; oversight not yet estimated |
| T02 | Run local database failure/recovery test | 4/week | 15 min | 60 min | Script outage, requests, restoration, and assertions; prove restoration on failure | Up to 60 min; oversight not yet estimated |
| T03 | Aggregate and document lab results | 8/week | 5 min | 40 min | Automatically collect evidence and calculate results; retain a 5-minute weekly review | **35 min** |
| T04 | Create baseline quotes across 8 LOBs × 4 states | 5–20 quotes/week | 5 min | 25–100 min | Use existing Playwright setup for baseline creation; retain ~1 min of scenario adjustments | **20–80 min** |
| T05 | Manually deploy to DEV | 1/week | 30 min active attention | 30 min | Automate execution through Azure DevOps; retain exception handling | Up to **30 min** |
| T06 | Validate DEV after deployment | 1/week | 10 min | 10 min | Automated checks plus remaining manual validation | **5 min** |
| T07 | Certify QA/UAT using browser smoke tests | Assumed 1 deployment per environment/week | 15–30 min | 30–60 team min | Same automated procedure across environments; retain manual checks | **20–50 team min** |
| T08 | Assess whether incoming tickets are workable | 20 tickets/week across 3 people | 5–10 min | 100–200 team min | Repository/customer-informed AI analysis; retain developer decision and 1–2 min review | **60–180 team min** |
| T09 | Reproduce defects locally, after test data exists | ~10 defects/week across team | ≤10 min | ≤100 team min | At least 80% of scenarios remain manual; automate reusable supporting steps | Not established |
| T10 | Write QA test plans for defects and features | 20 tickets/week across team | 5–10 min | 100–200 team min | AI-assisted drafting; retain review of expected behavior and coverage | **50–100 team min** |

## Toil Point Scorecard

| ID  |Task| Classification | Frequency | Time | Human judgment | Automation potential | Automation risk | Main consideration |
| --- |--| --- | --: | --: | --: | --: | --: | --- |
| T01 |Run local endpoint tests, including app startup and test-data preparation| Toil | 3 | 2 | 1 | 5 | 2 | Bounded local writes; explicit assertions |
| T02 |Run local database failure/recovery test| Toil | 3 | 3 | 1 | 5 | 3 | Deliberate interruption; confirmed local target and recovery required |
| T03 |Aggregate and document lab results| Mixed | 4 | 2 | 3 | 4 | 2 | Automate calculations; retain interpretation and detect missing evidence |
| T04 |Create baseline quotes| Mixed | 3–4 | 2 | 2 | 4 | 2 | Existing Playwright helps; 32 baselines need maintenance |
| T05 |Manually deploy to DEV| Toil | 2 | 3 | 1 | 5 | 4 | Shared-environment changes and deployment recovery |
| T06 |Validate DEV after deployment| Mixed | 2 | 2 | 2 | 4 | 3 | Incorrect checks could falsely certify an unusable environment |
| T07 |Certify QA/UAT using browser smoke tests| Mixed | 3 | 3 | 3 | 4 | 3 | Other teams own execution; manual checks remain |
| T08 |Assess whether incoming tickets are workable| Judgment-heavy | 4 | 2 | 5 | 3 | 3 | AI supports assessment; developer retains readiness decision |
| T09 |Reproduce defects locally, after test data exists| Judgment-heavy | 4 | 1–2 | 5 | 2 | 2 | Unique scenarios limit automation; exclude quote-creation time |
| T10 |Write QA test plans for defects and features| Mixed | 4 | 2 | 4 | 4 | 3 | Generated plans can omit important cases or assert incorrect expectations |

## Toil Point Priorities

| ID  | Task                                                                        | Priority  | Reasoning |
| --- | ---                                                                         | ---       | ---       |
| T04 |Create baseline quotes                                                       |     1     | Your selected candidate: existing Playwright capability, defined acceptance signal, and estimated eight-week break-even.          |
| T07 |Certify QA/UAT using browser smoke tests                                     |     2     | Shared checks across environments; saves an estimated 25–55 team minutes weekly and improves confidence in handoffs.        |
| T06 |Validate DEV after deployment                                                |     3     | Shared checks across environments; saves an estimated 25–55 team minutes weekly and improves confidence in handoffs.       |
| T05 |Manually deploy to DEV                                                       |     4     | Complements certification and enables more frequent feedback. Shared-environment changes make recovery and ownership important.          |
| T01 |Run local endpoint tests, including app startup and test-data preparation    |     5     | Clear assertions, low operational risk, and up to 40 minutes of gross weekly savings during active lab work.          |
| T03 |Aggregate and document lab results                                           |     6     | Builds naturally on automated checks; estimated 35 minutes saved per active lab week. Capture structured evidence while implementing earlier items.|
| T02 |Run local database failure/recovery test                                     |     7     | Repeatable and valuable, but deliberately disruptive. Keep it local and prove cleanup before expanding its scope.|
| T10 |Write QA test plans for defects and features                                 |     8     | Potentially saves 50–100 team minutes weekly, but needs review for missing coverage and incorrect expected behavior.|
| T08 |Assess whether incoming tickets are workable                                 |     9     | Large projected savings, but readiness depends heavily on business judgment. Continue the existing initiative with human approval and measured results.|
| T09 |Reproduce defects locally, after test data exists                            |     10    | Unique scenarios limit repeatability. Automate common supporting steps while leaving reproduction and interpretation primarily manual.|
