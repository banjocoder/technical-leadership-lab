# ADR-001: Build Artifacts Are Immutable

## Status

Accepted

## Context

PolicyService requires a repeatable way to turn a specific source revision into
a tested and traceable deployable artifact.

If an application is rebuilt independently for each environment, the resulting
outputs may differ because of changes in source, SDK versions, dependencies,
package resolution, build configuration, or build-machine state. This weakens
the ability to determine whether the software deployed to one environment is
the same software that was previously tested.

The CI workflow currently performs the following stages:

1. Restore dependencies in locked mode
2. Compile the application
3. Run unit tests
4. Publish the application
5. Package the published output
6. Publish the build artifact

If restore, compilation, or unit testing fails, no artifact is published.

The resulting PolicyService artifact is a self-contained `win-x64` ZIP file.
Each artifact contains provenance metadata including the application version,
pipeline run number, source revision, target framework, runtime identifier, and
SDK version.

The artifact filename also includes the application version and shortened Git
commit, and a SHA-256 hash is generated for the final ZIP.

Environment-specific configuration is not considered part of the immutable
application artifact.

## Decision

A successfully published build artifact will be treated as immutable.

Once the CI workflow publishes an artifact, its contents will not be modified.
The exact artifact that passes the build and verification process is the
artifact that should later be promoted through deployment environments.

Each artifact will have:

- A unique application version derived from the CI pipeline run number
- The Git commit SHA that produced it
- Build provenance recorded in `artifact-manifest.json`
- A SHA-256 hash identifying the exact packaged bytes

The current version format is:

`0.2.<pipeline-run-number>`

The artifact filename follows the form:

`PolicyService-<version>-<short-commit>.zip`

Environment-specific configuration will be supplied separately during
deployment rather than modifying the published artifact.

## Alternatives

### Rebuild the application for each environment

DEV, QA, and other environments could independently build the application from
source.

This was rejected because separate builds can produce different outputs due to
changes in dependencies, SDK versions, package sources, build configuration, or
build-machine state. A successful QA test would therefore not necessarily
validate the exact artifact later deployed elsewhere.

### Modify the artifact for each environment

A single build could be produced and then configuration files or other contents
inside the artifact could be modified before deployment to each environment.

This was rejected because modifying the package changes the artifact that was
originally tested. The original artifact hash and provenance would no longer
identify the deployed bytes.

### Promote the same immutable artifact

A single artifact can be built and tested once, then promoted unchanged through
successive environments while environment-specific configuration is supplied
externally.

This approach was selected because it provides the strongest traceability
between source, verification, and deployed software.

## Consequences

### Positive

- The exact artifact that passed CI can be identified later.
- Source code can be traced from the artifact using the Git commit SHA.
- The SHA-256 hash can identify whether the artifact contents have changed.
- Failed restore, compilation, or test stages cannot produce a releasable
  artifact.
- Promotion between environments does not introduce differences caused by
  rebuilding.
- Environment-specific configuration remains separate from application build
  output.
- Build provenance is available without relying exclusively on CI history.

### Negative

- Changes to application contents require a new build and a new artifact.
- Even small fixes cannot be applied directly to an existing published package.
- Self-contained publishing produces a larger artifact than a
  framework-dependent deployment.
- The current artifact targets `win-x64`, making it platform-specific.
- Updating the bundled .NET runtime requires rebuilding and republishing the
  application.

## Result

A published artifact represents a specific, verified output of a specific
source revision. If its contents change, it is a new artifact and must receive
a new identity and pass the CI process again.