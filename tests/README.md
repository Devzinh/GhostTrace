# Test ownership

**Active tests live in `src/GhostTrace.Tests` and `tests/GhostTrace.Tests.Unit`**. Both
projects are included in `GhostTrace.sln` and run in CI (`dotnet test GhostTrace.sln`) and
the release gate.

`GhostTrace.Tests.Integration` and `GhostTrace.Tests.ForensicSafety` remain placeholders
outside the solution until they contain real coverage for their claimed scope. Add either
project to the solution when it gains tests so CI and release pipelines pick it up.
