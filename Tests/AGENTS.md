# GameLovers.GameData Tests — AI Agent Guide

This file contains testing conventions for the `com.gamelovers.gamedata` package. It is the source of truth when reading, editing, or creating test files under `Tests/`.

For runtime architecture, gotchas, and package-level context, see the parent [`AGENTS.md`](../AGENTS.md).

§1 and §2 are shared verbatim across every GameLovers package. A change to either must be applied to all six `Tests/AGENTS.md` files in the same working session, one commit per submodule.

## 1. ADMIT — Test Admission Test

A proposed test is admitted only if all five answers are YES. Record the first two
as comments on the test itself.

| | Question |
|---|---|
| **A1 DEFECT** | Can you name the defect in one sentence, referencing a production file and symbol? "It could break" is not a defect. |
| **A2 RED** | Can you name the exact production edit — one line or one branch, identified by `file` + `symbol` — that makes this test fail? If no such single edit exists, the test pins nothing. |
| **A3 PACKAGE** | Does every assertion read a value this package computed? Reject assertions on `new X() != new X()`, `!= null` on a freshly constructed object, default struct/enum values, or anything the C# spec or the Unity engine already guarantees. |
| **A4 CHEAPEST** | Is this the cheapest tier that covers the defect? EditMode beats PlayMode; a `[TestCase]` row on an existing fixture beats a new `[Test]`; a new `[Test]` beats a new fixture. Grep before writing. |
| **A5 UNIQUE** | Does no existing test already fail on the A2 edit? Grep the symbol under test across `Tests/` first. |

**A5-bis — inherited-type coverage.** Before proposing a fixture for a type that
derives from or wraps another tested type, grep `Tests/` for the derived type's
name and for paired `[SetUp]` fields. Base-and-derived pairs are tested jointly in
the base's fixture unless the derived type adds new public surface.

**Two mechanical disqualifiers** — violate one and the test is rejected:

- **D1 — tautology.** If the only assertion is `Assert.DoesNotThrow`,
  `Assert.IsNotNull`, or a disjunction of `Contains(...)` substrings, the test
  fails A2 unless you write down what *would* throw, be null, or not match. A
  substring disjunction that includes a string the input itself embeds is
  unfalsifiable by construction.
- **D2 — name/body contract.** The test name is a claim. If deleting the
  production feature the name mentions leaves the test green, the name is a lie.

**Smoke exemption, by directory.** Fixtures under `Smoke/` are exempt from A1 and
A2 and may assert construction-without-throwing only. Their defect class is "the
assembly no longer loads / bootstrap regressed", which is real and not expressible
otherwise. The exemption is by directory, not by assertion shape — a Unit test
that only asserts `IsNotNull` is still rejected.

## 2. RCR — Revert and Confirm Red

> Every new or strengthened test must be observed failing, once, against a
> one-line production revert, before it is committed.

Line coverage proves a line executed. It does not prove any test would notice if
that line were wrong. RCR is the cheap substitute for mutation testing, and it is
what makes a coverage number trustworthy.

**Procedure** (~90 seconds per test):

1. Write the test. Run it. Green.
2. Apply the A2 edit — invert the comparison, delete the guard clause, return
   early, comment out the one line. **One line only**: a broad deletion proves
   nothing, because it would also "fail" a tautological test via a compile error.
3. Run only that test. It must be **RED**, and the failure message must name the
   thing you broke. A red-by-`NullReferenceException` does not count — that is the
   test crashing, not asserting.
4. `git checkout -- <production file>`. Re-run. Green.
5. Record the mutation in the test's header comment.

**Recording format** — on the test, not in a separate ledger. A ledger rots the
moment a test is renamed; a comment travels with the test, appears in every diff
that touches it, and lets a reviewer re-run the mutation in 30 seconds.

```csharp
[Test]
// ADMIT: <one-sentence defect, naming a production file and symbol>
// RCR:   <file> <symbol> — <the one-line mutation> → RED (<what the failure says>). <YYYY-MM-DD>
public void Method_Condition_ExpectedResult()
```

