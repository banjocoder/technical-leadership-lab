# Week 9 Lab: Architecture Proposal and Decision Enablement

Weeks 1-8 built a small PolicyService delivery system, observable workflows, reliability experiments, service objectives, and an independent environment certification prototype. Week 9 turns that engineering work into a decision another person can make.

> Leadership asks: “How should we improve deployment reliability?”

Your task is to answer with evidence, credible alternatives, an explicit recommendation, and a bounded next action. A working prototype gives you evidence; it does not automatically establish which investment leadership should choose.

Follow the Week 5 pattern: reason first, produce a small increment, inspect the result, revise it, and reflect. This walkthrough supplies starter material and review criteria. Your recommendation, estimates, tradeoffs, and answers remain your work.

**Workbook alignment:** Technical Leadership Study Workbook, Week 9, pages 28-30. Suggested time: **5-6 hours**. The final appendix records coverage of the learning outcomes, hands-on requirements, deliverables, completion checklist, knowledge check, reflections, and retrospective.

## Learning outcomes

By the end of this lab, you should be able to:

- Write a concise engineering proposal.
- Separate evidence, constraints, options, recommendation, and tradeoffs.
- Move implementation detail into appendices.
- Summarize a proposal verbally in 60 seconds.

## Before you begin

This lab assumes you completed Weeks 1-8 in your own `technical-leadership-lab` repository. Gather your actual artifacts:

- Build, immutable artifact, promotion, and readiness evidence from Weeks 1-3.
- Architecture views and ADRs from Week 4.
- Observability Contract and failed/recovered request records from Week 5.
- Failure Mode and Effects Analysis and ranked reliability backlog from Week 6.
- Reliability Scorecard, SLI/SLO definitions, and Toil Reduction Backlog from Week 7.
- Working certification prototype, persisted results, trigger evidence, and ADR-004 from Week 8.

Use actual paths and filenames. If an artifact or experiment is missing, identify the gap before making a claim that relies on it. You may rerun an existing disposable lab experiment to collect missing evidence; building another deployment platform is not required.

Use the **PolicyService Delivery System** as the proposal's system boundary. State the environment, workflow, and stakeholder roles it covers. If you adapt the exercise to a workplace scenario, separate workplace observations from lab demonstrations and protect private information. Do not present lab failures as measurements of workplace incident frequency or cost.

This is a simulated leadership decision. Reviewing, committing, and presenting your portfolio work does not require contacting real executives or approving a real rollout.

## How to work through the week

| Work block | Suggested time | Result |
| --- | --- | --- |
| Reading review and decision framing | 40 minutes | Audience, question, and reading record |
| Evidence, constraints, and alternatives | 65 minutes | Traceable comparison of four options |
| Two-page proposal and appendix | 75 minutes | Complete recommendation with bounded rollout |
| One-page compression and revision | 50 minutes | Executive version preserving the decision |
| Pitch rehearsal and independent review | 45 minutes | Timed spoken pitch and revision evidence |
| Knowledge check, reflection, and closeout | 45 minutes | Completed portfolio record |

Total: about **5 hours 20 minutes**. Allow additional time if earlier evidence needs repair. Checkpoints occur at larger milestones; resolve gaps before polishing the next artifact.

## Required reading and review selections

Use both resources named by the workbook:

