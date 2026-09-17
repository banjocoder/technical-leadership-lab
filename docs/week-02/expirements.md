## Expirement 1: Break Package Restoration

Injected failure:
Changed MSTest dependency to 999.999.999

Detected at:
Restore

Detection mechanism:
NuGet locked-mode validation

Error:
NU1004 — project dependencies and packages.lock.json are inconsistent

Downstream result:
Compile skipped
Tests skipped
Publish skipped
Package skipped
Artifact publication skipped

## Expirement 2: Break Compilation

Failure injected:
Invalid assignment of a string to an int.

Failure stage:
Compile

Observed behavior:
Restore completed successfully.
dotnet build failed with CS0029:
"Cannot implicitly convert type 'string' to 'int'."

Downstream behavior:
Unit tests, publish, packaging, and artifact publication did not run.

Detection:
GitHub Actions marked the Compile step and workflow job as failed.

## Expirement 3: Failing Unit Tests