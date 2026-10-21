# Technical Leadership Lab

A hands-on lab for practicing **technical leadership** skills through a single, evolving codebase. Over a multi-week program, each week adds an engineering capability (CI/CD, observability, SRE, environment certification) **and** a communication artifact (system maps, ADRs, proposals, retrospectives) — mirroring how real technical leaders pair execution with decision-making.

The lab is built around a fictional **Policy Service** (an ASP.NET Core Web API), which stays intentionally generic so the skills remain transferable across teams and stacks.

## How to use this lab

1. Work through the [Technical Leadership Study Workbook](Technical_Leadership_Study_Workbook.pdf) for the concepts and reflection prompts.
2. Follow the week-by-week labs in the [`labs/`](labs/) folder for detailed, step-by-step instruction.
3. Capture your reasoning, experiments, and retrospectives in the [`docs/`](docs/) folder.

## Weekly labs

| Week | Focus | Lab | Deliverable |
|:------|-------|-----|-------------|
| 1 | Delivery as an end-to-end system | [Week 1 Lab](labs/week-1-lab.md) | [Software Delivery System Map](docs/week-01/delivery-system-map.md) |
| 2 | Continuous Integration & reproducible builds | [Week 2 Lab](labs/week-2-lab.md) | [ADR-001: Build Artifacts Are Immutable](docs/adrs/ADR-001-build-artifacts-are-immutable.md) |
| 3 | Continuous Deployment & artifact promotion | [Week 3 Lab](labs/week-3-lab.md) | [Deployment Success vs. Environment Readiness diagram](docs/diagrams/Deployment_Success_vs_Environment_Readiness.png) |
| 4 | Architecture Communication and ADRs | [Week 4 Lab](labs/Week-4-Lab.md) | [ADR-002: Independent Readiness Validation](docs/adrs/ADR-002-independent-readiness-validation.md) <br/> [ADR-003: Delivery Evidence Storage](docs/adrs/ADR-003-delivery-evidence-storage.md) |
| 5 | Observability with OpenTelemetry | [Week 5 Lab](labs/Week-5-Lab.md)| [Observability Contract](docs/contracts/observability-contract.md) |
|6|Reliability Engineering and Failure Modes| [Week 6 Lab](labs/Week-6-Lab.md)| [FMEA](docs/reliability/fmea.md)<br/>[Realibility Improvements](docs/reliability/reliability-improvements.md)|
|7|Site Reliability Engineering: SLI, SLO, Error Budgets, and Toil| [Week 7 Lab](labs/Week-7-Lab.md)||
|8|Environment Certification Service| [Week 8 Lab](labs/Week-8-Lab.md)||
|9|Architecture Proposal and Decision Enablement| [Week 9 Lab](labs/Week-9-Lab.md)||
|10|Technical Leadership Simulation| [Week 10 Lab](labs/Week-10-Lab.md)||

## Repository structure

```
.github/       # Github CI pipeline configuration
.vscode/       # Local launch configuration for this system
diagraming/    # Tools for creating system diagrams using Structurizr
docs/          # Week-by-week reasoning, ADRs, diagrams, proposals, reliability notes
  ├── adrs/             # Architecture Decision Records
  ├── contracts/        # Details of traces, logs, and metrics configuration 
  ├── diagrams/         # C4 architectural diagrams of the application
  ├── reliability/      # Analysis of failure modes and reliability improvement plans
  ├── week-01/          # Delivery system map
  ├── week-02/          # Experiments, workbook responses, retrospective
  ├── week-03/          # Experiments, workbook responses, retrospective
  ├── week-04/          # Decisions, workbook responses, retrospective  
  ├── week-05/          # Experiments, observability contract, workbook responses, retrospective  
  ├── week-06/          # Experiments, workbook responses, retrospective
  ├── week-07/          # Reliability Scorecard, toil reduction backlog, workbook responses, retrospective
  ├── week-08/          # Working Certification prototype, new ADR, workbook responses, retrospective
  ├── week-09/          # Architectural Proposal (with executive one-pager), decision pitch, workbook responses, retrospective
  └── week-10/          # Capstone Architectural Review deck, completed final assessment, workbook responses, retrospective
labs/          # Step-by-step weekly instructions
scripts/       # build.ps1, record-readiness.ps1
src/           # PolicyService — the ASP.NET Core Web API under study
tests/         # PolicyService.Tests — unit tests
tools/         # Auxiliary services for the application (including RiskScore and Email services), including configuration for the diagramaing tool Structurizr
```

## The Policy Service

A minimal ASP.NET Core Web API that grows in complexity as the lab progresses. It exposes:

- `GET /health` — service health check
- `GET /readiness` — service readiness check
- `GET /policies/{id}` — retrieve a policy
- `POST /policies` — create a policy

## Getting started

```bash
# Build
dotnet build TechnicalLeadershipLab.sln

# Run tests
dotnet test

# Run the API
dotnet run --project src/PolicyService
```

## License

See [LICENSE](LICENSE).
