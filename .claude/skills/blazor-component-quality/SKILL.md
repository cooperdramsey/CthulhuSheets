---
name: blazor-component-quality
description: "Review Blazor components for thin-view discipline, lifecycle hygiene, correct parameter/callback usage, and state management for the CthulhuSheets investigator-sheet app. Use when: reviewing .razor components, auditing component lifecycle, checking EventCallback and parameter patterns, reviewing component communication, checking IDisposable/unsubscription, Blazor component review."
argument-hint: "Path to component, feature folder, or plan to review"
---

# Blazor Component Quality Specialist

Review Blazor components for the thin-view rule, correct lifecycle usage, clean component communication, and disciplined state management. Components should **render state and forward intent** — domain logic lives in the domain/logic layer (`Models/`, `Helpers/`, `Data/`), and shared client state lives in injected services (`Services/`).

This skill is the Blazor counterpart of a "thin view" review: it covers component structure, lifecycle, parameters/callbacks, DI usage, and event subscription hygiene.

## Modes

This skill runs in one of two modes. Determine the mode from what is being reviewed:

- **Code Review Mode** (default) — the target is code changes (a diff, a feature folder, `.razor`/`.razor.cs` files). Apply the checklist below and report concrete issues at `file:line`.
- **Consulting Mode** — the target is a *plan* or design document. No code exists yet. Pressure-test the plan's proposed components: where logic will live, how components communicate, who owns state, and where subscriptions are torn down.

When invoked as part of an aggregated review, the orchestrator states which mode to run. Otherwise infer it: a plan/markdown design artifact → Consulting Mode; code → Code Review Mode.

## Checklist

### 1. Thin Components

**A component's `@code` should only: load/receive state, handle UI events by delegating, and expose values for markup.**

**Violations to flag:**
- Domain rules implemented inside a component (derived-stat formulas, skill-roll success-level computation, sanity-loss calculation belong in `Helpers/` — e.g. `CharacteristicHelper.cs`, `SanityRules.cs`, `SkillRules.cs`)
- Business decisions in markup (`@if` chains encoding CoC rules rather than rendering pre-computed state)
- Components building/shaping data that two or more components need (belongs in a shared service such as `InvestigatorService.cs`)
- `@code` blocks that dwarf the markup — move to a `.razor.cs` code-behind partial class; if still huge, the component is doing too much
- Copy-pasted markup+logic across components that should be a shared child component or `RenderFragment`

### 2. Lifecycle Hygiene

**Violations to flag:**
- Heavy work in `OnParametersSet(Async)` that re-runs on every parameter set without checking whether inputs actually changed
- `OnAfterRender(Async)` used without a `firstRender` check when only first-render setup is intended
- JS interop called from `OnInitialized` (too early in some render modes) instead of `OnAfterRender`
- Awaited work in lifecycle methods without handling the component-already-disposed case (guard callbacks after await if they touch state)
- Constructor logic in components (use lifecycle methods; constructors run before DI/parameters are ready)
- Loading state not represented — markup assumes data exists during the first render before `OnInitializedAsync` completes

### 3. Subscription and Disposal Lifecycle

**Correct pattern:**
```csharp
protected override void OnInitialized() => _state.Changed += OnStateChanged;
public void Dispose() => _state.Changed -= OnStateChanged;   // component declares @implements IDisposable
```

**Violations to flag:**
- Subscribing to a DI service's event (or `NavigationManager.LocationChanged`, a timer, etc.) without implementing `IDisposable`/`IAsyncDisposable` and unsubscribing — memory leak; the service outlives the component
- Lambda subscriptions that can never be unsubscribed: `_state.Changed += () => StateHasChanged();` — use a named method
- Subscribing in a method that can run repeatedly (e.g., `OnParametersSet`) causing duplicate handlers
- Event handlers calling `StateHasChanged` without `InvokeAsync` when the event may fire off the sync context (timers, external callbacks)
- Undisposed `CancellationTokenSource`, timers, or JS object references

### 4. Parameters and Callbacks

**Violations to flag:**
- Mutating an object received as `[Parameter]` — parameters flow down; changes flow up via `EventCallback`
- `[Parameter]` properties written by the component itself (fight with the renderer; copy to a local field instead)
- `Action`/`Func` used for component callbacks where `EventCallback`/`EventCallback<T>` belongs (EventCallback handles `StateHasChanged` and async)
- Missing `@key` on list items that reorder/insert
- Two-way binding (`@bind-Value`) implemented without the matching `ValueChanged` convention when hand-rolled
- Component reaching into another component instance (`@ref` used to call sibling logic) instead of communicating via shared service or common parent
- Cascading parameters used for values that only one child needs (pass directly)

### 5. State Management

