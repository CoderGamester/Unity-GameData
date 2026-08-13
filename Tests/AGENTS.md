# GameLovers GameData Tests — Agent Guide

This guide adds test-only rules to the package and host guides.

## Shared test rules

<!-- BEGIN SHARED TEST RULES -->
### Admission

A new test is admitted only when all six answers are yes:

| Check | Requirement |
|---|---|
| **A1 Defect** | Name the production file/symbol and the incorrect behavior in one sentence. “It could break” is not a defect. |
| **A2 Red** | Name a plausible production edit that should make the assertion fail. Prefer one line/branch; shared-path integration mutations are allowed when isolation is dishonest. |
| **A3 Package-owned** | Every assertion must read behavior this package computes, not C# defaults, fresh-object non-nullness, or Unity guarantees. |
| **A4 Cheapest** | Use the cheapest honest tier: a test case before a new test, EditMode before PlayMode, and an existing fixture before a new fixture. |
| **A5 Unique** | Grep the symbol, derived/wrapper types, and paired setup fields. Do not add a test already reddened by the same narrow defect. |
| **A6 Environment** | Control ambient renderer, Addressables, sample, static, and project state, or branch the expectation on the state actually observed. |

Two additional rejects apply:

- **D1 Tautology:** a lone `DoesNotThrow`, freshly-created non-null assertion, language default, or input-derived substring match pins no package behavior unless used by a named harness sentinel.
- **D2 Name/body mismatch:** deleting or bypassing the behavior promised by the test name must not leave the test green. Strengthen the assertion or rename the test to its actual claim.

Fixtures under `Smoke/` are exempt from A1/A2 and may assert construction/bootstrap viability. The exemption is directory-scoped, not permission to use smoke assertions in Unit or Integration fixtures.

### Revert and Confirm Red (RCR)

Every new or strengthened behavioral test must be observed failing once against a plausible production mutation before commit:

1. Run the new test against normal production code and observe GREEN.
2. Preserve the exact working patch or use an isolated worktree; then apply the A2 mutation. Never restore a dirty file from `HEAD`.
3. Run the smallest attributable filter. RED must come from the intended assertion with a diagnostic failure, not a compile error or unrelated `NullReferenceException`.
4. Restore the saved production state, confirm the mutation is gone without losing other edits, and observe GREEN again.
5. Record the observation on the test using `file + symbol`, never a line number.

Use this compact form, targeting four lines and never exceeding six:

```csharp
[Test]
// ADMIT: <owned defect naming production file and symbol>
// RCR: <file> <symbol> — <mutation> → RED (<assertion failure>). <YYYY-MM-DD>
public void Method_Condition_ExpectedResult()
```

Do not narrate investigation history in the test. A nearby mutation that looked valid but stayed green may be recorded when that negative result prevents repeated work.

When a test resists an isolated mutation, classify it before acting:

| Verdict | Meaning | Action |
|---|---|---|
| **A3 reject** | No package production behavior participates. | Delete the test. |
| **A5 duplicate** | The same narrow mutation already belongs to a sibling. | Delete it and name the surviving sibling in review/commit context. |
| **D2 overclaim** | The mutation implied by the name leaves the body green. | Strengthen or rename. |
| **UNFALSIFIABLE** | Real package behavior is double-guarded or cannot be broken by a safe isolated edit. | Keep only after attempted mutations are recorded with the specific reason. |
| **SHARED-PATH** | A broader mutation reddens this legitimate integration path together with siblings. | Keep, recording the observed mutation and blast radius. |

Unannotated tests have three possible histories: observed RED with lost write-back, collateral RED under another test's mutation, or never probed. Check `.test-all/rcr/` before probing and never write prepared annotation text without matching observed evidence. Mutation records stay under `.test-all/rcr/`, not `/tmp`.

Benchmarks use the inverted check: removing the workload from the measured body must materially change the result. Run the actual test assembly; a plain Unity open does not compile assemblies constrained by `UNITY_INCLUDE_TESTS`.
<!-- END SHARED TEST RULES -->

## Placement

- `Editor/Unit/`: preferred pure-logic tier, including `Unit/ConfigBrowser/` for extractable editor logic.
- `Editor/Integration/`, `Security/`, and `Boundary/`: cross-type behavior, serializer trust, and limits.
- `Editor/Performance/` and `Editor/Smoke/`: benchmarks and bootstrap checks only.
- `PlayMode/Integration/`: Unity object lifecycle or frame-dependent behavior. `PlayMode/Performance/` and `PlayMode/Smoke/` retain their narrow purposes.

## Package conventions

- Most fixtures use singular `{Subject}Test`; existing ConfigBrowser and `floatPTests` fixtures use plural names. Match the local fixture rather than renaming for uniformity.
- Editor unit tests use `GameLovers.GameData.Tests`; specialized directories and PlayMode add their existing namespace segments.
- Prefer internal seams from `Runtime/AssemblyInfo.cs` and `Editor/AssemblyInfo.cs` over private reflection.
- `ObservableResolverField<T>` is tested beside `ObservableField<T>` in `ObservableFieldTest`. Resolver list and dictionary types have dedicated fixtures.
- `ConfigsProvider.AddAllConfigs` and `UpdateTo` tests must use non-generic `System.Collections.IEnumerable` explicitly.
- Use NUnit classic assertions by default; use constraints when they express a real capability such as ignored-case matching or tolerance.

## Cleanup and performance

- Destroy every created Unity object and unsubscribe static/global listeners in teardown. Do not infer cleanup conventions from neighboring fixtures; inspect the specific test.
- Both test asmdefs reference `Unity.PerformanceTesting` directly, and the package declares that dependency. Do not make performance fixtures silently conditional.
- Performance tests reset state around each measurement iteration and include the workload inside the measured body.

## Verification

- Serialization work runs relevant Unit and Security fixtures. Observable changes run subscription/order tests. Unity-object changes run the PlayMode assembly.
- Update this guide only when a stable test placement, assembly, cleanup, or helper convention changes.
