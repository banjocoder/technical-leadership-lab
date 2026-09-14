# Software Delivery System Map

## System Overview

The Policy Service is a small .NET Web API used to examine software
delivery as an end-to-end system. This map describes how a source-code
change moves from a developer workstation to a running application and
identifies the controls, artifacts, and verification mechanisms involved.

## Delivery Flow

[Source Code]

↓ Human: edit

[Modified Source]

↓ Human: commit

[Git Commit]

↓ Human: push

[Remote Repository]

↓ Human: run build.ps1

[Build]

↓ Automated: compile

[Compiled Output]

↓ Automated: unit tests

[Verified Build Output]

↓ Human: select environment configuration

[Configured Application]

↓ Human: start application

[Running Application]

↓ Human: call /health

[Health Result]

↓ Human decision

[Release Acceptance]

## Control

- **Build control:** We have designed our system to stop the build pipeline if the application does not succesfully build

- **Test gate:** build process stops when our tests produce a failure

## Artifact

- **Build output:** The build process transforms source code into compiled application output, including the PolicyService binaries, runtime metadata, configuration files, and required dependencies.

- **Source commit:** A durable version of source code recorded in Git that represents the input to the build.


## Verification

- **Unit tests:** Verify specific expected behaviors of the PolicyService, including policy creation, retrieval, and handling of a nonexistent policy.

- **Health endpoint:** Verifies that the running application is responsive and capable of serving the health request.

## Human Intervention

- **Source management:** A developer manually edits, commits, and pushes source changes.
- **Build initiation:** A developer manually starts the local build process.
- **Environment selection:** A developer manually selects the environment-specific configuration.
- **Application startup:** A developer manually starts the application.
- **Health verification:** A developer manually invokes the health endpoint and interprets the result.
- **Release acceptance:** A human makes the final decision about whether the running application is acceptable.

The build script is currently the primary automated region of the delivery system. Once initiated, restore, compilation, and unit testing execute in a defined sequence without additional human intervention.