1. [Azure Well-Architected - Operational Excellence](https://learn.microsoft.com/en-us/training/modules/azure-well-architected-operational-excellence/) - review selected units on development standards, automation, observability, and safe deployment practices. Treat them as criteria for comparing options; the lab does not require an Azure migration.
2. [Architecture Decision Records](https://github.com/architecture-decision-record/architecture-decision-record) - review context, alternatives, decisions, and consequences. Apply ADR thinking to the proposal without treating a proposed recommendation as an accepted decision.

In `docs/week-09/README.md`, record the units/sections reviewed, date, and one implication for your comparison from each resource. For example: what would automation reduce, and what would still need ownership? Reuse Week 8 reading notes where useful, but record what you reviewed this week.

## Portfolio artifacts

Follow your repository's established conventions. These paths are suggestions:

| Path | Purpose |
| --- | --- |
| `docs/week-09/README.md` | Scope, reading record, artifact index, page-count and timing verification |
| `docs/week-09/proposal.md` | **Required:** two-page maximum engineering proposal |
| `docs/week-09/executive-summary.md` | **Required:** one-page executive version |
| `docs/week-09/decision-pitch.md` | **Required:** 60-second decision pitch script and rehearsal record |
| `docs/week-09/appendix.md` | Evidence register, detailed comparison, estimates, and implementation details |
| `docs/week-09/review.md` | Initial review, compression changes, feedback, and final acceptance result |
| `docs/week-09/retrospective.md` | Knowledge answers, reflections, and weekly retrospective |

These supporting files are a suggested organization, not additional workbook deliverables. You may combine them when that makes the portfolio easier to use. Keep the required proposal, executive version, and pitch clearly identifiable.

Markdown has no intrinsic page count. In Step 7, choose and record a normal print layout, preview both written versions, and verify their page limits. Word counts are drafting aids, not proof of page count.

---

## Step 1: Frame the decision before describing the solution

Write the workbook's leadership question at the top of your working notes:

> How should we improve deployment reliability?

Define what that means for this system. Choose a concrete outcome, such as reducing time lost to unusable QA environments, detecting a wrong deployment version before testing, or improving recovery from a known failure. Do not treat all reliability problems as one interchangeable problem.

Complete this starter:

```markdown
## Decision Frame

- System and workflow: [boundary]
- Decision maker: [role with authority]
- Affected users: [who experiences the problem]
- Desired outcome: [change in user/operational experience]
- Current failure or uncertainty: [observable problem]
- Scope: [environment, pilot boundary, and exclusions]
- Decision needed: [specific choice]
- Immediate next action if approved: [owner and bounded action]
- Decision timing: [date or trigger, with reason]
- Status: Proposed for simulated review
```

Ask yourself:

- Is the requested choice about a pilot, standardization, investigation, or a platform replacement?
- Does the named role have authority over the resources needed?
- What can that person actually approve after reading one page?
- What would improve for QA or another user if your proposal succeeds?

A generic request to “support reliability” gives the reader too little to act on. A bounded choice identifies what starts, who owns it, and what commitment is needed. Fill those elements with your own scope and estimates.

## Step 2: Build an evidence register

Create a small register in `appendix.md` before writing persuasive prose.

| ID | Claim or observation | Source path and record identity | Evidence type | Limitation | Decision implication |
| --- | --- | --- | --- | --- | --- |
| E1 | [what occurred] | [file, run, version, or trace] | [observed/measured] | [what it cannot prove] | [why it matters] |
| E2 | [estimate or hypothesis] | [calculation or rationale] | [estimated/assumed] | [uncertainty] | [how to validate] |

Use records from your repository to investigate:

- Can deployment complete while certification reports an unusable environment?
- Which known failure modes did your experiments detect? Which remain untested?
- Does the same certification capability work after different deployment mechanisms?
- What manual work does the toil backlog identify, and how was its frequency estimated?
- What do the SLI/SLO and failure records establish about the problem's severity?

For each important claim, distinguish an observation from an inference. A successful lab test demonstrates a capability under tested conditions; it does not prove organization-wide reliability gains. If you lack an incident baseline, propose collecting it during the pilot rather than inventing a savings figure.

Put only the strongest decision-relevant evidence in the proposal. Keep record identities and deeper analysis in the appendix, with links that allow a reviewer to trace claims.

## Step 3: Separate constraints from preferences and unknowns

List constraints that affect feasibility, sequencing, cost, or authority. Avoid adopting workplace restrictions as facts about your personal lab.

| Constraint or unknown | Basis | Effect on options | How to confirm or handle it |
| --- | --- | --- | --- |
| [fixed deployment interface, if applicable] | [observed/documented] | [what cannot change] | [integration boundary] |
| [available engineering capacity] | [estimate/assumption] | [scope or timing effect] | [owner confirmation] |
| [certification operating owner] | [known/unresolved] | [support obligation] | [decision or prerequisite] |

Consider access permissions, operating ownership, environment lifespan, data safety, available people, retention, and platform migration dependencies. Label each entry **confirmed**, **assumed**, or **unresolved**.

Then challenge your list:

- Which item is a real limit, and which is merely your preferred design?
- Which unknown would change the recommendation if resolved differently?
- Does the recommendation depend on another initiative finishing first?
- Can a smaller reversible step generate evidence before a larger commitment?

Preserve decisive constraints in the main body. Do not hide a missing owner or required permission in an appendix.

## Step 4: Compare all four required alternatives fairly

The workbook requires at least these four options. Compare each within the same problem boundary and decision horizon:

| Option | What changes? | Reliability mechanism to assess | Questions to answer |
| --- | --- | --- | --- |
| Do nothing | Continue the current approach | Existing controls and accepted residual risk | What cost or exposure continues? When would you revisit this choice? |
| Add independent validation | Introduce or extend certification outside deployment execution | Evidence about the resulting environment | What can it detect? Who operates it? What happens if validation is unavailable? |
| Standardize current deployment | Improve consistency of the current process | Fewer variations and preventable execution errors | Which failure modes does standardization address? What remains undetected? |
| Replace deployment platform | Migrate deployment execution to another platform | Capabilities and controls gained through replacement | What migration effort, delay, transition risk, and ownership accompany the change? |

Do not declare independent validation the winner simply because Week 8 built it. Identify a credible benefit and drawback for every alternative, including your preferred one. If you recommend a combination or sequence, first assess all four separately, then explain the added cost and dependency of the combination.

Use a detailed comparison in the appendix:

| Criterion | Do nothing | Independent validation | Standardize current | Replace platform |
| --- | --- | --- | --- | --- |
| Addresses demonstrated failure modes | [evidence] | [evidence] | [evidence] | [evidence/unknown] |
| Time to useful outcome | [estimate] | [estimate] | [estimate] | [estimate] |
| Initial effort and required access | [basis] | [basis] | [basis] | [basis] |
| Ongoing operating burden and owner | [assessment] | [assessment] | [assessment] | [assessment] |
| Residual risk | [assessment] | [assessment] | [assessment] | [assessment] |
| Reversibility and migration dependency | [assessment] | [assessment] | [assessment] | [assessment] |

Use qualitative judgments where numbers would imply unsupported precision. If you use scores or weights, explain them and check whether a plausible change in assumptions changes the winner. The main proposal still needs a readable explanation of why the recommendation is preferable.

**Checkpoint: decision foundation.**

You have a bounded decision, traceable evidence, explicit constraints, and all four alternatives. A reader can see what you know, what you assume, and what remains uncertain.

## Step 5: Define the recommendation, tradeoff, rollout, and finish line

Select your recommendation using the comparison. Complete these statements in your own words:

- “I recommend [option and scope] because [evidence and decisive criteria].”
- “We gain [benefit], while accepting [cost, limitation, or risk].”
- “We would reconsider this choice if [new evidence or changed constraint].”

A tradeoff must describe something you give up or accept. “There are risks” is insufficient; state the consequence and who carries it. For independent validation, possible questions include false failures, maintenance effort, decision policy, and whether detection improves before prevention. For replacement, consider the benefits deferred during migration and the transition risk.

### A. Design a bounded rollout

Plan the recommended option, not four implementations. Use phases appropriate to your choice:

| Phase | Scope and owner | Evidence produced | Continue/stop condition | Recovery or exit action |
| --- | --- | --- | --- | --- |
| Baseline | [what you measure first] | [records] | [baseline sufficiency] | [handle missing data] |
| Limited pilot | [environment, duration, capacity] | [results and effort] | [measurable acceptance] | [revert or revise] |
| Review | [decision maker and date/trigger] | [outcome comparison] | [expand/change/stop] | [close or re-scope] |

Treat this as a proposal for future work. Do not claim a rollout occurred because you wrote its plan. Your Week 8 prototype can support the plan, but pilot adoption and ongoing operations are separate commitments.

### B. Make success criteria measurable and bounded

Choose a few criteria that include user impact, technical effectiveness, and operating effort.

| Criterion | Baseline or known gap | Proposed target and window | Measurement source | Owner | Decision if missed |
| --- | --- | --- | --- | --- | --- |
| [user-impact outcome] | [measured value or unknown] | [target] | [records] | [role] | [action] |
| [detection/prevention effectiveness] | [tested coverage] | [target] | [experiment/results] | [role] | [action] |
| [operating burden] | [current effort] | [target] | [time/error records] | [role] | [action] |

Explain proposed targets; do not present them as achieved results. When the baseline is unknown, define a baseline collection period and a review that sets the improvement target. Distinguish a short pilot criterion from a service SLO measured over a longer window. Include an end date or review trigger and explicit expand, revise, or stop outcomes.

### C. Draft one explicit decision request

Use this scaffold, filling every bracket:

> Approve [specific option/action] for [scope and duration], with [owner and capacity/access commitment]. The next action is [concrete step] by [role/date], and we will review [criteria] at [date or trigger] to decide whether to expand, revise, or stop.

Keep one primary approval request. List prerequisites and operational tasks as consequences of that decision rather than unrelated requests competing for attention.

## Step 6: Write the two-page proposal and separate the appendix

Create `proposal.md` with the eight exact decision sections required by the workbook. Add the explicit decision request near the beginning or end so it is easy to find.

```markdown
# Deployment Reliability Proposal

Status: Proposed | Decision owner: [role] | Scope: [boundary]

## Problem
[Who is affected, by what failure, and why it matters now.]

## Evidence
[Two or three supported observations with evidence references.
Label estimates and explain material uncertainty.]

## Constraints
[Limits that shape feasibility, scope, or timing.]

## Options
[All four required alternatives, with a concise benefit and drawback.]

## Recommendation
[Chosen option, scope, reason, and explicit decision request.]

## Tradeoffs
[What improves, what is accepted, and material residual risk.]

## Rollout
[Owner, limited next step, effort, sequencing, and exit/review point.]

## Success Criteria
[Measurable outcomes, window, evidence source, and decision at review.]

Supporting detail: [Appendix](appendix.md)
```

Aim initially for roughly 700-900 words, then verify the rendered **two-page maximum**. Adjust for your tables and chosen print layout. These word counts are drafting guidance only.

In `appendix.md`, organize technical detail under useful headings:

- Evidence register and limitations.
- Detailed option comparison and estimate assumptions.
- Architecture views and ADR links.
- Certification inputs, checks, record structure, and trigger examples.
- Supporting telemetry, failure experiments, scorecard, and toil analysis.

Use links to existing records rather than copying every earlier artifact. Keep status consistent with existing ADRs; a proposed initiative is not approved merely because ADR-004 records a prototype design choice.

For each technical detail, ask: **Could it change the decision, scope, cost, ownership, or accepted risk?** If yes, keep its consequence in the main body. Move the mechanism or implementation detail to the appendix. API routes, JSON fields, command syntax, and package configuration often belong in the appendix; the required operating owner, data risks, and validation failure policy may belong in the proposal.

The appendix may be longer, but it must not become a way to hide decision-critical information outside the page limit.

## Step 7: Verify the page limit and preserve a reviewable draft

Choose one repeatable Markdown rendering or printing workflow. Use the same settings for both versions; for example, US Letter or A4, normal margins, and readable 11-12 point body text. Record the renderer, paper size, font, margins, and resulting page counts in the Week 9 README.

1. Preview or print `proposal.md` by itself, excluding the appendix.
2. Confirm it is at most two pages and that headings/tables are readable.
3. Remove repetition or move implementation detail if it exceeds the limit.
4. Check that no essential section has become a heading with no meaningful content.
5. Preserve this version in Git history or retain a clearly labeled draft for comparison.

A two-page label in a heading or a Markdown page-break marker does not prove compliance. Avoid shrinking the text to make it fit.

**Checkpoint: full proposal.**

The proposal fits the page limit, contains all eight sections, compares all four options, and asks for an actionable decision. Its recommendation follows from evidence and constraints. The appendix supports it without carrying hidden conditions.

## Step 8: Cut to one page without losing the decision structure

Create `executive-summary.md` from the verified proposal. Keep the same eight labeled sections, using short paragraphs or a compact options table. Aim initially for roughly 350-450 words, then verify **one rendered page** using Step 7's settings.

Use three editing passes:

1. **Keep the decision spine:** retain the problem, strongest evidence, decisive constraints, four alternatives, recommendation, real tradeoff, bounded rollout, success criteria, and request.
2. **Cut duplication:** remove background the intended reader already knows, repeated benefits, tool inventories, and explanations that do not change the choice.
3. **Translate the remaining detail:** explain the consequence of a mechanism in language the decision maker can use. Preserve uncertainty and ownership commitments.

In `review.md`, record at least one substantive edit:

| Removed or shortened content | Why the decision remains sound | Where supporting detail lives |
| --- | --- | --- |
| [paragraph or detail] | [reason] | [appendix link, or unnecessary] |

Compare the two versions:

- Is the recommended option and scope identical?
- Is the decision request still explicit?
- Can the executive see the main alternative and cost of choosing your option?
- Are the evidence qualifications, owners, success criteria, and stop/review condition consistent?
- Can someone choose without opening the appendix?

If compression changes your judgment, update both versions deliberately and explain the change. Do not leave contradictory documents.

## Step 9: Create and rehearse the 60-second decision pitch

Draft `decision-pitch.md`. About 110-140 words is a useful starting point; a timed spoken rehearsal determines whether it fits.

| Segment | Approximate time | Purpose |
| --- | --- | --- |
| Problem and impact | 0-10 seconds | Establish why the choice matters |
| Evidence and key constraint | 10-25 seconds | Ground the recommendation |
| Recommendation, alternative, and tradeoff | 25-45 seconds | Explain the judgment |
| One decision request and next action | 45-60 seconds | Make action possible |

Starter outline:

```markdown
# 60-Second Decision Pitch

## Script
[Problem and affected user. Strongest evidence and key constraint.
Recommendation and why it is preferable to the strongest alternative.
Accepted tradeoff. One explicit request, next action, and review point.]

## Rehearsal Record
| Attempt | Duration | What was unclear or omitted? | Revision |
| --- | --- | --- | --- |
| 1 | [seconds] | [observation] | [edit] |
| 2 | [seconds] | [observation] | [edit] |
```

Speak it aloud at a natural pace, time it, revise, and repeat. Deliver the final version within 60 seconds without rushing. A text word count alone does not complete the verbal learning outcome. An audio recording is optional; retain the script and timing record.

You do not need to recite eight headings or all four alternatives in the pitch. The written versions retain them. The spoken version must communicate a coherent choice, its consequence, and **one explicit decision request**.

## Step 10: Run the decision package end-to-end and revise

For this communication lab, “runs end-to-end” means a reader can move from the proposal to a choice and next action, trace the important evidence, and hear the timed pitch. A new software deployment is not required.

Ask a reviewer, coach, or independent self-review to start with the one-page version. Do not explain the answer first. For self-review, put the draft aside briefly and use the questions as a separate evaluation pass.

Have the reviewer answer:

1. What problem is being addressed, and who is affected?
2. Which evidence supports action, and what is still uncertain?
3. What are the four choices and the decisive constraint?
4. What is recommended, and what do we accept or give up?
5. What exactly is the decision maker being asked to approve?
6. Who starts what action, and what is the effort or commitment?
7. When and how will we decide whether it worked?

Then follow one significant claim to its source and listen to the pitch. Capture the result in `review.md`:

| Review item | Finding | Revision or reason no change is needed | Final verification |
| --- | --- | --- | --- |
| [question or claim] | [finding] | [action] | [result] |

Also pose a skeptical objection: why not standardize the existing process or replace the platform instead? Respond using evidence and constraints, not loyalty to the prototype. If the objection reveals a better choice, revise the recommendation.

After revisions, recheck page counts, consistency, links, and pitch timing. Record the review as a simulated result; do not imply actual leadership approval.

**Checkpoint: decision enablement.**

A reader can identify the decision and next action without oral coaching. Important claims are traceable, both written versions fit their page limits, and the final spoken pitch fits 60 seconds.

---

## Step 11: Knowledge check

Answer all six workbook questions without consulting its answer key. Save your original answers in `retrospective.md` before checking them.

1. What is the difference between explaining a technical solution and enabling a decision?
2. What belongs in the main recommendation versus an appendix?
3. Why should alternatives be presented even when you strongly prefer one option?
4. What makes a decision request actionable?
5. Why are constraints important in a technical proposal?
6. How can success criteria protect an initiative from becoming open-ended?

After answering, compare with the workbook's Week 9 answer key on page 40. Preserve your first responses and write corrections in your own words. Connect each correction to a specific sentence, comparison, or review finding from this week's artifacts.

## Step 12: Reflection prompts

Use all three workbook prompts exactly:

**R1. Did your recommendation make the tradeoff obvious?**

Quote the sentence that states what you accept or give up. Explain whether a reviewer noticed it without prompting and how you improved it if needed.

**R2. Which paragraph could be removed without weakening the decision?**

Identify a paragraph from your longer draft and explain why the decision survives its removal. If it contains useful implementation detail, show where you moved it. Use your actual compression record rather than a hypothetical example.

**R3. What question would a skeptical executive ask first?**

Write the question, your concise response, and the evidence or uncertainty behind it. Explain what new information would cause you to change your recommendation.

## Step 13: Weekly retrospective

Answer the same four workbook prompts used in earlier weeks:

1. **What became clearer this week?**
2. **What was harder than expected?**
3. **What would I do differently in a production system?**
4. **What artifact from this week best demonstrates growth?**

Include an observation from compression, review, or the spoken rehearsal. Keep learning notes separate from the proposal so the proposal remains usable by someone who did not participate in the lab.

## Step 14: Repository closeout

Update the root README's weekly roadmap and documentation index with links to the Week 9 overview and three required deliverables. Check relative links from their actual locations, including links to earlier evidence.

### Completion checklist

The first six items preserve the workbook's completion checklist; the remaining items verify this week's specific requirements.

- [ ] I completed the required reading/tutorial selections.
- [ ] The lab runs end-to-end and I can explain the result.
- [ ] The required deliverable is committed to the repository.
- [ ] I can explain at least one tradeoff I made.
- [ ] I completed the knowledge check without consulting the answer key.
- [ ] I wrote a brief weekly retrospective.
- [ ] I framed the leadership question: “How should we improve deployment reliability?”
- [ ] My proposal contains Problem, Evidence, Constraints, Options, Recommendation, Tradeoffs, Rollout, and Success Criteria.
- [ ] I compared do nothing, add independent validation, standardize current deployment, and replace deployment platform.
- [ ] Evidence, assumptions, estimates, and material unknowns are distinguishable.
- [ ] I verified the full proposal is at most two rendered pages using recorded readable settings.
- [ ] I cut it to one rendered page while preserving the decision structure.
- [ ] Both written versions agree on recommendation, scope, commitments, and success criteria.
- [ ] I moved technical details that do not affect the decision into a linked appendix.
- [ ] I rehearsed the pitch aloud and recorded a final delivery within 60 seconds.
- [ ] The pitch contains one explicit decision request with a concrete next action.
- [ ] Review findings were resolved or explained, and important evidence links were checked.
- [ ] I answered all six knowledge questions and all three reflection prompts.
- [ ] All four weekly retrospective prompts are answered.
- [ ] Documentation links work, and the required artifacts are included in the intended commit.

Review changes before staging:

```powershell
git status
git diff --check
git diff
```

Stage the actual Week 9 documents and README/index changes deliberately. Inspect `git diff --cached` for unrelated work, confidential content, broken references, and unfinished placeholders. This documentation lab does not require changing application code or running deployment experiments unless you need additional evidence.

```powershell
# Run after staging and reviewing the intended changes.
git commit -m "Complete week 9 architecture proposal lab"
git push
```

If you maintain weekly milestone tags, add `week-09` after confirming it does not already exist. A tag is optional; committed versions of all three required deliverables are required.

**Week 9 is complete when another person can read your one-page version, identify the choice and its consequences, trace the evidence, and act on the decision request conveyed in your 60-second pitch.**

---

## Appendix: Workbook coverage review

This walkthrough was checked against the supplied workbook's **Week 9 pages 28-30** and its knowledge-check answer-key location on page 40. This table verifies coverage in the walkthrough; it does not certify that a reader completed the exercises.

| Workbook requirement | Walkthrough coverage | Evidence the reader must produce | Review result |
| --- | --- | --- | --- |
| Outcome: concise engineering proposal | Steps 5-7 | Verified two-page maximum proposal | Covered |
| Outcome: separate evidence, constraints, options, recommendation, tradeoffs | Steps 2-6 | Distinct sections and traceable comparison | Covered |
| Outcome: move implementation detail into appendices | Steps 6, 8 | Linked appendix and recorded content moves | Covered |
| Outcome: summarize verbally in 60 seconds | Step 9 | Script and timed spoken rehearsal | Covered |
| Resource: Operational Excellence selected units | Reading section, Step 14 | Review record and decision implication | Covered |
| Resource: Architecture Decision Records | Reading section, Steps 4-6 | Reading record and explicit consequences | Covered |
| Lab 1: leadership asks how to improve deployment reliability | Step 1 | Bounded decision frame | Covered |
| Lab 2: two-page maximum proposal with all eight specified sections | Steps 5-7 | Proposal with page verification | Covered |
| Lab 3: compare all four specified alternatives | Step 4 | Fair comparison in both written versions | Covered |
| Lab 4: cut two pages to one without losing decision structure | Step 8 | One-page version and compression record | Covered |
| Lab 5: 60-second verbal summary with one explicit decision request | Step 9 | Timed pitch and actionable request | Covered |
| Lab 6: move technical details that do not affect the decision into appendix | Steps 6, 8 | Appendix and main-body consequence check | Covered |
| Deliverable: two-page proposal | Steps 6-7, 14 | Committed proposal within the maximum | Covered |
| Deliverable: one-page executive version | Steps 8, 14 | Committed one-page version | Covered |
| Deliverable: 60-second decision pitch | Steps 9, 14 | Committed script and rehearsal record | Covered |
| Completion: required reading/tutorial selections | Reading section, Step 14 | Completed selection notes | Covered |
| Completion: end-to-end and explain result | Step 10 | Reviewed package, evidence trace, and pitch | Covered |
| Completion: required deliverable committed | Step 14 | Commit containing all three deliverables | Covered |
| Completion: explain at least one tradeoff | Steps 4-6, 10 | Specific accepted consequence and review response | Covered |
| Completion: knowledge check before answer key | Step 11 | Six original answers, then corrections | Covered |
| Completion: brief weekly retrospective | Step 13 | Four retrospective responses | Covered |
| Reflection: recommendation makes tradeoff obvious | Step 12, R1 | Tradeoff sentence and reviewer observation | Covered |
| Reflection: removable paragraph | Steps 8, 12, R2 | Actual cut and explanation | Covered |
| Reflection: skeptical executive's first question | Steps 10, 12, R3 | Question, response, evidence, reconsideration condition | Covered |
| Knowledge questions 1-6 | Step 11 | All exact prompts, without supplied answers | Covered |
| Retrospective: clearer, harder, production changes, growth artifact | Step 13 | All four exact prompts | Covered |

**Review outcome:** No Week 9 requirement is omitted. Evidence registers, page verification, spoken timing, and independent review support the required communication exercises. A new platform implementation, actual executive approval, a slide deck, and recorded audio are not prerequisites.
