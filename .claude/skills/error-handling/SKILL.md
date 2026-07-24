---
name: error-handling
description: "Review C#/.NET and Blazor code for error handling quality and robustness. Use when: checking null safety, auditing boundary validation, reviewing error propagation, checking graceful degradation, async error handling review."
argument-hint: "Path to file, feature folder, or plan to review"
---

# Error Handling & Robustness Specialist

Review C# and Blazor code for appropriate error handling at system boundaries, null safety at integration points, graceful degradation, and clear failure signals. The goal is code that fails loudly in development and fails gracefully in production — never silently corrupts a saved investigator sheet.

## Modes

This skill runs in one of two modes. Determine the mode from what is being reviewed:

- **Code Review Mode** (default) — the target is code changes (a diff, a feature folder, a set of `.cs`/`.razor` files). Apply the checklist below and report concrete issues at `file:line`.
- **Consulting Mode** — the target is a *plan* or design document. No code exists yet. Pressure-test the plan for **error handling, null safety, and robustness**: find the failure cases, boundaries, and invalid states the plan leaves unaddressed, and propose handling before code is written.

When invoked as part of an aggregated review, the orchestrator states which mode to run. Otherwise infer it: a plan/markdown design artifact → Consulting Mode; code → Code Review Mode.

## System Boundaries in This Project

Error handling belongs at boundaries. Here they are:

- **Browser storage**: localStorage/IndexedDB reads via `ICharacterStore` — missing keys, stale/renamed schema fields, corrupted JSON, saved-character deserialization failures. A corrupt save must not crash the app; it must fall back gracefully.
- **User input**: character-creation forms (`CreationCharacteristicsStep`, `CreationOccupationSkillsStep`, `CreationWealthStep`, …), characteristic point-buy, skill point allocation, manual stat edits. Invalid values must be caught at the boundary, not allowed to corrupt the investigator model.
- **JS interop**: IndexedDB operations go through JS interop and can throw `JSException` — these calls require try/catch and a clear fallback path.
- **Static rules data**: values in `Data/Occupations.cs` and `Data/DefaultSkills.cs` are code, not external files, so build-time errors catch typos — but a wrong skill base value or occupation credit-rating range silently corrupts every character that uses it. Use the `rules-review` skill to audit rules-fidelity of static data.

## Checklist

### 1. Null Safety at Boundaries

**Violations to flag:**
- Dereferencing deserialized objects (storage reads, JS interop results) without null/shape validation
- Chained access across nullable results: `investigator.Skills[0].Specialization.Name` without checking each link
- Nullable reference type warnings suppressed with `!` where null is genuinely possible
- Blazor `[Parameter]` properties assumed non-null without a guard or a `[EditorRequired]` modifier
- Passing null into a method that does not handle null for that parameter

**When null checks are NOT needed (over-validation to avoid):**
- Values assigned in the same scope before use
- Non-nullable value types
- After a null check has already been performed in the same scope
- Domain-internal calls where the caller enforces invariants at a known entry point

### 2. Silent Failures

**Violations to flag:**
- Empty `catch` blocks: `catch (Exception) { }` with no log or re-throw
- `catch` blocks that log but continue as if nothing happened, leaving investigator state corrupted
- Methods returning `null`/`default` as a failure signal without documenting the condition
- `async void` methods (exceptions are unobservable — use `async Task` except for event handlers that Blazor owns)
- Fire-and-forget tasks (`_ = DoAsync()`) without exception observation
- Storage failures swallowed in components — the UI shows stale or empty state with no message
- `if (x == null) return;` in methods where the caller expects a result or side effect

### 3. Validation and Fail-Fast

**Violations to flag:**
- Missing guard clauses at public domain entry points for arguments that would corrupt results
- Logic continuing after detecting an invalid state instead of stopping early
- Static rules data that could be misconfigured — an occupation with a null skill list, a negative credit-rating bound, a characteristic default outside 1–99 — with no guard at initialization
- State transitions that don't verify preconditions (e.g., advancing the character-creation step before required fields are populated)

**Good patterns to recognize:**
```csharp
// Fail fast with a message that names the data
ArgumentNullException.ThrowIfNull(investigator);
ArgumentOutOfRangeException.ThrowIfNegative(characteristic.Value);

// Guard at a public domain entry point
if (occupation is null)
    throw new ArgumentException($"Occupation '{occupationId}' was not found in Data/Occupations.cs");
```

### 4. Graceful Degradation in Production

**Violations to flag:**
- Unhandled exceptions in component lifecycle methods or event handlers that blow away the UI instead of rendering an error state
- Missing loading/error states in components that await data (`if (_investigator is null)` render path)
- Storage read failures crashing the app instead of falling back to a default investigator or an error message (a corrupt saved sheet must not brick the app)
- JS interop (`JSException`) propagating unhandled out of `IndexedDbCharacterStore` into Blazor without a fallback to `LocalStorageCharacterStore` or an explicit error UI

