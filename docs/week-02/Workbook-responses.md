## Reflection prompts:

1) What on a self-hosted agent is implicit rather than declared?

preinstalled software/CLI tools
environment variables
OS and architecture

I’d add things like filesystem permissions, network access, certificates/trust configuration, and installed SDK/toolchain versions. The important idea is that these can affect the build even though they aren't described by the pipeline.

2) Could another engineer recreate the build machine from your documentation? Why or why not?

Another engineer could reproduce most of the build requirements from the repository, but recreating an identical build machine would also require documenting the OS, architecture, runner prerequisites, installed system tools, permissions, and other machine-level dependencies.

3) What would make an artifact impossible to reproduce later?

Reproduction becomes unreliable when the build depends on mutable or undocumented inputs such as changing dependency versions, package feeds, SDKs, build-machine state, timestamps, environment variables, or platform-specific behavior.

## Knowledge check:

1) Why should a build artifact be immutable?

An artifact should be immutable so that the exact bytes that were tested are the exact bytes later promoted and deployed. Modifying the artifact after verification breaks that guarantee.

2) Why is building independently in every environment weaker than artifact promotion?

Rebuilding in every environment means QA and production may not receive the same output. Artifact promotion builds once and moves the same verified artifact forward.

3) What should uniquely identify an artifact?

An artifact should have a unique build/artifact ID tied to its source commit and build metadata. A cryptographic hash can additionally identify the exact packaged bytes.

4) What is the difference between an agent and a pipeline?

A pipeline defines the work; an agent is the compute environment that executes that work.

5) Which build dependencies should be explicitly declared?

SDK/compiler versions, target runtime/framework, package sources, package versions and dependency locks, test frameworks/adapters, target architecture, build scripts, and any tools required during the build.

6) What risks arise when self-hosted agents accumulate undocumented software?

Builds may become dependent on software nobody knows they require, making them non-reproducible on another agent. Upgrading or replacing the machine also becomes risky, and failures become harder to diagnose because pipeline behavior depends on hidden machine state.
