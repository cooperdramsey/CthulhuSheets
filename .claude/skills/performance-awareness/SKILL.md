---
name: performance-awareness
description: "Review Blazor WASM code for performance anti-patterns in the CthulhuSheets investigator-sheet app. Use when: auditing render paths, checking for unnecessary re-renders, reviewing localStorage/IndexedDB access patterns, checking payload size, async correctness, allocation pressure, performance review."
argument-hint: "Path to file, feature folder, or plan to review"
---

# Performance Awareness Specialist

Review Blazor WASM code for performance anti-patterns. This is a **Blazor WebAssembly app**: the runtime is client-side .NET in the browser, so the hot concerns are **render churn** (unnecessary component re-renders and work performed inside render), **startup/payload size**, **repeated storage reads**, and **async correctness** — not frame-time GC as in a game. This is a personal-project character-sheet app; be proportionate — flag real costs, not micro-optimizations.

## Modes

This skill runs in one of two modes. Determine the mode from what is being reviewed:

- **Code Review Mode** (default) — the target is code changes (a diff, a feature folder, a set of `.cs`/`.razor` files). Apply the checklist below and report concrete issues at `file:line`.
- **Consulting Mode** — the target is a *plan* or design document. No code exists yet. Pressure-test the plan for work that lands on a **render path or startup path**, and propose caching/structure choices before code is written.

When invoked as part of an aggregated review, the orchestrator states which mode to run. Otherwise infer it: a plan/markdown design artifact → Consulting Mode; code → Code Review Mode.

## Hot Paths in This Project

- **Render path**: anything executed during `BuildRenderTree` — Razor markup expressions, computed properties evaluated in markup, `ShouldRender`, and anything re-run every `StateHasChanged`. Interactive surfaces like the Skills tab filtering/sorting and derived-stat readouts (Damage Bonus/Build, HP, MP) re-render on every user action.
- **Storage path**: `ICharacterStore` reads (IndexedDB/localStorage via JS interop) — each is async and relatively expensive; unnecessary repeated reads for unchanging data add latency.
- **Startup path**: WASM download size and initial data load from `Data/` (`DefaultSkills.cs`, `Occupations.cs`).

## Checklist

### 1. Work in the Render Path

**Violations to flag:**
- Expensive computation (filtering/sorting the skill list, recomputing derived stats across all characteristics) invoked directly from Razor markup — it re-runs on *every* render, not just when inputs change
- LINQ chains in markup expressions re-evaluated per render (`@foreach (var s in Skills.Where(...).OrderBy(...))`) — compute once into a field when state changes
- Method-group or lambda re-computation of values that only change on specific events (e.g. recalculating Damage Bonus/Build or HP every render instead of on characteristic change) — recompute on the event, cache in a field
- `async` work kicked off in a render-path property getter
- Building new collections/objects in property getters called from markup

**How to detect:**
- Read every markup expression and computed property in `.razor` files: does it allocate or iterate? Could it run once on state change instead?

### 2. Re-render Churn

**Violations to flag:**
- `StateHasChanged()` called in a loop or per-item during a batch update (batch, then render once)
- Large lists rendered without `@key`, causing full-list diffs on reorder/insert (e.g. the Skills tab skill list)
- Very large lists rendered eagerly where `Virtualize` fits
- Child components receiving new instances of unchanged data each parent render (new lambda/collection allocated per render forces child re-render and defeats parameter equality)
- Timer/event-driven `StateHasChanged` at high frequency without need
- Cascading values changing frequently and re-rendering entire subtrees

### 3. Storage and Data-Access Patterns

**Violations to flag:**
- Repeated `ICharacterStore` reads for the same investigator data within a single user interaction — read once, hold in a service or component field for the lifetime of the view
- Reloading static rules data (`DefaultSkills.cs`, `Occupations.cs`) on every navigation — these are immutable at runtime; load once at startup or on first use and cache in a scoped/singleton service
- Sequential `await`s of independent storage reads (use `Task.WhenAll` where the WASM threading model permits — note caveat below)
- Reading far more persisted data than the current view needs (e.g. loading full investigator JSON when only the roster entry is needed)
- Polling `ICharacterStore` where data can only change through this client's own actions

### 4. Async Correctness

**Violations to flag:**
- Sync-over-async: `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`
- `async void` outside event handlers
- Fire-and-forget tasks doing work whose failure matters
- `Task.Run` in WASM — the runtime is single-threaded; `Task.Run` does not parallelize work, it just adds scheduler overhead. Prefer direct `await` of async operations instead.
- Missing cancellation in long-running operations where the component can be disposed before they complete (guard callbacks after `await` if they touch state)

### 5. Allocation and Collection Choice (proportionate)

**Violations to flag:**
- `List<T>.Contains` in a loop over large data where a `HashSet<T>`/`Dictionary<K,V>` lookup fits (e.g. skill-name lookups, occupation skill-set matching)
- Rebuilding large intermediate collections repeatedly in one operation (`.ToList()` between every LINQ stage)
- String concatenation in loops (use `StringBuilder` or `string.Join`)
- Repeated re-parsing of the same static data (parse once at startup, hold typed objects)