**Anchor on `file` + `symbol`, never `file:line`.** Line numbers rot on the first
unrelated edit above them — a stale `:474` pointing at a method that moved to `:464`
sends the next reader to the wrong code and quietly destroys the comment's value.

**Budget: four lines is the target, six is the ceiling.** One sentence of ADMIT,
one of RCR, wrapped. This obeys the repo-wide rule in the root `AGENTS.md`
(§ Code comments): *"One sentence usually suffices. Multi-paragraph rationale is a
smell."* Anything past the ceiling belongs in the commit body or `docs/`, not on the
test. Two things in particular must NOT appear here:
- **Change narration.** *"An earlier version of this test was a tautology"* is diff
  context; the root `AGENTS.md` forbids it outright. A comment states the code's
  permanent condition, not its history. Put it in the commit message.
- **Investigation transcript.** The empirical detail that convinced *you* is not
  what the next reader needs. They need the mutation and the expected failure.

The one extension worth its lines is a **negative** result: naming a nearby edit
that looks like a valid mutation but is NOT one (because it is already guarded, or
because it reddens a sibling test instead). That stops the next reader repeating a
dead end, and it cannot be recovered from the code.

Also add one line per new test to the commit body: `RCR: <TestName> ← <file> <symbol> <mutation>`.
That makes `git log --grep=RCR` the audit surface.

**UNFALSIFIABLE — the one honest exemption.** Some correct tests provably have no
one-line mutation. The commonest case is **double-guarded validation**: an
unconfigured object trips two independent guards, so disabling either leaves the
other throwing. Deleting such a test would lose real coverage, so it is exempt —
but only on the same terms as §13, never as a shrug:

```csharp
// RCR: none exists — <input> trips both <guard A> and <guard B>; disabling either
// leaves the other throwing (verified). Double-covered, not single-line falsifiable.
```

The reason must be falsifiable and must record that a mutation was actually tried
and observed green. "Couldn't find one" is not a reason — that is an unfinished RCR,
not an exemption.

**Verdicts for a test that resists mutation.** Work out which of four it is; they
have different answers:

| Finding | Test | Action |
|---|---|---|
| **A3 reject** — no line in `Runtime/` or `Editor/` participates; the assertion is C#- or Unity-guaranteed | pins nothing, ever | **Delete** |
| **A5 duplicate** — the only mutation that reddens it already belongs to a sibling | pins nothing new | **Delete**, naming the surviving sibling in the commit body |
| **D2 overclaim** — the name promises behaviour the body cannot detect | name is a lie | **Strengthen the assertion**, or rename to what it actually checks |
| **UNFALSIFIABLE** — real behaviour, but double-guarded or otherwise unbreakable one line at a time | valid | **Keep**, with the exemption comment above |

**A3 is checked first, and it is the commonest way the exemption gets abused.**
UNFALSIFIABLE is for behaviour this package genuinely owns but cannot be broken one
line at a time. It is *never* for behaviour the package does not own. The tell is in
the reason itself: if you find yourself writing "no line in Runtime/ participates",
"the only edit is a compile error", or "these are C#'s zero-init values", you have
found an A3 reject and the verdict is **delete** — a field-only struct's assignment
and default values are the language's guarantees, not yours. Writing that sentence
under an UNFALSIFIABLE heading launders a test §1 would never have admitted.

Prove the class before acting. An A5 duplicate is confirmed when the sibling's
mutation is observed reddening both; a D2 overclaim is confirmed when the mutation
the name implies leaves the test green; an A3 reject is confirmed when no production
symbol appears anywhere in the causal chain behind the assertion.

**Two consequences, stated so RCR does not become theatre:**

- A test with no `// RCR:` line — and no UNFALSIFIABLE exemption — is not trusted
  coverage. In an audit it is a suspect by default.
- **Benchmarks are included, inverted:** a performance test must be observed
  *changing its number* when the measured operation is removed from the measured
  body. A benchmark whose measured region does not contain the workload is a
  tautology in `Measure` clothing.

