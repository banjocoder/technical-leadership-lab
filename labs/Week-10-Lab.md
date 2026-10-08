# Week 10 Lab: Technical Leadership Simulation

Weeks 1-9 established a PolicyService delivery system, reproducible artifacts, environment readiness, architecture decisions, observability, failure analysis, reliability targets, independent certification, and a decision-oriented proposal.

Week 10 asks a different question:

> Can you help a mixed audience make a defensible reliability decision, demonstrate the evidence behind it, and change your recommendation when a challenge reveals a weakness?

You will assemble a capstone architecture packet, rehearse a 15-minute review, answer skeptical questions, and revise the design using feedback. Follow the Week 5 pattern: reason first, prepare a small working demonstration, inspect evidence, revise, and reflect. This walkthrough supplies scaffolds and acceptance criteria; the recommendation and assessment remain your work.

**Workbook alignment:** Technical Leadership Study Workbook, Week 10, pages 31-33; Final Capstone Assessment, pages 34-35; Week 10 knowledge-check comparison points, page 41; personal review cadence and program completion, page 43. Suggested time: **6-8 hours**.

## Learning outcomes

By the end of this lab, you should be able to:

- Present architecture and tradeoffs to mixed technical/non-technical stakeholders.
- Answer skeptical questions concisely.
- Connect implementation decisions to organizational capability.
- Produce a portfolio-quality capstone package.

## Before you begin

This lab assumes you completed Weeks 1-9 in your own `technical-leadership-lab` repository. Gather the actual artifacts you produced:

- Week 1 delivery-system map.
- Week 2 reproducible build, artifact identity, and immutability decision.
- Week 3 deployment/readiness evidence and artifact-promotion model.
- Week 4 architecture views and ADRs.
- Week 5 Observability Contract and correlated healthy/failure telemetry.
- Week 6 failure-mode analysis and ranked reliability backlog.
- Week 7 Reliability Scorecard, SLIs/SLOs, and toil backlog.
- Week 8 certification prototype, machine-readable history, deployment-independent invocation evidence, and ADR-004.
- Week 9 executive proposal, options comparison, and concise decision request.

Use your real filenames, interfaces, and commands. These are an inventory of expected prior work, not a claim that every feature exists in your repository. Label missing capabilities honestly and repair gaps needed for the demonstration before describing them as implemented.

The **PolicyService Delivery System** is the working slice you can demonstrate. The capstone company described below is a scenario for evaluating adoption across many applications. A successful one-service demonstration does not prove readiness for all 30 applications.

You may submit a Markdown **review packet** instead of slides. The workbook accepts a deck/packet; creating a presentation file is optional. This week does not require building 30 environments or replacing your deployment platform.

## How to work through the week

| Work block | Suggested time | Result |
| --- | --- | --- |
| Reference review, inventory, and problem framing | 50 minutes | Evidence map and bounded decision |
| Architecture, reliability, ownership, and rollout synthesis | 100 minutes | Coherent capstone models |
| Working demonstration and evidence capture | 60 minutes | Repeatable healthy/failure/recovery run |
| ADR review, executive proposal, and review packet | 75 minutes | Decision-ready draft |
| Timed review, skeptical challenges, revision, and critique | 80 minutes | Feedback-tested final packet |
| Final assessment, knowledge check, reflection, and closeout | 55 minutes | Completed portfolio record |

The plan totals seven hours. Repairing incomplete earlier work may take longer. Checkpoints occur at larger milestones; resolve conflicting claims before polishing the packet.

## Required reading and reference review

Use the three resources named by the workbook:

1. [Azure Well-Architected Framework learning path](https://learn.microsoft.com/en-us/training/paths/azure-well-architected-framework/) - use selected relevant material as a final architecture quality checklist.
2. [C4 Model](https://c4model.com/) - review abstraction and diagram audience.
3. [Google SRE Workbook](https://sre.google/workbook/table-of-contents/) - review reliability and operational sections relevant to your design.

Record the specific sections reviewed in `docs/week-10/README.md`. For each resource, record one question it raised, the artifact you inspected, and a decision you confirmed or revised. Week 10 lists reference/review resources rather than a new timed tutorial; avoid repeating entire learning paths without a reason.

## Portfolio artifacts

Follow your repository conventions; these paths are suggested:

| Path | Purpose |
| --- | --- |
| `docs/week-10/README.md` | Reading record, scenario, artifact index, and reproduction instructions |
| `docs/week-10/capstone-review.md` | Required review packet with seven timed sections and explicit decision request |
| `docs/week-10/architecture.md` | Context, deployment, validation, CI/CD, observability, and ownership models |
| `docs/week-10/reliability-and-rollout.md` | SLIs/SLOs, failure response, rollout stages, and migration criteria |
| `docs/week-10/executive-proposal.md` | Required two-page executive proposal |
| `docs/week-10/challenge-log.md` | Timed skeptical questions, responses, feedback, and revisions |
| `docs/week-10/experiments.md` | Healthy/failure/recovery demonstration and repeatability evidence |
| `docs/week-10/evidence/` | Sanitized certification records, telemetry, and relevant run outputs |
| `docs/week-10/final-assessment.md` | All eleven evidence items and six scored dimensions |
| `docs/week-10/retrospective.md` | Knowledge check, four reflections, and weekly retrospective |
| `docs/week-10/thinking-changed.md` | One-page Week 1-to-Week 10 comparison |
| Existing ADR directory | Three selected, current architecture decision records |

Link to earlier documents when they remain authoritative. Do not copy them into Week 10 solely to fill the table. Keep a frozen evidence reference or commit identifier when a later change could make a claimed result difficult to reproduce.

---

## Step 1: Frame the capstone problem and requested decision

Use the workbook's Final Capstone Assessment scenario:

> A company has 30 independently customized customer applications. Deployment methods vary between teams. QA frequently discovers broken environments after deployment. Some configuration changes occur outside source control. Leadership wants to standardize deployment eventually, but migration will take 18 months.

### A. Separate facts, unknowns, and assumptions

Complete this table before selecting a solution:

| Category | What you know or need to establish | Consequence for the decision |
| --- | --- | --- |
| Scenario facts | Record the stated facts | Explain the constraint or outcome affected |
| Missing baseline | Identify unknown frequency, duration, impact, or costs | Define how a pilot would measure it |
| Application variation | Identify likely differences requiring investigation | Explain what must be configurable or validated |
| Out-of-source-control configuration | Define what can be observed and what remains untracked | Identify residual uncertainty |
| Organizational capacity | Identify who could operate the control and approve adoption | Identify authority or capacity still needed |
| Lab evidence | Cite actual PolicyService behavior | Explain the limits of extrapolation |

Do not turn “QA frequently discovers broken environments” into an invented incident count, savings figure, or SLO. Proposed targets are choices; measured results require evidence.

### B. Define the decision boundary

Write a problem statement of approximately 100 words that distinguishes symptoms, causes or suspected causes, constraints, and the desired outcome. Identify which causes your evidence supports and which need further investigation.

Then draft a decision request using this scaffold:

```markdown
## Decision requested

- Authority: [role that can approve this]
- Choice: [specific approval, rejection, or selection]
- Scope: [applications/environments and explicit boundary]
- Resources: [people, access, time, and cost assumptions]
- Accountable owner: [role; proposed or confirmed]
- Immediate next action: [who does what after the decision]
- Review point: [when evidence will support expansion, revision, or stopping]
- Success and stop criteria: [measurable conditions]
```

You will revise this request after the challenge exercise. Do not imply that a simulated stakeholder has actually authorized a company initiative.

### C. Map prior evidence to the scenario

For each earlier artifact, record whether it is **implemented and observed**, **designed but unverified**, or **proposed for broader adoption**. Add a link and one limitation. A diagram is design evidence; it is not proof that the depicted system runs.

**Checkpoint: grounded decision.** A reviewer can distinguish the scenario, your working slice, unknown organizational facts, and the concrete choice you want made.

## Step 2: Assemble architecture views for their audiences

Create or reconcile the views in `architecture.md`. Reuse Week 4 models where appropriate, but check them against the current system. Label each view as current, proposed, or a clearly marked combination.

### A. System context diagram

Show the relevant people, the system boundary, and external systems. Start from PolicyService Delivery System and explain how it relates to the broader customer-application scenario.

Ask:

- Who changes software, approves use, consumes evidence, and responds to failures?
- Which systems build, deploy, validate, and store evidence?
- Which relationships represent organizational responsibilities rather than runtime calls?

Keep implementation classes and methods out of this view. Add a short caption explaining which decision the diagram helps its audience make.

### B. Deployment architecture

Show actual execution locations and the proposed adoption boundary: application environments, build/deployment agents, validation execution, dependencies, and evidence storage as applicable.

Identify where identity and configuration enter, which trust or access boundary a check crosses, and how the same artifact moves between environments. Show DEV and QA separately if that matches your existing views. A proposed production path must be labeled as proposed.

If the current runners are ephemeral, say so. Explain how evidence survives teardown. If R2 storage was selected in an ADR but has not been implemented, depict its proposed status and name the storage used by the demonstration.

### C. Validation architecture

Describe the complete certification path:

**Invocation -> target and deployment identity -> checks -> overall decision -> persisted result -> consuming actor/system**

Use a diagram or a view with a companion table. Describe the following:

| Design concern | Decision to make explicit |
| --- | --- |
| Inputs and identity | Expected environment/deployment identity and how observed identity is compared |
| Check boundaries | Reachability, database, schema, configuration, version, dependency, and synthetic transaction coverage |
| Decision policy | Blocking/advisory checks, overall outcome, and handling incomplete results |
| Independence | Stable invocation contract usable by different deployment mechanisms |
| Revalidation | Whether and how checks run without a new deployment |
| Evidence | Timestamp, identity, check details, policy version, and history location |
| Failure of validation | Timeout, unreachable target, unavailable validator, missing history, or stale evidence |
| Side effects | Synthetic data isolation, cleanup, credentials, and check permissions |

An absent or stale certification is not automatically a pass. Explain the policy you selected and who can authorize an exception. Distinguish detection from an enforced deployment gate: if a manual deployment can bypass the consumer, state that limitation.

### D. CI/CD model

Document the path from source change to verified environment. Distinguish build tests, deployment execution, certification, and promotion policy. Explain how the immutable artifact identity survives the path and how configuration changes outside source control affect confidence.

For the scenario's mixed deployment methods, show the common validation contract and any method-specific adapter. Avoid claiming Azure DevOps is implemented if your working slice uses GitHub Actions or a simulation; name each status accurately.

### E. Observability model

Link the Week 5 contract, then explain how request telemetry, deployment evidence, and certification history connect through shared identity. Show which operational question each signal answers.

Include collection/retention/access ownership, diagnostic delay, cardinality limits, and how missing telemetry is recognized. Keep runtime telemetry and delivery/certification evidence distinguishable even if they share storage or identifiers.

For every view, add one quality attribute it supports and one limitation. Check that component names and relationships agree across views.

## Step 3: Connect reliability, ownership, and migration

### A. Refine the reliability metrics

Bring forward the Week 7 scorecard and Week 6 failure analysis. Define metrics that evaluate outcomes as well as check execution.

Use this scaffold; select targets yourself:

| SLI | Operational meaning and formula | Population/window | Source | Proposed SLO | Owner and action on breach | Evidence limitations |
| --- | --- | --- | --- | --- | --- | --- |
| [name] | [numerator/denominator or duration definition] | [eligible observations and period] | [record fields/query] | [target] | [role/action] | [gaps] |

Potential questions to turn into metrics:

- How often does a delivered environment meet required readiness conditions?
- How quickly are blocking failures detected after deployment?
- How often does QA encounter a blocking problem after certification passed?
- How reliably does certification itself execute and produce usable evidence?

These are candidate questions, not mandatory numerical targets. Explain denominator exclusions, retries, duplicate runs, missing results, and zero observations. Do not equate a synthetic success rate with actual customer transaction availability.

For any error-budget policy, state the SLO/window and how budget consumption changes actions. Small deterministic lab samples verify calculations and event handling; they do not establish long-term SLO compliance or business savings.

Choose at least two high-priority failure modes from your existing analysis. For each, connect prevention, detection, operational response, recovery, and remaining risk. Include validator failure or unavailable/stale evidence as one reviewed failure concern.

### B. Make ownership operational

Complete an ownership table. Use roles if real organizational authority has not been confirmed.

| Capability | Accountable role | Operator/responder | Decision rights | Escalation path | Ongoing capacity needed | Status |
| --- | --- | --- | --- | --- | --- | --- |
| Application and environment | To define | To define | To define | To define | To estimate | Proposed/confirmed |
| Deployment integration | To define | To define | To define | To define | To estimate | Proposed/confirmed |
| Certification service and policy | To define | To define | To define | To define | To estimate | Proposed/confirmed |
| Schema/configuration expectations | To define | To define | To define | To define | To estimate | Proposed/confirmed |
| Synthetic workflow and cleanup | To define | To define | To define | To define | To estimate | Proposed/confirmed |
| Evidence/telemetry storage | To define | To define | To define | To define | To estimate | Proposed/confirmed |
| QA handoff and exceptions | To define | To define | To define | To define | To estimate | Proposed/confirmed |

Resolve what happens after a failure: who investigates, who fixes, who recertifies, and who permits QA work or promotion. A team name without an action or decision right is incomplete ownership.

Connect one implementation choice to organizational capability. For example, examine whether adding application-specific checks requires centralized service changes or an application team can contribute them through a reviewed contract. Explain the staffing and maintenance consequence of your choice.

### C. Design an incremental rollout and migration

Create a stage table:

| Stage | Scope | Entry condition | Work and accountable role | Evidence/exit criteria | Recovery or stop condition |
| --- | --- | --- | --- | --- | --- |
| Baseline and discovery | To choose | To define | To define | To define | To define |
| Bounded pilot | To choose | To define | To define | To define | To define |
| Evidence-led expansion | To choose | To define | To define | To define | To define |
| Coexistence during platform migration | To choose | To define | To define | To define | To define |
| Integration transition and retirement | To choose | To define | To define | To define | To define |

Consider an advisory/shadow period before enforcing gates, but choose and justify your actual policy. Describe which reliability improvement can begin during the 18-month migration and which capability depends on the new platform.

Identify what must survive migration: service continuity, artifact/environment identity, certification policy, evidence history, recovery options, and measurable acceptance criteria. Explain when an old integration may be retired and how a new one will demonstrate equivalent behavior.

Changing or disabling a gate is distinct from recovering the application. Include a recovery path for application/configuration changes, and explain schema or synthetic-data changes that cannot safely be reversed automatically.

**Checkpoint: coherent operating model.** Another engineer can follow a failed result to a responder, a decision, and a recovery action. Leadership can see a bounded first step and what evidence governs expansion.

## Step 4: Run and explain the working slice end-to-end

Create `experiments.md` with reproducible commands using your actual Week 8 interface. Include prerequisites, artifact identity, target environment, expected checks, evidence paths, and restoration steps. Do not invent a command for a tool you have not built.

### A. Capture a healthy baseline

Run the existing delivery path and certification. Save deployment results, per-check and overall certification results, observed identity, and relevant telemetry. Explain why the evidence supports the declared readiness boundary.

### B. Inject one reversible fault

Choose an existing, safe Week 6/8 failure case in a disposable lab environment, such as unavailable database connectivity or mismatched expected deployment version. Write the expected failed check and consuming-system behavior before running it.

Record the deployment outcome separately from certification. Verify that required failures do not yield a misleading overall pass and that the selected consumer follows the documented policy. If enforcement is only proposed, say what the experiment observed and what remains unverified.

### C. Restore and rerun

Remove the fault, rerun certification, and retain recovery evidence. Keep deployment/artifact identity honest: a configuration-only fault can preserve artifact identity, whereas rebuilt code requires a new identified artifact.

### D. Verify independent invocation and repeatability

Reuse Week 8 evidence that the same certification contract works after Azure DevOps, PowerShell, or simulated manual deployment. Clearly distinguish actual integrations from simulations. Repeat a manual invocation now and explain whether it needs the original deployment job to remain alive.

Ask a peer to follow the instructions, or perform an independent rerun from a fresh session. Record missing prerequisites, unexpected outputs, and fixes. Retain evidence before ephemeral jobs disappear.

| Run | Artifact/environment identity | Deployment outcome | Check/overall outcome | Consumer behavior | Evidence reference | Limitation |
| --- | --- | --- | --- | --- | --- | --- |
| Healthy baseline | To record | To observe | To observe | To observe | To link | To explain |
| Controlled fault | To record | To observe | To observe | To observe | To link | To explain |
| Recovery | To record | To observe | To observe | To observe | To link | To explain |
| Independent rerun | To record | To record if applicable | To observe | To observe | To link | To explain |

Finish with a short explanation of what the slice demonstrates, what it cannot establish across 30 applications, and what an adoption pilot should test next.

## Step 5: Select and reconcile three ADRs

The final assessment requires **three architecture decision records**. Select three substantial existing decisions or create a record for a genuinely new decision. You do not need three new ADRs solely because it is Week 10.

Candidates include immutable artifacts, independent validation, evidence persistence, or a significant adoption/control policy. Week 4 and Week 8 independence decisions may overlap; preserve their history and clarify scope or supersession instead of presenting duplicate text as two distinct decisions.

For each selected ADR, verify:

- Context describes the current constraint and decision scope.
- Alternatives are credible, including consequences of deferral where relevant.
- The selected option has a rationale and acknowledged disadvantages.
- Operational ownership and migration consequences are explicit.
- Status and evidence agree; a proposed choice is not labeled as accepted by real stakeholders.

Create an index linking the three records and the view, experiment, or proposal section each supports. Renumbering earlier ADRs is unnecessary.

## Step 6: Produce the two-page executive proposal

Adapt the Week 9 proposal to the capstone scenario and the evidence you now have. Use the following structure:

1. Problem and desired outcome.
2. Evidence, including limits and missing baseline.
3. Constraints, including application diversity, untracked changes, capacity, and the 18-month migration.
4. Options and their consequences.
5. Recommendation and why it fits those constraints.
6. Tradeoffs and residual risk.
7. Rollout and accountable ownership.
8. Measurable success/stop criteria and explicit decision requested.

Retain a credible comparison of **do nothing, add independent validation, standardize current deployment, and replace the deployment platform**. Explain coexistence where appropriate; these approaches need not be mutually exclusive over time. You choose the recommendation.

Move commands, payload schemas, package details, and full diagrams into linked appendices unless they materially change the requested decision. Distinguish budget/staffing estimates from measured costs.

Markdown has no fixed pagination. Preview or print the proposal using a consistent readable page size, font, and margins; verify it fits two pages. Keep the Markdown as the authoritative portfolio artifact and record the preview settings. Do not claim “two pages” based on word count alone or make it unreadable to fit.

**Checkpoint: decision-ready package.** The proposal presents options, the packet and ADRs agree, and the demonstration supports the implemented claims. A reader can identify the decision without reading an appendix.

## Step 7: Prepare the exact 15-minute architecture review

Create `capstone-review.md` with these workbook allocations:

| Section | Duration | Cumulative finish | Audience purpose |
| --- | --- | --- | --- |
| Problem | 2 minutes | 02:00 | Explain impact, constraints, and desired outcome |
| Current Architecture | 2 minutes | 04:00 | Show where reliability evidence/control is missing |
| Proposed Architecture | 3 minutes | 07:00 | Explain boundaries and the working slice versus target design |
| Decisions/Tradeoffs | 3 minutes | 10:00 | Compare options and justify consequential choices |
| Reliability Model | 2 minutes | 12:00 | Explain failure response, SLIs/SLOs, and evidence limits |
| Migration Plan | 2 minutes | 14:00 | Show incremental adoption, continuity, and recovery |
| Decision Requested | 1 minute | 15:00 | State authority, choice, scope, resources, and next action |

For each section, write one key message, the supporting view/evidence link, and concise speaker notes. Use the following scaffold repeatedly:

```markdown
## [Section] — [time allocation]

- Key message: [one sentence]
- Evidence/view: [link]
- Speaking points: [short outline in your own words]
- Transition: [connection to the next decision-relevant point]
- If asked for detail: [appendix reference]
```

Present a context view and business consequence to non-technical stakeholders; use deployment and validation detail when it explains risk or responsibility. Explain terms such as artifact, certification, and SLO at first use. Use the diagrams to clarify a decision, not to narrate every box.

Rehearse with a timer and record actual section times. Keep questions after the timed review so interruptions do not hide whether the seven-part presentation fits. If a live demonstration is included, count it within the relevant section; retain saved evidence if the runtime is unavailable.

Trim repeated detail or move it to the appendix when time runs over. Do not silently shorten the decision request to compensate for an overly detailed architecture section.

## Step 8: Invite skeptical challenges and answer in under 60 seconds

Ask another person or ChatGPT to act as a skeptical review panel. Provide the draft packet, proposal, and relevant evidence. A useful practice set is eight questions spanning the roles below; the workbook does not mandate a question count.

Use this prompt if working with ChatGPT:

```text
Act as a skeptical architecture review panel for my Week 10 capstone.
Use the packet and evidence I provide. Alternate perspectives: executive
sponsor, QA lead, platform owner, application engineer, and operations owner.
Ask one question at a time and wait for my answer. Challenge evidence,
ownership, scope, migration dependencies, cost, and failure behavior.
Do not give me a model answer first or assume missing features exist.
I will time each spoken response and report its duration. Then critique
whether I answered the actual question, used evidence, acknowledged limits,
and stated an action. Continue with one targeted follow-up when warranted.
Help distinguish a real design weakness from a communication problem.
```

For text-only practice, write the answer and read it aloud while timing it. A text exchange alone does not verify a spoken response is under 60 seconds. Mark each answer as timed or unverified.

Challenge areas to prepare for, without scripting the answers:

| Perspective | Example challenge |
| --- | --- |
| Executive sponsor | Why invest now when the platform will change in 18 months? |
| QA lead | Certification passed, but QA still cannot issue a policy. What happens next? |
| Platform owner | The pipeline already has tests. What evidence does this layer add? |
| Application engineer | How will one contract accommodate 30 customized applications? |
| Operations owner | Who acts when certification itself is unavailable at handoff? |
| Process advocate | Why not solve this through training and deployment checklists? |
| Finance/sponsor | What measurable result justifies expansion, and what would make you stop? |
| Governance reviewer | Who may override a failed check, and how is that decision recorded? |

Use a response structure: direct answer, relevant evidence/tradeoff, uncertainty, next action. If you do not know, identify the missing evidence and how you would obtain it.

Record every challenge:

| ID/role | Question | Response summary | Actual duration | Evidence cited | Feedback | Weakness type | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- |
| To record | To record | To summarize | To measure | To link | To record | Design/evidence/communication | To choose |

Retain an overlong first answer, revise it, and time a second attempt. The final practiced response to each recorded challenge must be **under 60 seconds**. Follow-up questions are separate turns, not a way to disguise a long answer.

## Step 9: Revise, rehearse again, and critique the final review

Before changing the design, classify the feedback:

- **Design weakness:** behavior, ownership, boundary, or policy is inadequate.
- **Evidence weakness:** a claim exceeds what has been verified.
- **Communication weakness:** the design is defensible but the audience cannot understand the consequence or requested choice.

Complete a revision record:

| Challenge ID | Weakness and consequence | Change or reason to retain decision | Affected artifacts | Verification | Remaining risk |
| --- | --- | --- | --- | --- | --- |
| To identify | To explain | To decide | To link | To record | To assess |

Revise architecture or the proposal whenever a challenge reveals a real weakness. Update affected ADRs, diagrams, rollout criteria, ownership, and the executive request together. Rerun the affected demonstration if behavior changes; documentation-only improvements need a consistency review.

Do not force an architectural change to satisfy a count. If the reviewer finds no real design weakness, record the review result and make any justified evidence/communication improvement. Avoid using that possibility to dismiss difficult feedback without investigation.

Deliver the final timed 15-minute review. Recording is optional under the workbook's “if useful” instruction. If you record, review the playback; otherwise use a reviewer or a timed self-review with written notes.

Critique all four dimensions explicitly:

| Dimension | Specific observation | Improvement | Evidence of the second attempt |
| --- | --- | --- | --- |
| Clarity | To record | To change | To cite |
| Pace | To record | To change | To cite |
| Jargon | To record | To change | To cite |
| Decision framing | To record | To change | To cite |

**Checkpoint: judgment under challenge.** Final answers are timed below 60 seconds, real weaknesses have a documented response, and the final packet remains consistent with the observed slice.

## Step 10: Complete the final capstone assessment

### A. Verify all eleven required evidence items

Create this checklist in `final-assessment.md`. Replace each placeholder with an actual relative link and acceptance observation.

| Workbook evidence item | Artifact link | Acceptance observation |
| --- | --- | --- |
| System context diagram | To link | Actors, boundary, external systems, and audience are clear |
| Deployment architecture | To link | Execution locations, environments, identity, and current/proposed status are clear |
| Validation architecture | To link | Inputs, checks, decision, history, consumers, and failure behavior are defined |
| CI/CD model | To link | Build, promotion, deployment, certification, and policy are distinguishable |
| Observability model | To link | Signals correlate to request/deployment identity and answer operational questions |
| Reliability metrics (SLIs/SLOs) | To link | Formula, population, window, source, target, owner, and limits are explicit |
| Ownership model | To link | Accountability, operation, decision rights, escalation, and capacity are defined |
| Rollout strategy | To link | Pilot, expansion, coexistence, transition, recovery, and exit criteria are defined |
| Three architecture decision records | Three links | Distinct consequential decisions have options, rationale, status, and consequences |
| Two-page executive proposal | To link | Readable two-page preview verified; options and explicit request are present |
| 15-minute architecture review with an explicit decision request | To link | All seven allocations used; actual timing and final request are recorded |

### B. Apply all six scoring dimensions

Use the workbook's levels below. Assess the demonstrated evidence, not the effort spent. For each dimension, choose a level, cite evidence, explain the choice, and record one remaining improvement. A numeric score is unnecessary; the workbook supplies qualitative levels.

| Dimension | Developing | Strong | Excellent |
| --- | --- | --- | --- |
| Problem framing | Describes symptoms | Separates causes, constraints, and outcomes | Frames the decision and identifies residual risk |
| Architecture | Shows components | Shows boundaries, flows, and deployment model | Connects architecture to quality attributes and migration |
| Reliability | Lists checks | Defines failure modes and recovery | Defines measurable targets and validates assumptions |
| Tradeoffs | Presents one solution | Compares credible options | Explains why the recommendation fits current constraints |
| Communication | Technically correct | Clear to mixed audiences | Concise, decision-oriented, and adaptable under challenge |
| Leadership | Completes assigned work | Defines next steps and ownership | Creates a reusable capability others can adopt |

```markdown
## Assessment results

| Dimension | Level | Evidence | Reason | Next improvement |
| --- | --- | --- | --- | --- |
| Problem framing | [level] | [link] | [reason] | [action] |
| Architecture | [level] | [link] | [reason] | [action] |
| Reliability | [level] | [link] | [reason] | [action] |
| Tradeoffs | [level] | [link] | [reason] | [action] |
| Communication | [level] | [link] | [reason] | [action] |
| Leadership | [level] | [link] | [reason] | [action] |

- Assessor and date: [peer, coach, or independent self-review]
- Overall readiness and limitations: [evidence-based assessment]
- Required corrections before submission: [items or none, with rationale]
```

Ask a reviewer to select one architectural claim and follow it to the ADR, operational owner, implementation/evidence, and rollout implication. Record whether the chain holds. Close missing required evidence before marking the capstone complete; do not assert that every dimension must be Excellent when the workbook sets no such pass threshold.

## Step 11: Knowledge check

Answer the workbook's six questions without consulting the answer key. Save your original answers in `retrospective.md` first.

1. Why is “who owns this?” an architecture question as well as an organizational question?
2. How would you justify an independent validation layer if the deployment pipeline already has tests?
3. What is the risk of making a near-term reliability control dependent on a multi-year platform migration?
4. How should you respond when a stakeholder proposes solving a detection problem only with training/process?
5. What does a good migration plan preserve while changing architecture?
6. What is the difference between defending your design and exercising technical judgment?

Then compare with the Week 10 answer key on workbook page 41. Preserve your original answers and record corrections separately. Connect each correction to a concrete decision, challenge, or observation from this lab rather than copying the comparison wording.

## Step 12: Reflection prompts

Use all four workbook prompts:

**R1. Which question exposed the weakest part of your design?**

Identify the challenge ID, the weakness, its consequence, and the evidence that changed your understanding. If no design weakness was found, explain the probing performed and the most uncertain remaining assumption.

**R2. Where did you use too much implementation detail?**

Name the audience and review section. Explain which decision-relevant consequence should replace the detail, and link to what you moved into an appendix or removed.

**R3. What did you change because of feedback?**

Describe the before/after behavior or framing, the feedback source, and how you checked the result. Distinguish design changes from evidence or communication changes.

**R4. What evidence now demonstrates a repeatable technical-leadership capability?**

Point to a reusable contract, reproduction run, decision packet, ownership/rollout model, or challenge-and-revision process. Explain what another team could reuse and what still requires adaptation.

## Step 13: Weekly retrospective and final personal review

Answer the same four retrospective questions used in the earlier weeks:

1. **What became clearer this week?**
2. **What was harder than expected?**
3. **What would I do differently in a production system?**
4. **What artifact from this week best demonstrates growth?**

Keep this learning record separate from the executive proposal and architecture contract.

Complete the workbook's end-of-Week-10 personal review: compare your first-week delivery diagram with your final architecture and write **one page on how your thinking changed** in `thinking-changed.md`.

Include the two diagram references and discuss:

- Which boundaries or distinctions you did not recognize in Week 1.
- How your reasoning about failure, evidence, ownership, and migration changed.
- One decision you would now make differently, and why.
- One remaining limitation and a concrete next learning step.

Verify the one-page length in a readable preview, using the same pagination discipline as the proposal.

Finally, evaluate the program-completion condition: can you structure an ambiguous delivery/reliability problem, build a working slice, explain failure modes and observability, present alternatives, and make a concise recommendation with measurable success criteria? Link evidence for each capability and state remaining gaps honestly.

## Step 14: Repository closeout

Update the root README's weekly roadmap and portfolio index. Link the final packet, proposal, assessment, and reproducible demonstration. Check links from their actual repository locations, including cross-links to earlier weeks.

### Completion checklist

The first six items preserve the workbook's Week 10 checklist; the remaining items verify the exercises and final assessment.

- [ ] I completed the required reading/tutorial selections.
- [ ] The lab runs end-to-end and I can explain the result.
- [ ] The required deliverable is committed to the repository.
- [ ] I can explain at least one tradeoff I made.
- [ ] I completed the knowledge check without consulting the answer key.
- [ ] I wrote a brief weekly retrospective.
- [ ] My packet distinguishes scenario facts, assumptions, implemented capabilities, and proposed adoption.
- [ ] All eleven final assessment evidence items have actual links and acceptance observations.
- [ ] Three distinct ADRs are current and consistent with the recommendation.
- [ ] The executive proposal fits two readable preview pages.
- [ ] The review uses the workbook's seven sections and exact 15-minute allocation.
- [ ] Another person or ChatGPT challenged the draft; final practiced answers are timed under 60 seconds each.
- [ ] Real weaknesses prompted revisions, with rationale and verification recorded.
- [ ] The final review was critiqued for clarity, pace, jargon, and decision framing; recording was used if useful.
- [ ] Healthy, failed, recovered, and independent-invocation evidence can be reproduced.
- [ ] Ownership includes response, escalation, decision rights, maintenance, and exceptions.
- [ ] Rollout addresses the 18-month migration, continuity, recovery, and measurable expansion/stop criteria.
- [ ] All six scoring dimensions have levels, reasons, evidence, and improvement actions.
- [ ] All six knowledge questions and four reflection prompts have my own responses.
- [ ] The one-page Week 1-to-Week 10 comparison is complete.
- [ ] Evidence contains no credentials, raw connection strings, or real customer data.
- [ ] Existing required checks pass for any code/configuration changes; documentation links are verified.

Review changes before staging:

```powershell
git status
git diff --check
git diff
```

If code changed, run the repository's existing locked restore, build, and test checks and repeat the affected experiment. For documentation-only changes, review consistency, Markdown formatting, evidence references, and relative links.

Stage the intended Week 10 files, revised ADRs/views, index changes, and any implementation changes deliberately. Review `git diff --cached` before committing. Exclude private configuration and unrelated changes.

```powershell
# Run after staging the intended files and reviewing the staged diff.
git commit -m "Complete week 10 technical leadership capstone"
git push
```

If you maintain weekly milestone tags, add `week-10` after confirming it does not already exist. A tag or recording is optional; the capstone review packet and completed final assessment are required.

**Week 10 is complete when another person can assess your evidence, understand the requested decision, challenge your reasoning, and identify a repeatable capability that can be adopted beyond the working slice.**

---

## Appendix: Workbook coverage review

This review checks the walkthrough's coverage, not whether a reader has completed the work.

| Workbook requirement | Walkthrough coverage | Evidence the reader must produce |
| --- | --- | --- |
| Present architecture/tradeoffs to mixed stakeholders | Steps 2, 6-9 | Audience-appropriate views, options, and timed review |
| Answer skeptical questions concisely | Step 8 | Challenge log and responses timed under 60 seconds |
| Connect implementation to organizational capability | Step 3 | Ownership/capacity model and explained design consequence |
| Portfolio-quality capstone package | Steps 5-10, 14 | Consistent review packet, proposal, ADRs, and assessment |
| Azure Well-Architected, C4, SRE Workbook resources | Required reading section | Specific review selections and applied observations |
| Hands-on 1: 15-minute review, seven exact allocations | Step 7 | 2/2/3/3/2/2/1-minute outline and actual timing |
| Hands-on 2: person or ChatGPT challenge | Step 8 | Reviewer/panel questions and feedback |
| Hands-on 3: each response under 60 seconds | Step 8 | Measured final responses and retries where needed |
| Hands-on 4: revise real weaknesses | Step 9 | Feedback classification, changes, and verification |
| Hands-on 5: record if useful; critique four dimensions | Step 9 | Clarity, pace, jargon, decision-framing critique; optional recording |
| Required deck/packet and completed final assessment | Steps 7, 10 | Final review packet and completed assessment |
| Completion: readings/tutorial selections | Reading section, Step 14 | Review record and checked completion item |
| Completion: lab runs end-to-end and result explained | Step 4 | Healthy/fault/recovery/repeatability records and scope explanation |
| Completion: required deliverable committed | Step 14 | Repository commit with required artifacts |
| Completion: at least one tradeoff explained | Steps 5-8 | Credible alternatives and consequence of selected choice |
| Completion: knowledge check before answer key | Step 11 | Six original answers and subsequent corrections |
| Completion: brief weekly retrospective | Step 13 | Four retrospective responses |
| Reflection: weakest design question | Step 12, R1 | Challenge, consequence, and evidence |
| Reflection: excess implementation detail | Step 12, R2 | Audience-specific revision and appendix reference |
| Reflection: feedback-driven change | Step 12, R3 | Before/after and verification |
| Reflection: repeatable leadership capability | Step 12, R4 | Reusable evidence and adoption limits |
| Knowledge questions 1-6 | Step 11 | All six workbook prompts preserved without supplied answers |
| Capstone scenario: 30 applications, varied deployment, QA breakage, untracked configuration, 18 months | Steps 1-3, 6 | Fact/assumption map and bounded recommendation |
| Assessment: system context diagram | Step 2A | Context diagram and caption |
| Assessment: deployment architecture | Step 2B | Execution/environment view |
| Assessment: validation architecture | Step 2C | Check/decision/history/consumer model |
| Assessment: CI/CD model | Step 2D | Artifact and control flow |
| Assessment: observability model | Step 2E | Signal/identity/evidence relationships |
| Assessment: reliability metrics, SLIs/SLOs | Step 3A | Definitions, sources, targets, actions, and limits |
| Assessment: ownership model | Step 3B | Accountability, operation, rights, escalation, and capacity |
| Assessment: rollout strategy | Step 3C | Incremental adoption and migration criteria |
| Assessment: three ADRs | Step 5 | Three linked, reconciled decisions |
| Assessment: two-page executive proposal | Step 6 | Proposal and pagination verification |
| Assessment: 15-minute review and explicit request | Steps 1B, 7-9 | Final request and timed presentation |
| Rubric: all six dimensions and three levels | Step 10B | Evidence-based level per dimension and improvements |
| End-of-Week-10 personal review | Step 13 | One-page first-diagram/final-architecture comparison |
| Program-completion condition | Step 13 | Capability-to-evidence evaluation and remaining gaps |

**Review outcome:** All Week 10 requirements, final capstone evidence items, rubric dimensions, and end-of-program review tasks are covered. Recording and presentation-file creation remain optional. Reproduction checks and fault recovery support the workbook's end-to-end requirement without requiring deployment of the full scenario.
