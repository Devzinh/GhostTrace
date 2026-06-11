# Test ownership

**All active tests live in `src/GhostTrace.Tests`** — that is the single project run by
CI (`dotnet test GhostTrace.sln`) and the release gate. New tests belong there.

The projects in this folder (`GhostTrace.Tests.Unit`, `GhostTrace.Tests.Integration`,
`GhostTrace.Tests.ForensicSafety`) are empty placeholders that were removed from
`GhostTrace.sln` until they contain real coverage for their claimed scope. If one of
them is implemented, add it back to the solution with `dotnet sln add` so the CI and
release pipelines pick it up.