## 3. Placement Rules

The test root is `Tests/Editor/` — **not** `EditMode/` — plus `Tests/PlayMode/`. This
package is the only one of the six with `Security/` and `Boundary/` subfolders.

| Directory | Contents |
|---|---|
| `Editor/Unit/` | Pure logic: providers, serializer, observables, math (`floatP`), enum/type serialization helpers, validation attributes, migrations. Preferred tier — see A4. |
| `Editor/Unit/ConfigBrowser/` | Editor-tooling internals with extractable logic: `ConfigTreeBuilder`, `ConfigValidationService`, `ConfigExportService`, `ConfigsEditorUtil`. Same namespace as `Editor/Unit/` (no sub-namespace) — the subfolder is organizational only. |
| `Editor/Integration/` | Cross-type editor/tooling interactions (e.g. `ConfigsProvider` + `ConfigsSerializer` round-trips, `ObservableComputedField` composition). |
| `Editor/Security/` | Serializer safety expectations (`ConfigsSerializer` trust modes, `ConfigTypesBinder` whitelist enforcement). |
| `Editor/Boundary/` | Edge-case / limit conditions (`ConfigsProvider` boundary inputs, `floatP` boundary values). |
| `Editor/Performance/` | Allocation / hot-path measurements only — not general coverage. |
| `Editor/Smoke/` | Assembly-loads / bootstrap-only checks. Smoke exemption from §1 applies. |
| `PlayMode/Smoke/` | PlayMode assembly-loads / bootstrap-only checks. |
| `PlayMode/Integration/` | Tests requiring a running scene (e.g. `ConfigsScriptableObject` lifecycle). |
| `PlayMode/Performance/` | PlayMode allocation / hot-path measurements. |

Use EditMode (`Tests/Editor/`) unless the code under test needs a running scene or
Unity coroutine/frame timing — see root `AGENTS.md` §6.

## 4. Namespace and Suppression

All test files use `namespace GameLovers.GameData.Tests` for `Editor/` (including
the `Unit/` and `Unit/ConfigBrowser/` subfolders — the `ConfigBrowser/` folder does
**not** add a namespace segment). The remaining `Editor/` subfolders each get a
dotted child namespace matching their directory:

- `GameLovers.GameData.Tests.Boundary`
- `GameLovers.GameData.Tests.Integration`
- `GameLovers.GameData.Tests.Performance`
- `GameLovers.GameData.Tests.Security`
- `GameLovers.GameData.Tests.Smoke`

`PlayMode/` mirrors the same pattern under an additional `.PlayMode` segment:

- `GameLovers.GameData.Tests.PlayMode.Smoke`
- `GameLovers.GameData.Tests.PlayMode.Performance`
- `GameLovers.GameData.Tests.PlayMode.Integration`

No `// ReSharper disable once CheckNamespace` suppression comment is used anywhere
in this package's tests — unlike `com.gamelovers.mobileservices`, the namespace
here does not collide with anything that would need suppressing.

## 5. Naming

- **Test class**: `{TypeName}Test` (singular) for the overwhelming majority of
  fixtures — e.g. `ConfigsProviderTest`, `ObservableFieldTest`. **Known
  inconsistency, do not "fix" it**: the 4 fixtures under `Editor/Unit/ConfigBrowser/`
  use the plural `{TypeName}Tests` suffix (`ConfigExportServiceTests`,
  `ConfigTreeBuilderTests`, `ConfigValidationServiceTests`,
  `ConfigsEditorUtilTests`), and `Editor/Unit/floatPTests.cs` also uses the plural
  form despite living outside `ConfigBrowser/`. Match whichever suffix the sibling
  fixtures in the same file/type already use; do not rename existing files to
  "normalize" the suffix.
- **Test method**: `MethodOrBehavior_Condition_ExpectedResult` — e.g.
  `AddSingletonConfig_Success`, `UpdateTo_SetsVersionAndMergesConfigs`.
