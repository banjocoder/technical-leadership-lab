# Week 4 Lab: Architecture Communication and ADRs

Weeks 1–3 established a working PolicyService delivery process: source changes produce a tested, identified artifact; that artifact is promoted through DEV and QA; deployment execution and environment readiness are recorded separately.

Week 4 asks a different question:

> Can you explain what this system is, where its boundaries are, why it is designed this way, and which decisions are important enough to preserve?

This week focuses on reasoning, diagrams, and Architecture Decision Records (ADRs). You will document the system you have already built and record one decision about its future evidence storage. You do not need to expand the application or implement a new storage integration to complete the week.

## Learning outcomes

By the end of this lab, you should be able to:

- Define a system boundary and distinguish the logical system from its hosting platform.
- Choose architecture views that answer different questions for different audiences.
- Represent artifact creation, deployment, readiness, and evidence as distinct responsibilities.
- Map logical elements onto execution infrastructure using Structurizr DSL.
- Write an ADR that preserves context, a decision, alternatives, consequences, and ownership.
- Review diagrams and ADRs for consistency with implemented and planned behavior.

## Before you begin

This lab assumes you completed Weeks 1–3 in your own `technical-leadership-lab` repository. You should have:

- PolicyService and its test project.
- A GitHub Actions workflow that builds, tests, packages, and identifies an immutable artifact.
- DEV and QA deployment stages that consume the same artifact.
- Environment-specific configuration supplied during deployment.
- Application liveness and readiness checks, plus recorded deployment/readiness evidence.
- The Week 3 failure experiments and deployment metadata schema.
- `docs/adrs/ADR-001-build-artifacts-are-immutable.md` from Week 2.