**When to propagate vs. recover:**
- In development: fail loudly — throw with a precise message, log details
- In production: log, show a friendly error state, fall back to safe defaults where possible
- **Never**: silently continue with corrupted investigator state

### 5. Error Propagation Contracts

**Violations to flag:**
- Methods returning `bool` for success/failure where callers never check the result
- Inconsistent failure contracts across siblings (one returns null, one returns empty, one throws)
- `EventCallback` invocations that assume a subscriber exists for correctness
- Async methods that complete normally even when their core operation failed
- `ICharacterStore` implementors with differing failure behavior not captured in the interface contract

## Decision Framework

| Observation | Action |
|---|---|
| Empty catch block | Critical — log and decide: recover or re-throw |
| `async void` on a non-event-handler | Critical — change to `async Task` |
| Storage deserialization not guarded against null/missing fields | Critical — add null coalescing or migration via `StorageMigrator` |
| Silent return on precondition failure | Warning — log or throw before return |
| Component awaits data with no loading/error render path | Warning — add both states |
| Unchecked bool return value | Warning — check it or change the contract |
| Over-validation (null check on always-set value) | Suggestion — remove if provably unnecessary |

**When error handling should be minimal:**
- Internal private methods where the same class guarantees invariants
- Pure domain functions whose callers validate at the entry point
- Test code

## Procedure

1. Read all files in the target feature or file set
2. Identify all boundaries touched: browser storage (IndexedDB/localStorage via `ICharacterStore`), user input, JS interop
3. At each boundary, check null safety, validation, and error signaling
4. Search all `catch` blocks — classify: handle-and-continue, log-and-return, re-throw, or empty (flag empty)
5. Search for `async void` and fire-and-forget tasks
6. Check methods returning `null`/`false`/`default` — verify the failure case is documented and handled by callers
7. Check components for loading/error/empty render states
8. Categorize: **Critical** (silent failures corrupting investigator state, unguarded storage reads, `async void`), **Warning** (missing boundary checks, unchecked returns, missing UI error states), **Suggestion** (better messages, low-risk guards)
9. For each finding, specify the exact risk and the exact fix

## Consulting Mode (Plan Review)

Surface the failure modes the plan hasn't thought about — plans describe the happy path; enumerate the unhappy ones. Name the proposed step, boundary, or data source from the plan.

**Interrogate the plan for:**
- **New boundaries**: What new storage keys, input surfaces, or JS interop calls does the plan create? Does it say what happens when each receives bad or missing data?
- **Failure cases**: For each operation, what happens on failure? Does the plan specify dev-time fail-fast vs. production fallback?
- **Persistence integrity**: If the plan changes what's saved to localStorage/IndexedDB (new fields on `Investigator`, `Skill`, `Weapon`, etc.), does it address deserializing existing saved data? Does `StorageMigrator` need updating, or do new fields need `[JsonIgnore]`-safe defaults?
- **State integrity**: Could a failure mid-operation leave investigator state partially mutated? Is there a safe ordering?
- **Signaling contract**: For new fallible methods or storage operations, does the plan say how failure is reported and who handles it?

**For each gap, propose a concrete remediation to the plan**: a validation step, a defined fallback, an explicit contract, or reordering so partial failure can't corrupt a saved sheet.

### Consulting Output Format

```
## Error Handling Plan Review: [Plan/Feature Name]

### Gaps (must address before implementation)
- **[Category]**: [Failure case or boundary the plan ignores]
  - Risk: [What goes wrong if unhandled]
  - Remediation: [Validation/fallback/contract to add to the plan]

### Concerns (should address)
- **[Category]**: [Description]
  - Remediation: [Plan change]

### Recommendations (consider)
- **[Category]**: [Description]
  - Note: [Why it strengthens robustness]

### Summary
- Gaps: N | Concerns: N | Recommendations: N
- Design readiness: [Ready / Needs Revision / Major Gaps]
- Failure-path coverage: [Good / Partial / Happy-path only]
```

## Output Format

```
## Error Handling Review: [Feature/File]

### Critical Issues
- **[Category]**: [file:line]
  - Risk: [What goes wrong in the failure case]
  - Fix: [Specific code to add/change]

### Warnings
- **[Category]**: [file:line]
  - Risk: [Description]
  - Fix: [Approach]

### Suggestions
- **[Category]**: [file:line]
  - Note: [What and why it would improve robustness]

### Summary
- Critical: N | Warnings: N | Suggestions: N
- Boundary safety: [Good / Needs Attention / Poor]
```