- **SetUp method**: named `Setup()` in 13 files (majority) and `Init()` in 5 files
  (`ObservableFieldTest`, `ObservableDictionaryTest`, `ObservableListTest`,
  `ObservableResolverDictionaryTest`, `EnumSelectorTest`). Both names are in active
  use — match the sibling fixture's convention (observable-family fixtures tend to
  use `Init()`) rather than renaming existing `[SetUp]` methods.
- **TearDown method**: none exist. Zero `[TearDown]` methods anywhere in this
  package's tests today. This is a **known weakness**, not a convention to
  preserve — see §10.

## 6. Mock / Helper Types

- Shared builders live in `Tests/Editor/Unit/TestDataBuilders.cs`
  (`ConfigsProviderBuilder` and any sibling builder types) — check there before
  writing new construction boilerplate.
- **NSubstitute** (`Substitute.For<T>()`) is used in 7 files:
  `ObservableHashSetTest.cs`, `ObservableListTest.cs`,
  `ObservableResolverListTest.cs`, `ObservableDictionaryTest.cs`,
  `ObservableBatchTest.cs`, `ComputedFieldTest.cs`, `ObservableFieldTest.cs` — all
  observable-family fixtures, mocking observer/callback delegates.
- Nested mock config structs/classes (e.g. `MockSingletonConfig`,
  `MockCollectionConfig`, `MockValidatableConfig` / `MockValidatableConfigAlt` — see
  root `AGENTS.md` §4 for why the `Alt` sibling exists) are declared as nested
  types inside the consuming test class, following the pattern in
  `ConfigsProviderTest.cs`.

**Resolver / derived observable field types — anti-false-positive rule (replicated
from root `AGENTS.md` §6; do not merely link it, this exact rule has caused
LLM-driven audit false positives):**

`ObservableResolverField<T>` is tested **jointly** with its base class
`ObservableField<T>` via **paired `[SetUp]` fields** in `ObservableFieldTest.cs`
(fields `_observableField` and `_observableResolverField`), **not** in a dedicated
`ObservableResolverFieldTest.cs` file — no such file exists, and none should be
created. Most `[Test]` methods exercise both fields with a single assertion pair
(see `ValueCheck`, `ValueSetCheck`, `ObserveCheck`, `RebindCheck_KeepsObservers`).
Before proposing or accepting "missing coverage" for `ObservableResolverField<T>`,
grep `Tests/Editor/Unit/` for `_observableResolverField` / `ObservableResolverField<`
references first.

**This is the opposite arrangement from the list/dictionary resolver types.**
`ObservableResolverListTest.cs` and `ObservableResolverDictionaryTest.cs` **do**
exist as dedicated, standalone fixtures (they are not paired inside
`ObservableListTest.cs` / `ObservableDictionaryTest.cs`). State explicitly which
arrangement applies to which type so a future audit does not "fix" the field
variant into a dedicated file, or "fix" the list/dictionary variants into a
paired-`[SetUp]` shape — both directions would be wrong:

| Type | Arrangement |
|---|---|
| `ObservableResolverField<T>` | Paired `[SetUp]` fields inside `ObservableFieldTest.cs` |
| `ObservableResolverListTest` | Dedicated standalone fixture (own file) |
| `ObservableResolverDictionaryTest` | Dedicated standalone fixture (own file) |

## 7. Black-Box / Reflection Policy

- **Prefer the package's internal test seams over `BindingFlags.NonPublic`
  reflection.** `Runtime/AssemblyInfo.cs` and `Editor/AssemblyInfo.cs` both declare
  `[assembly: InternalsVisibleTo("GameLovers.GameData.Editor.Tests")]`, exposing:
  - `EnumSelector<T>.SetSelectionString(string)` — simulates a stale serialized
    enum-name string that the public `SetSelection(T)` API can't reach.
  - `SerializableType<T>.FromSerializedNames(className, assemblyName)` — static
    factory simulating Unity's deserialization order without reflection or the
    struct-boxing dance (see root `AGENTS.md` §4 on why
    `OnAfterDeserializeImpl` exists).
  - `UnitySerializedDictionary<TKey,TValue>.SetSerializedLists(List<TKey>, List<TValue>)`
    plus the read-only `KeyDataInternal` / `ValueDataInternal` accessors — read/write
    the YAML-serializer-populated backing lists.