Have a text editor and a working Structurizr DSL environment available. Use your existing installation if you have one. The [Structurizr documentation](https://docs.structurizr.com/) provides installation and DSL guidance; this lab concentrates on modeling rather than installing the tool.

Read your own workflow and evidence script before drawing. If your implementation differs from the original project, document the difference rather than claiming behavior you have not built.

## How to work through the week

Use the same pattern as the earlier labs: **reason first, create the artifact, inspect it, revise it, and then reflect**.

1. Define the system boundary.
2. Create the System Context view.
3. Create the Container/logical view.
4. Map the system onto its deployment infrastructure.
5. Add a Dynamic view of one delivery.
6. Review ADR-001 and draft ADR-002.
7. Draft ADR-003 and reconcile the diagrams.
8. Perform a consolidated architecture review.
9. Complete the knowledge check, reflections, retrospective, and repository closeout.

Checkpoints occur at larger milestones. You can work independently or use a coach to review each checkpoint. For the ADRs, work one section at a time: draft your answer, review it against the prompts, revise it, and then continue.

## Portfolio artifacts

Use this structure, adapting filenames if your repository already has an established convention:

| Path | Purpose |
| --- | --- |
| `diagraming/workspace.dsl`<br/>`diagraming/workspace.json` | Shared Structurizr model and view definitions |
| `docs/adrs/ADR-002-readiness-validation-is-independent.md` | Independent readiness decision |
| `docs/adrs/ADR-003-delivery-evidence-storage.md` | Durable evidence-storage decision |
|`docs/week-04/workbook-responses.md`| Recorded responses for the workbook questions|
| `docs/week-04/retrospective.md` | Knowledge-check answers, reflections, and weekly retrospective |

Store diagram exports alongside the documents or in your existing diagrams directory. Preserve the editable DSL as well as the rendered diagrams.

You will finish with **three ADRs total**: the existing ADR-001 and two new records. Do not create another immutability ADR to meet a file count.

---

## Step 1: Define the boundary before drawing

Imagine a new engineer asks:

> What system are we actually studying in this lab?


1. What would you name the system of interest, and what does that name imply about its boundary?
2. Which people or roles interact with it?
3. Which external systems support it?
4. Which things are internal responsibilities, and which are deployment destinations or hosting infrastructure?
5. Which details should be absent from a System Context diagram?

For continuity across this lab, use **PolicyService Delivery System** as the system name. Write a short purpose statement in your own words that covers the path from source changes through artifact creation, promotion, deployment, validation, and evidence.

### Platform versus logical system

The delivery system is largely implemented using GitHub Actions. GitHub still supplies platform capabilities outside the logical system boundary: source hosting, execution infrastructure, artifact storage, and environment controls.

Consider this distinction when describing database provisioning: GitHub supplies the runner; your workflow decides how to configure that runner and provision the lab database. Do not assign those responsibilities to the platform merely because the commands execute there.

Create a **Boundary Decisions** table with these columns:

| Element | Inside or outside the chosen boundary? | Reason | View where it belongs |
| --- | --- | --- | --- |
| Contributor | To decide | To explain | To choose |
| GitHub | To decide | To explain | To choose |
| QA Environment Approver | To decide | To explain | To choose |
| Package Repository | To decide | To explain | To choose |
| DEV and QA targets | To decide | To explain | To choose |
| PolicyService runtime and database | To decide | To explain | To choose |

Avoid adding every dependency to the context diagram. Ask whether the element helps answer the diagram's question. A generic network box, individual SDK, or every package feed may add little unless its boundary matters to the story.

**Checkpoint:** A reader should be able to tell whether you are documenting PolicyService itself or the system that delivers it. Your purpose statement and boundary table should agree.

## Step 2: Create the System Context view

The System Context view answers:

> Who interacts with the PolicyService Delivery System, and which external systems does it depend on?

Treat the delivery system as one black box. Begin with the Contributor, QA Environment Approver, GitHub, Package Repository, and DEV/QA targets. Leave build processes, scripts, readiness modules, and individual jobs for the next view.

For this delivery-focused lab, DEV and QA can appear as logical target boxes. Explain that they represent delivery destinations; their actual infrastructure will be modeled in the Deployment view. This is an explicit teaching convention rather than a claim that an environment is a conventional C4 software system.

### Reason about the relationships

Before drawing arrows, write a short verb phrase for each interaction:

- Contributor and GitHub: how does a change enter the system?
- GitHub and the delivery system: what does GitHub supply beyond a trigger?
- Approver and the delivery system: what transition is being authorized?
- Delivery system and DEV/QA: does the system only deploy, or also validate?
- Package Repository and the delivery system: which responsibility uses the packages?

Keep responsibility clear. At this level, an approver authorizes promotion through the delivery system; the literal GitHub Environment gate belongs in a more concrete view.

### Starter DSL

Start with this dsl template. It supplies syntax and element names; you must add the relationships and descriptions.

```dsl
workspace "PolicyService Delivery System" {
    model {
        contributor = person "Contributor"
        qaApprover = person "QA Environment Approver"

        gitHub = softwareSystem "GitHub"
        packageRepository = element "Package Repository"
        devEnv = element "DEV Environment"
        qaEnv = element "QA Environment"

        ss = softwareSystem "PolicyService Delivery System"

        // TODO: Add relationships with concise descriptions.
        // Syntax example, not a completed relationship:
        // contributor -> gitHub "Your relationship description"
    }

    views {
        systemContext ss "SystemContext" {
            include *
            autoLayout lr
        }
    }
}
```

Render the view after adding your relationships. An element definition alone does not ensure that it appears in every view; inspect the rendered result and explicitly include elements if needed.

Explain how delivery starts, which capabilities GitHub provides, what the delivery system does to the targets, and where human authorization enters.

### Review your first attempt

- Is the Contributor still visible?
- Is the delivery system a single black box?
- Do the GitHub labels describe system-level capabilities rather than an internal build step?
- Do the target relationships communicate validation as well as deployment?
- Do all arrows say why the interaction exists?

If your diagram contains numbered steps or reveals the build process, you may have created a Dynamic view. Preserve that work for Step 5, then create a separate context view.

## Step 3: Create the Container/logical view

Now change the question:

> What responsibilities make up the delivery system, and how do they relate?

A C4 container is a runnable, deployable, or data-holding architectural unit. It does not mean Docker. This lab also needs to show a passive artifact and workflow capabilities, so label the result **Container / Logical Architecture** and document its notation choices.

### Identify the responsibilities

Expand `ss` into a block and add these logical elements. Write a one-sentence responsibility for each before connecting them:

| Element | Question your description should answer |
| --- | --- |
| Build Process | What must happen before a deployable artifact exists? |
| Immutable PolicyService Artifact | What identity and lifecycle does the output have? |
| DEV Deployment Process | What does it place, configure, and start? |
| DEV Readiness Validation | What does it evaluate after deployment? |
| QA Deployment Process | What additional preconditions govern its execution? |
| QA Readiness Validation | What does it independently report? |
| Delivery Evidence | Which lifecycle results need to be retained? |

Use this syntax fragment as a starting point **inside `ss`**, adding the remaining elements yourself:

```dsl
buildProcess = container "Build Process"

buildArtifact = container "Immutable PolicyService Artifact" "Packaged application promoted unchanged" "ZIP file" {
    tags "Artifact"
}

devDeploymentProcess = container "DEV Deployment Process"
devReadinessProcess = container "DEV Readiness Validation"

// TODO: Add QA deployment, QA readiness, and Delivery Evidence.
```

The `container` representation of the ZIP is a pragmatic DSL convention. It is not a claim that the ZIP is an executing C4 container. Style it distinctly, explain the convention in the legend, and keep it separate from the Build Process. A stricter alternative is to model an Artifact Store and describe the stored immutable package in relationships.

### Keep DEV and QA separate for this lab

DEV and QA may reuse implementation mechanisms, but they occupy distinct stages with different entry conditions. QA follows successful DEV execution and required readiness, and it has an additional human approval condition.

Keep separate deployment and readiness elements for DEV and QA. In your narrative, explain why this separation describes lifecycle responsibilities without implying that every implementation routine is duplicated.

### Build the relationships yourself

Show:

1. What produces the immutable artifact.
2. Which processes retrieve or consume that artifact.
3. Which processes deploy into each target.
4. Which processes independently evaluate each target.
5. What makes an artifact eligible for QA approval.
6. Which responsibilities contribute build, deployment, and validation evidence.

An artifact does not deploy itself. Separate the relationship that retrieves an artifact from the relationship that deploys it to a target.

At this stage, **Delivery Evidence** is a logical responsibility. Do not place it on a physical host before considering ADR-003.

Add a Container view alongside the context view:

```dsl
container ss "ContainerView" {
    include *
    autoLayout lr
}
```

Use concise verb phrases on arrows. Distinguish artifacts and evidence using tags, styles, or a legend; retain readable text at normal viewing size.

**Checkpoint: review the context and logical views together.**

- Can a reader distinguish build, deployment, readiness, and evidence responsibilities?
- Does the artifact have a lifecycle independent of the build that produced it?
- Is approval represented as a control rather than an invented executable container?
- Is DEV readiness part of the QA eligibility condition?
- Does the logical view add useful responsibilities without becoming a list of YAML steps?

Record one change you made after reviewing your first diagram and why it improved the explanation.

## Step 4: Add the Deployment view

The Deployment view answers:

> Where and how do these logical responsibilities execute today?

Use the Week 3 workflow as evidence. In the original lab, each job runs on an ephemeral GitHub-hosted Windows runner. DEV and QA are temporary runtime environments created during a workflow execution, rather than long-lived servers.

### Inventory the deployment topology

Complete this table before extending the DSL:

| Location | What runs or lives there? | Persistent or ephemeral? |
| --- | --- | --- |
| GitHub platform | Identify platform services used by the workflow | To describe |
| Build Runner | Identify the relevant logical process | To describe |
| Workflow Artifact Storage | Identify the package shared by DEV and QA | To describe |
| DEV Runner | Identify deployment, validation, application, and database instances | To describe |
| QA Runner | Identify deployment, validation, application, and database instances | To describe |
| QA Environment approval control | Identify the transition it governs | To describe |

The artifact path should originate at artifact storage for both DEV and QA. QA consumes the original build artifact; it does not package or copy a new artifact out of the DEV runtime.

### Understand the DSL layers

| Construct | Meaning in this lab |
| --- | --- |
| `container` | Logical process, workload, or data-holding element |
| `deploymentEnvironment` | Named deployment model, such as one pipeline execution |
| `deploymentNode` | Infrastructure where instances execute |
| `containerInstance` | An instance of an existing logical container at a location |
| `infrastructureNode` | Supporting infrastructure, such as artifact storage or an approval control |
| `deploymentGroup` | Grouping used to constrain replicated relationships between instances |

Do not define `deploymentNode = element` as a custom archetype and expect native deployment behavior. A generic logical DEV target and a native DEV Runner deployment node have different meanings.

### Add the runtime elements

Inside `ss`, define logical PolicyService runtime and Policy Database containers. Give them a tag such as `DeploymentOnly` if useful. Define the application's database relationship and the deployment/validation relationships to the runtime.

Exclude these two workload elements from the Container/logical view if that view is focused on delivery responsibilities:

```dsl
exclude policyServiceRuntime
exclude policyDatabase
```

Use those identifiers only after you have defined them in your model.

### Starter deployment fragment

Add this fragment **inside `model`, after defining the referenced logical containers**. It shows the mapping pattern for Build and DEV; complete QA and the supporting infrastructure yourself.

```dsl
pipelineExecution = deploymentEnvironment "Pipeline Execution" {
    devGroup = deploymentGroup "DEV"
    qaGroup = deploymentGroup "QA"

    githubPlatform = deploymentNode "GitHub Actions" "Hosted execution infrastructure" "GitHub Actions" {
        artifactStorage = infrastructureNode "Workflow Artifact Storage" "Stores the immutable build artifact" "GitHub Actions Artifacts"

        buildRunner = deploymentNode "Build Runner" "Ephemeral Windows runner" "windows-latest" {
            buildInstance = containerInstance buildProcess
        }

        devRunner = deploymentNode "DEV Runner" "Ephemeral Windows runner" "windows-latest" {
            deploymentGroup devGroup

            devDeployInstance = containerInstance devDeploymentProcess
            devReadinessInstance = containerInstance devReadinessProcess
            devPolicyServiceInstance = containerInstance policyServiceRuntime
            devDatabaseInstance = containerInstance policyDatabase
        }

        // TODO: Add a QA runner using qaGroup and your QA containers.
        // TODO: Add the QA Environment approval infrastructure node.
        // TODO: Add artifact-publication and retrieval relationships.
        // TODO: Connect DEV readiness eligibility and QA approval.
    }
}
```

Add the corresponding view **inside `views`**:

```dsl
deployment ss pipelineExecution "DeploymentView" {
    include *
    autoLayout lr
}
```

The scope and deployment environment are both required; `deployment *` alone does not complete the view definition.

### Inspect instance relationships

You have one logical PolicyService-to-database relationship but two runtime sets. Inspect the rendered view for accidental cross-environment connections, such as DEV PolicyService using the QA database. Deployment groups help constrain the replicated instance relationships.

The deployment groups are a modeling mechanism, not network isolation or runtime security. They also do not enforce workflow ordering. Your actual workflow implements ordering and approval; the diagram documents those conditions.

Do not instantiate Delivery Evidence on GitHub simply to fill an empty place in the diagram. Its physical implementation is the decision you will address in Step 7.

In `deployment-view.md`, explain how this topology differs from persistent DEV/QA servers. Identify which logical responsibilities would remain if the hosting arrangement changed.

For additional syntax help, consult the [DSL language reference](https://docs.structurizr.com/dsl/language), [deployment view example](https://docs.structurizr.com/dsl/cookbook/deployment-view/), and [deployment groups example](https://docs.structurizr.com/dsl/cookbook/deployment-groups/).

## Step 5: Add the Dynamic view

The Dynamic view answers:

> What happens during one delivery, and which conditions permit the next transition?

Create an ordered interaction view using your existing model. Begin by drafting the sequence in prose; then add a Structurizr Dynamic view or another sequence notation you can maintain in the repository.

Your view should cover source submission, delivery execution, artifact publication, DEV deployment, DEV validation, evidence recording, QA authorization, QA deployment of the same artifact, and QA validation/evidence.

Choose the arrow order and labels yourself. Make the distinction between **artifact movement** and **permission to proceed** explicit.

Write a failure-path explanation under the diagram:

- What happens if build verification fails?
- What happens if DEV deployment succeeds but required readiness fails?
- What happens while QA authorization is pending?
- What happens if QA deployment succeeds but QA is NotReady?
- Which evidence should remain available when a gate blocks continuation?

**Checkpoint: review the deployment and dynamic views together.**

Can a reader identify where the application runs, where its database lives, where the artifact comes from, and why QA cannot proceed yet? Verify that the dynamic sequence does not imply that failed readiness reverses an already completed deployment. Identify planned behavior explicitly when it is not implemented.

---

## Step 6: Review ADR-001 and write ADR-002

ADRs explain why a meaningful architectural choice exists. They preserve the situation, the selected option, credible alternatives, and the consequences accepted by the team.

### Review ADR-001

Read `ADR-001-build-artifacts-are-immutable.md`. Check that your architecture shows one identified artifact consumed by both DEV and QA, with environment-specific configuration external to the immutable package.

Keep the existing record. If you discover a mismatch between the decision and the implementation, record the finding and address it deliberately rather than silently replacing the decision history.

### Start ADR-002

Create `docs/adrs/ADR-002-readiness-validation-is-independent.md` using this template:

```markdown
# ADR-002: Readiness Validation Is Independent of Deployment Execution

- Status: Proposed
- Date: YYYY-MM-DD

## Context

[Describe the problem, constraints, and outcome needed.]

## Decision

[State the choice and its boundaries.]

## Alternatives Considered

[For each credible alternative, record its appeal and limitations.]

## Consequences

### Positive

[What does this enable?]

### Negative

[What cost or complexity is accepted?]

### Responsibilities

[Who defines policy, maintains checks, and responds to failures?]

## Evidence

[Link to implemented behavior, experiments, and diagrams.]
```

Use your own date. Set the status to Accepted once you have reviewed and adopted the decision for your project.

### A. Context: describe the problem before the solution

Use the Week 3 experiments as your evidence. Explain why successful build or deployment execution cannot establish that an environment is usable.

Prompts:

- What outcome makes the delivery process valuable to its users?
- Which boundaries need distinct evidence?
- What becomes difficult to diagnose if one overall status represents everything?

**Review before continuing:** Does your Context explain the need without already prescribing the complete solution?

### B. Decision: define what independent means

Prompts:

- What runs after DEV and QA deployment?
- What do deployment success and readiness success each mean?
- Which checks are baseline blockers in this lab?
- Which database, external-dependency, or smoke checks depend on the environment's intended purpose?
- Can validation be rerun without deploying again?
- What does NotReady prevent?

Use the policy developed in Week 3: application reachability and required configuration are baseline conditions; additional checks may be required according to scope. A required dependency failure should block acceptance. A deliberately scoped QA environment needs an explicit policy explaining any accepted degraded condition.

Independence means separate responsibilities and results. It does not require a separate vendor platform, machine, or repository.

**Review before continuing:** A readiness failure occurs after deployment. Your wording should block further promotion or acceptance for use, rather than claim to stop the deployment that already completed.

### C. Alternatives: compare credible options

Write the appeal and limitations of:

1. Treating successful deployment as sufficient evidence of readiness.
2. Embedding deployment and readiness in one step with one combined result.
3. Performing validation only during the build.

Then explain why independent post-deployment validation addresses your constraints. Consider whether a different deployment tool could invoke the same validation capability.

Do not dismiss the build-only option with “same problem.” Explain what build-time evidence can establish and which target-environment conditions it cannot evaluate.

### D. Consequences and responsibilities

Describe what you gain, what you pay for, and who must maintain the policy. Consider failure diagnosis, rerunning validation, workflow complexity, execution time, configuration, and the number of states to interpret.

Avoid claiming a guarantee that the application works in every situation. Readiness provides evidence against the configured criteria at the time of evaluation.

### E. Evidence

Link to the Week 3 experiments, readiness implementation, evidence script, workflow, and relevant diagrams. Choose the strongest examples rather than repeating the entire decision.

**Checkpoint:** Your ADR should let a future engineer explain why deployment and readiness are separate even if they have never read this conversation or the lab.

## Step 7: Write ADR-003 and reconcile the diagrams

This decision resolves the location of the logical Delivery Evidence responsibility.

The direction selected for the original Week 4 project was **structured historical JSON records in Cloudflare R2, with relevant current results presented in GitHub Actions workflow summaries**. A separate historical dashboard was deferred.

Use that direction as the decision candidate for this lab. You still need to explain why it fits, evaluate alternatives, and define the consequences. If your project's constraints lead you to a different choice, document it and update the diagrams consistently.

**Week 4 requires the decision and architecture updates. It does not require creating an R2 bucket, changing workflow credentials, implementing evidence uploads, or building a historical dashboard.**

### A. Context: define the questions evidence must answer

Create `docs/adrs/ADR-003-delivery-evidence-storage.md` using the same ADR structure as Step 6.

Write a solution-neutral Context that explains:

- Who needs to inspect delivery results and what decisions they make.
- Which artifact, source revision, environment, timestamps, and run identity need to be traceable.
- Why deployment and readiness outcomes need durable, structured history.
- How “what happened during a delivery?” differs from “what is running now?”

In this lab, runners disappear after jobs complete. A historical record of successful validation does not prove that a persistent DEV or QA application is currently running.

### B. Decision: separate storage, production, and presentation

Draft a Decision that assigns these responsibilities clearly:

| Responsibility | Candidate selected in the original project |
| --- | --- |
| Produce delivery and validation evidence | GitHub Actions executing the delivery system |
| Retain durable historical records | External Cloudflare R2 bucket |
| Present relevant current-run results | GitHub Actions workflow summary |
| Present a separate historical dashboard | Deferred |

In your own words, address:

- What constitutes a historical record?
- How will new records avoid overwriting earlier evidence?
- Which minimum evidence fields must be captured?
- Which system is the durable source of record?
- What is intentionally deferred?

The minimum evidence should cover artifact version and SHA-256, source commit, environment, timestamps, workflow run, deployment result, readiness result, and per-check details. Reuse the Week 3 schema concepts; do not invent conflicting result names.

Treat immutability as a write policy: each delivery or validation event has a unique key and prior records are not overwritten. Object storage alone does not establish that policy. Consider retries and run attempts when specifying unique record identity; defer the exact key format if implementation is future work.

### C. Alternatives: explain the trade-offs

Evaluate these alternatives in your own words:

| Alternative | Questions to investigate |
| --- | --- |
| GitHub Actions as the only store | What integration is easy? How does platform retention and access affect independence? |
| Deployment-local JSON | How useful is current identity? What happens when the environment is recreated or history is overwritten? |
| Application database table | What querying is convenient? Could an outage or failed deployment prevent recording the needed evidence? |
| Dedicated relational evidence registry, optional | What stronger querying does it provide? What extra service and ownership does it introduce? |

A local JSON record is not inherently temporary; its lifecycle depends on how it is managed. GitHub does retain run information; the concern is retention and platform coupling, rather than an absence of storage. Keep rejected alternatives accurate and credible.

You may describe a hybrid where a local file reports current identity and an independent store retains history. Explain which responsibility each copy serves and which source is authoritative.

### D. Consequences and responsibilities

Consider independent retention, historical analysis, decision support, and future reporting. Balance these benefits against:

- A new Cloudflare dependency and operational owner.
- Credentials and access controls.
- Record schema, unique keys, and retention policy.
- Failed-write handling and missing-evidence investigation.
- Less convenient querying than a relational database.
- Future summary/dashboard maintenance.

State who owns those responsibilities. “Independent” here means independent of the target application and its database, environment lifecycle, and workflow retention. It does not mean the evidence has no external dependencies.

Decide how a failed evidence write should be reported and whether it blocks promotion. Keep an evidence-publication failure distinguishable from a deployment or readiness failure. If that policy is unresolved, list it as an implementation follow-up.

### E. Evidence and implementation status

If the R2 integration is not implemented, say so directly. List the future acceptance evidence: a stored uniquely identified JSON record, a readable workflow summary, failure reporting, and verification that earlier records remain intact.

An ADR can be Accepted while its implementation is still Planned. Record both states explicitly.

### F. Reconcile the views

Update your architecture after adopting ADR-003:

1. Add Cloudflare R2 as an external evidence-storage dependency in the context model.
2. Connect the logical evidence responsibility to the chosen external storage realization.
3. Show the planned evidence publication and presentation path in the deployment/dynamic documentation.
4. Distinguish R2 evidence storage from GitHub's immutable application-artifact storage.
5. Mark the R2 integration as planned and the historical dashboard as deferred.

You may use a separate target-state Deployment view or visibly tag planned elements. Explain your choice in the diagram narrative. Do not place R2 inside a Windows runner or imply that the external Cloudflare platform belongs to the delivery system.

For the planned sequence, record evidence before enforcing the promotion decision so a blocked environment retains diagnostic information. Compare this with your current workflow and identify any implementation gaps.

---

## Step 8: Perform the architecture review

Review the set as if you were a new engineer. Use three passes and record findings in `architecture-review.md`.

### Pass 1: Boundaries and responsibilities

- Is the system of interest consistent across all views?
- Can you identify how the Contributor initiates delivery?
- Are deployment and readiness distinct?
- Does the Approver authorize the correct transition?
- Is the external evidence dependency visible where appropriate?
- Can the reader distinguish logical responsibilities from platform services?

### Pass 2: Workflow and states

Test the model with these scenarios. Write the expected recorded states and next action yourself:

| Scenario | Deployment result | Readiness result | Next action or gate |
| --- | --- | --- | --- |
| DEV deployment completes; required checks pass | Fill in | Fill in | Explain |
| DEV deployment completes; a required dependency fails | Fill in | Fill in | Explain |
| Deployment execution fails before validation | Fill in | Fill in | Explain |
| DEV meets its criteria; QA approval is pending | Fill in for the relevant target | Fill in for the relevant target | Explain |
| QA deployment completes; required configuration fails | Fill in | Fill in | Explain |

Use the Week 3 result semantics: `Succeeded`/`Failed` for deployment execution and `Ready`/`NotReady`/`NotEvaluated` for readiness where applicable. Do not invent a QA result when its job has not run.

Confirm that:

- QA consumes the same build artifact as DEV.
- Required DEV readiness is satisfied before QA approval can release promotion.
- Failed readiness blocks promotion or acceptance without rewriting deployment history.
- Evidence recording precedes gate enforcement in the documented design.
- Planned storage behavior is distinguished from current implementation.

### Pass 3: ADR-to-diagram alignment

| ADR | Architectural trace to look for |
| --- | --- |
| ADR-001: Immutable Artifacts | One artifact identity, unchanged promotion, external environment configuration |
| ADR-002: Independent Readiness Validation | Separate deployment and readiness responsibilities/results, with readiness informing acceptance and promotion |
| ADR-003: Delivery Evidence Storage | Durable historical evidence, explicit producer and presentation roles, planned R2 realization, deferred historical dashboard |

For each finding, record the view or ADR affected, the ambiguity, the correction, and whether it is documentation work or future implementation.

### Quality-attribute exercise

Choose two quality attributes relevant to your decisions, such as maintainability, reliability, or auditability. For each, write a concrete scenario with a trigger, the expected system response, and a way to assess that response.

Prompts: What should change if the deployment tool is replaced? How should you investigate a failed QA validation after the runner is gone? Which architectural boundary makes that task easier, and what cost comes with it?

Keep this exercise short. Its purpose is to connect an architectural choice to an observable quality goal.

## Step 9: Knowledge check

Answer these from memory before rereading your ADRs. Save your answers in `docs/week-04/retrospective.md`.

1. When should you create an ADR?
2. Why should an architecture diagram avoid showing every class?
3. What question does a system-context diagram answer?
4. What is a quality attribute?
5. Why is maintainability an architecture concern?
6. What is the difference between architecture and implementation?

After writing, review each answer for a clear explanation and a relevant example from PolicyService. If you use a coach, ask for feedback on your reasoning rather than a replacement answer.

## Step 10: Reflection prompts

Use the workbook's three prompts:

**R1. Which diagram would you show an executive? Which would you show an engineer implementing the system?**

Explain the audience's question and why your selected view answers it. Consider whether implementation and operations need different levels of detail.

**R2. What decision is expensive enough to deserve an ADR?**

Choose a decision from this project. Explain the future cost of reversing it, the coupling it creates or removes, or the operational ownership it introduces.

**R3. Which implementation details are intentionally absent from your diagrams?**

Name concrete omissions and explain why they do not help the selected view. Consider classes, methods, script names, YAML steps, configuration keys, credentials, object-key formats, and endpoint implementation details. Some interface or deployment details can be architecturally important; justify the omissions for your diagram's purpose rather than treating all detail as irrelevant.

## Step 11: Weekly retrospective

Answer the same four questions used in the earlier weeks:

1. **What became clearer this week?**
2. **What was harder than expected?**
3. **What would I do differently in a production system?**
4. **What artifact from this week best demonstrates growth?**

Write what you experienced. Include a modeling mistake or revision if it taught you something useful. Keep the retrospective separate from the architecture overview so the overview remains useful to a reader who was not part of your learning process.

## Step 12: Repository closeout

Update the root README's weekly roadmap and the documentation index to link to Week 4, its diagram set, and all three ADRs. In the Week 4 overview, include:

- The system purpose and boundary decisions.
- A table identifying the question answered by each view.
- Links to diagrams, editable DSL, and ADRs.
- Current implementation, accepted future decisions, and deferred work.

### Completion checklist

- [ ] The system boundary is explicit and consistent.
- [ ] System Context, Container/logical, Deployment, and Dynamic views are saved.
- [ ] The DSL renders, and exported diagrams have readable labels.
- [ ] Custom artifact/evidence notation and logical DEV/QA target boxes are explained.
- [ ] DEV and QA remain distinct lifecycle stages with clear promotion conditions.
- [ ] Deployment instances match the runner/application/database topology.
- [ ] No accidental DEV-to-QA database relationships appear.
- [ ] Artifact storage supplies the same package to DEV and QA.
- [ ] Existing ADR-001 is reviewed and linked.
- [ ] ADR-002 and ADR-003 contain context, decisions, credible alternatives, consequences, and responsibilities.
- [ ] R2 is documented as the selected external evidence store, with implementation status explicit.
- [ ] The historical dashboard is marked deferred.
- [ ] The three-pass architecture review is complete.
- [ ] Knowledge-check, reflection, and retrospective answers are recorded in your own words.
- [ ] README and documentation links work.

Review your actual changes before committing:

```powershell
git status
git diff --check
git diff
```

Stage the Week 4 files and index updates deliberately. Run this example only if the paths match the files you created:

```powershell
git add docs/week-04 docs/adrs/ADR-002-readiness-validation-is-independent.md docs/adrs/ADR-003-delivery-evidence-storage.md README.md
git diff --cached
git commit -m "Complete week 4 architecture and ADR study"
git push
```

If you have maintained weekly milestone tags, add the Week 4 tag after confirming it does not already exist:

```powershell
git tag week-04
git push origin week-04
```

**Week 4 is complete when another engineer can use your views and ADRs to explain the system's boundaries, delivery sequence, and major trade-offs without needing the original conversations.**