**Not worth flagging here:** small allocations in event handlers, LINQ over small collections off the render path, anything that runs once.

### 6. Startup and Payload

**Violations to flag:**
- Heavy NuGet packages pulled into the `CthulhuSheets` project for marginal use (every dependency ships to the browser as WASM)
- Large assets served unoptimized from `wwwroot`
- Static rules data (`DefaultSkills.cs`, `Occupations.cs`) parsed eagerly on every startup when a feature only needs a subset up front — fine at current scope, flag if data grows large
- Work in `Program.cs`/root component initialization that could be deferred past first render

## Decision Framework

| Observation | Action |
|---|---|
| Skill-list filtering or derived-stat computation in a markup expression | Critical — compute on state change, render the cached result |
| Sync-over-async (`.Result`/`.Wait()`) | Critical — make the path async |
| Repeated `ICharacterStore` reads for unchanging data per interaction | Major — cache in a scoped service or component field |
| Static rules data reloaded on every navigation | Major — cache in a scoped/singleton service |
| Large list without `@key` / no virtualization | Warning — add `@key`; consider `Virtualize` |
| Sequential independent awaits | Warning — `Task.WhenAll` (mind WASM threading) |
| Wrong collection for large-data lookup | Warning — switch structure |
| `Task.Run` in WASM | Warning — remove; use direct async/await |
| Small allocation off the render path | Not a finding — skip |

**When optimization is acceptable to skip:**
- Code runs once (startup, one-shot events) and its cost is invisible
- Collections are provably small (current data scope: dozens of skills/occupations, not thousands)
- The fix adds real complexity for imperceptible gain — note as a watch item instead

## Procedure

1. Identify render-path code: markup expressions, computed properties used in markup, lifecycle methods that run per-render (`OnParametersSet`, `ShouldRender`)
2. Check each for computation/allocation that could move to event-time with a cached field
3. Trace `StateHasChanged` usage and list rendering (`@key`, `Virtualize`)
4. Map `ICharacterStore` usage per user flow: count reads, check for repeated reads of unchanging data, sequential awaits
5. Audit async patterns: sync-over-async, `async void`, fire-and-forget, `Task.Run`, missing disposal guards
6. Check collection choices against realistic data sizes
7. Categorize: **Critical** (render-path recomputation, sync-over-async), **Warning** (repeated storage reads, churn, structure choice), **Suggestion** (caching opportunities, deferrals)
8. For each finding, specify the exact cost and the concrete fix

## Consulting Mode (Plan Review)

Identify proposed work that will land on a render or startup path and design the cost out up front. In this app, assume interactive features (Skills tab filtering/sorting, character-creation steps, derived-stat displays) re-render on every user action — score/filter computations belong in event handlers with cached results, not markup. Name the proposed step or system from the plan.

**Interrogate the plan for:**
- **Render-path work**: Does the plan put filtering/sorting/derived-stat computation where it runs per render? Specify event-time computation + cached field in the plan.
- **Data flow**: Does the plan read from `ICharacterStore` repeatedly, per-component, or per-navigation? Specify a single load + shared client-side cache.
- **Storage design**: Do planned reads imply loading more persisted data than the view needs?
- **Structure choice**: For lookups the plan describes (skill name → skill object, occupation → skill list), does it pick an indexed structure?
- **Client payload**: Does the plan add project dependencies that ship to the browser? Is each justified?

**For each gap, propose a concrete remediation to the plan** — but only where a real hot path exists; don't impose ceremony on cold paths.

### Consulting Output Format

```
## Performance Plan Review: [Plan/Feature Name]

### Gaps (must address before implementation)
- **[Category]**: [Proposed work that lands on a hot path]
  - Risk: [Render churn / storage cost / startup cost if built as planned]
  - Remediation: [Cache / batch / structure / deferral to specify in the plan]

### Concerns (should address)
- **[Category]**: [Description]
  - Remediation: [Plan change]

### Recommendations (consider)
- **[Category]**: [Description]
  - Note: [Low-priority optimization worth noting]

### Summary
- Gaps: N | Concerns: N | Recommendations: N
- Design readiness: [Ready / Needs Revision / Major Gaps]
- Hot paths introduced: [none / list them]
```

## Output Format

```
## Performance Review: [Feature/File]

### Critical Issues
- **[Category]** `[member]` in [file:line]
  - Problem: [Concrete description of the cost]
  - Fix: [Specific code change]

### Warnings
- **[Category]** `[member]` in [file:line]
  - Problem: [Description]
  - Fix: [Approach]

### Suggestions
- **[Category]** [file:line]
  - Note: [Opportunity and why it is low priority]

### Summary
- Critical: N | Warnings: N | Suggestions: N
- Key risk areas: [top 1-3 hot paths that need attention]
```