- Reach for one of these seams before reaching for `BindingFlags.NonPublic`. If a
  genuinely private code path has no seam and no black-box entry point, treat it
  as untestable and record it in §13 (reason iii) rather than adding reflection.

## 8. Fields and Setup

- Fields are prefixed with `_` and use concrete types (e.g. `private ConfigsProvider _provider;`).
- `[SetUp]` builds fresh instances per test — see §5 for the `Setup()` / `Init()` naming split.
- **`ConfigsProvider.AddAllConfigs` / `UpdateTo` require fully-qualified
  `System.Collections.IEnumerable`.** Both methods take
  `IReadOnlyDictionary<Type, IEnumerable>` with the **non-generic**
  `System.Collections.IEnumerable`. When a test file also has `using
  System.Collections.Generic;` in scope, write the fully-qualified
  `System.Collections.IEnumerable` to avoid resolving to `IEnumerable<T>` — see the
  `UpdateTo_*` tests in `ConfigsProviderTest.cs` for the working pattern.

## 9. Assertion Style

Classic NUnit asserts dominate (`Assert.AreEqual`, `Assert.IsTrue`,
`Assert.Throws<T>`, etc.) across essentially all fixtures. The constraint model
(`Assert.That(...)`) appears in exactly 3 places, all inside
`ValidationAttributesTest.cs`, all using `Does.Contain(...).IgnoreCase`. Do not
introduce `Assert.That` elsewhere without a specific reason (e.g. a genuine need
for `IgnoreCase` substring matching) — it would be inconsistent with the rest of
the package.

## 10. PlayMode Test Cleanup

**Zero `[TearDown]` methods exist anywhere in this package's tests** (Editor or
PlayMode). This is flagged as a known weakness rather than the convention: any
PlayMode fixture that instantiates a `ConfigsScriptableObject` or other
Unity-object-backed type should add teardown (e.g. `Object.Destroy`) when adding
new tests, but do not assume existing fixtures already do this — check the
specific fixture before relying on ordering/isolation between tests.

## 11. Performance Tests

`Editor/Performance/` and `PlayMode/Performance/` fixtures measure allocations and
hot paths (`ConfigsProviderPerformanceTest`, `ObservablePerformanceTest`,
`floatPPerformanceTest`, `RuntimePerformanceTest`) using `Unity.PerformanceTesting`.

**Dependency, resolved 2026-08-02**: both `Tests/Editor/GameLovers.GameData.Editor.Tests.asmdef`
and `Tests/PlayMode/GameLovers.GameData.PlayMode.Tests.asmdef` reference
`Unity.PerformanceTesting` **unconditionally**, so `package.json` now declares
`com.unity.test-framework.performance` (`3.5.0`) as a dependency. Previously it did
not, and any consumer without the Performance Testing package already installed hit
a missing-assembly compile error in this package's test assemblies.

The trade-off taken: the dependency is a plain `dependencies` entry, so it resolves
into every consumer project, not only ones that run these tests — UPM has no
test-only dependency scope for regular packages. The alternative (guarding the
asmdef references with `defineConstraints`) was rejected because it makes the
performance fixtures silently vanish when the package is absent rather than fail
loudly. If you ever add a perf fixture to a package that must stay dependency-free,
isolate it in its own asmdef rather than reintroducing the undeclared reference.

## 12. Test Directory Layout

```
Tests/
  Editor/
    Unit/                  # pure logic (preferred tier)
      ConfigBrowser/       # editor-tooling internals (same namespace as Unit/)
    Integration/           # editor/tooling cross-type interactions
    Security/              # serializer safety
    Boundary/              # edge-case / limit conditions
    Performance/           # allocations / hot paths
    Smoke/                 # assembly-loads / bootstrap only
  PlayMode/
    Smoke/                 # assembly-loads / bootstrap only
    Integration/            # running-scene / Unity coroutine flows
    Performance/            # allocations / hot paths
```