**Violations to flag:**
- The same state duplicated in multiple components and manually synchronized (single source of truth: a shared scoped service or the common parent)
- Shared client state held in `static` fields instead of a DI service
- Component state that should survive navigation held in the component (lost on dispose) instead of a service (e.g. `InvestigatorService.cs`)
- Persistence concerns (localStorage/IndexedDB keys, JSON shapes) embedded in components instead of the storage service (`ICharacterStore`, `IndexedDbCharacterStore`, `LocalStorageCharacterStore`)
- Registering services with the wrong lifetime (in WASM, `Scoped` ≈ `Singleton` per app instance — but choose intentionally and consistently)

## Decision Framework

| Observation | Action |
|---|---|
| Domain logic in a component | Critical — move to Domain layer (`Helpers/`, `Models/`) |
| Event subscription without Dispose unsubscribe | Critical — implement IDisposable, unsubscribe |
| Parameter object mutated by child | Critical — EventCallback up, or copy-in |
| Lambda subscription to long-lived service | Major — named method + unsubscribe |
| Duplicated state synchronized by hand | Major — single source of truth |
| Heavy un-guarded work in OnParametersSet | Warning — diff inputs before recomputing |
| Missing loading render state | Warning — add null/loading branch |
| Large `@code` block inline | Suggestion — move to code-behind |

**When exceptions are acceptable:**
- Purely presentational computed properties (string formatting, CSS class selection) belong in the component — that *is* view logic
- Tiny page components may keep small `@code` blocks inline
- `@ref` is fine for imperative UI concerns (focus, scroll) — just not for logic

## Procedure

1. Read all `.razor`/`.razor.cs` files in the target
2. For each component, classify its `@code` members: rendering support (fine), event forwarding (fine), domain logic (flag), shared-state shaping (flag)
3. Audit lifecycle methods against the checklist; verify loading states exist for async data
4. Inventory every event subscription, timer, CTS, and JS interop handle; verify a matching teardown in `Dispose`
5. Check every `[Parameter]`: is it mutated? Written internally? Should it be `EventCallback`?
6. Map component communication: parameters down, callbacks up, services across — flag reach-ins
7. Locate all client state: who owns it, is there exactly one source of truth, does its lifetime match its need?
8. Categorize: **Critical** (logic in views, leaks, parameter mutation), **Warning** (lifecycle misuse, missing states, churn), **Suggestion** (organization, code-behind)

## Consulting Mode (Plan Review)

Get component responsibilities and communication right on paper — moving logic out of a component is much cheaper before it's written. Name the proposed component, service, or interaction from the plan.

**Interrogate the plan for:**
- **Logic placement**: For each planned component, does the plan state what logic it owns? Anything resembling CoC rules (derived stats, roll resolution, sanity loss) must be assigned to `Helpers/` or `Models/`, with the component consuming results.
- **Communication paths**: For each component-to-component interaction, does the plan specify the mechanism (parameter, EventCallback, shared service)? Flag implied sibling reach-ins or parameter mutation.
- **State ownership**: For each piece of client state (current investigator, character-creation progress, roster), does the plan name a single owner and its lifetime (component vs. scoped service vs. persisted via `ICharacterStore`)?
- **Subscription lifecycle**: For each planned subscription to a shared service or external source, does the plan name the teardown point?
- **Loading/error states**: Do planned data-driven components account for loading, empty, and error renders?

**For each gap, propose a concrete remediation to the plan**: assign the logic a Domain home, specify the communication mechanism, name the state owner, or add the Dispose contract.

### Consulting Output Format

```
## Blazor Component Quality Plan Review: [Plan/Feature Name]

### Proposed Component Map
| Component | Owns State? | Logic Location | Communication | Concern |
|-----------|-------------|----------------|---------------|---------|
| X.razor   | investigator | Helpers/Domain  | EventCallback | clean / flagged |

### Gaps (must address before implementation)
- **[Category]**: [What the plan gets wrong or leaves undefined]
  - Risk: [Leak / coupling / logic-in-view if built as planned]
  - Remediation: [Assignment/mechanism/lifecycle to specify in the plan]

### Concerns (should address)
- **[Category]**: [Description]
  - Remediation: [Plan change]

### Recommendations (consider)
- **[Category]**: [Description]
  - Note: [Why it helps]

### Summary
- Gaps: N | Concerns: N | Recommendations: N
- Design readiness: [Ready / Needs Revision / Major Gaps]
```

## Output Format

```
## Blazor Component Quality Review: [Feature/File]

### Critical Issues
- **[Category]**: [file:line]
  - Problem: [Concrete description]
  - Fix: [Specific change]

### Warnings
- **[Category]**: [file:line]
  - Problem: [Description]
  - Fix: [Approach]

### Suggestions
- **[Category]**: [file:line]
  - Note: [What and why]

### Summary
- Critical: N | Warnings: N | Suggestions: N
- Component health: [Thin & Clean / Needs Attention / Logic-Heavy]
```