## 13. Coverage Register

Every untested symbol worth naming is either ACCEPTED (justified — do not
re-report) or OPEN (a real gap, owed a test). An untested symbol in neither state
is an audit finding.

An ACCEPTED row needs one of exactly three falsifiable reasons:
- **(i) no branching** — zero conditionals, so there is no behaviour to pin.
- **(ii) engine-owned** — the assertion would target Unity/OS behaviour
  (`[DllImport]`, `AndroidJavaObject`, Addressables statics).
- **(iii) harness-impossible** — the state cannot be fabricated in EditMode or
  PlayMode, **with the specific blocker named**.

"Low value", "hard to test", and "covered by manual QA" are NOT valid reasons. If
none of the three applies, the row is OPEN.

ACCEPTED is dated and **expires on edit**: if the symbol's file changes, the
reason is re-checked in that PR. A `(i) no branching` row is void the moment
someone adds an `if`.

OPEN is the only place a deletion may park coverage. A test removed for weakness
either had a stronger sibling (named in the commit body) or leaves an OPEN row.
The count of OPEN rows is the honest coverage-debt number.

| Symbol (file:line) | State | Reason / Owed | Recorded |
|---|---|---|---|
| `ConfigBrowserWindow` (`Editor/Windows/ConfigBrowserWindow.cs`) | ACCEPTED | **(iii) harness-impossible** — blocker: no `EditorWindow` test harness exists for this package (no UIToolkit visual-tree / window-lifecycle test infrastructure). The extractable logic this window drives already lives in the tested internals `ConfigTreeBuilder`, `ConfigValidationService`, `ConfigExportService`, and `ConfigsEditorUtil` (see `Editor/Unit/ConfigBrowser/*Tests.cs`). | 2026-07-31 |
| `ObservableDebugWindow` (`Editor/Windows/ObservableDebugWindow.cs`) | ACCEPTED | **(iii) harness-impossible** — blocker: no `EditorWindow` test harness exists for this package; driving the live-observable inspection UI would require a UIToolkit visual-tree/window-lifecycle test rig this package does not have. | 2026-07-31 |
| `ConfigsScriptableObjectInspector` (`Editor/Inspectors/ConfigsScriptableObjectInspector.cs`) | ACCEPTED | **(iii) harness-impossible** — blocker: `UnityEditor.Editor`-derived Inspector classes need a live Inspector window / `Editor.CreateEditor` render pass, which this package's test harness cannot fabricate. | 2026-07-31 |
| 4 `VisualElement` classes under `Editor/Elements/*` (incl. `Editor/Elements/MigrationPanel/*`) | ACCEPTED | **(iii) harness-impossible** — blocker: no UIToolkit visual-tree test harness; these are thin composition wrappers over the tested `ConfigTreeBuilder` / `ConfigsEditorUtil` internals. | 2026-07-31 |
| Both `PropertyDrawer` types under `Editor/*` | ACCEPTED | **(iii) harness-impossible** — blocker: `PropertyDrawer.OnGUI`/`CreatePropertyGUI` require a live Inspector/`SerializedProperty` draw pass, which cannot be fabricated in EditMode or PlayMode without an Inspector test harness. | 2026-07-31 |

### OPEN

| Symbol (file:line) | State | Reason / Owed | Recorded |
|---|---|---|---|

## 14. Update Policy

Update this file when:
- Test conventions change (new asmdef references, assertion style, naming patterns, new test categories)
- New test directories or categories are added
- Mock/stub patterns change (e.g., NSubstitute added to a new fixture family)
- A coverage gap from §13 becomes testable (e.g., an `EditorWindow` test harness is adopted and a Coverage Register row moves from ACCEPTED to tested)
- The `Unity.PerformanceTesting` dependency arrangement in §11 changes (e.g. a perf
  fixture moves to its own asmdef, or the dependency is scoped differently)
