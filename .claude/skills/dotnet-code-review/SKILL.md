---
name: dotnet-code-review
description: "Comprehensive .NET/Blazor code review aggregating all specialist checks. Use when: full code review, feature review, PR review, comprehensive quality audit, reviewing a complete feature or system, or pressure-testing an implementation plan before coding."
argument-hint: "Path to feature folder, list of files, or plan to review"
---

# .NET Code Review — Aggregated Specialist Review

Run a comprehensive review by applying all specialist checklists against a target, then merge their findings into one unified report. This orchestrator coordinates eight specialists covering Blazor component quality, DRY, simplification, performance, naming, error handling, SOLID, and interface hygiene.

## Modes

This review runs in one of two modes. Determine the mode from what is being reviewed, then run **every** specialist in that same mode:

- **Code Review Mode** (default) — the target is code changes (a feature folder, a diff, a set of `.cs`/`.razor` files). Each specialist applies its checklist to the actual code and reports issues at `file:line`. The aggregated report drives **code remediations**.
- **Consulting Mode** — the target is a *plan* or design document. No code exists yet. Each specialist runs in *its* Consulting Mode, pressure-testing the plan to surface gaps and proposing additions/changes. The aggregated report drives **plan remediations** — fixes applied to the plan before any code is written.

Infer the mode if not told: a plan/markdown design artifact → Consulting Mode; code or a diff → Code Review Mode. All specialists run in the same mode so findings aggregate consistently.

**Scale depth to the target.** Every specialist pass always runs, but for a small diff keep each pass focused on the changed files and their immediate collaborators. The full inventory and exhaustive cross-referencing are for feature-sized or system-sized targets.

## When to Use

**Code Review Mode:** reviewing a complete feature before merging; comprehensive quality audit; PR-level review; post-implementation quality gate.

**Consulting Mode:** reviewing an implementation plan before starting work; validating a design proposal against project standards; pre-implementation design gate (used by `plan-with-review` as a plan-review pass).

## Specialist Areas

| Specialist | Skill | Focus |
|-----------|-------|-------|
| **Blazor Component Quality** | `blazor-component-quality` | Thin components, lifecycle hygiene, parameters/callbacks, subscriptions/disposal, state ownership |
| **DRY Principles** | `dry-principles` | Code duplication, missed abstractions, data duplication, pattern repetition |
| **Code Simplification** | `code-simplification` | Dead code, over-engineering, unnecessary abstractions, excessive nesting |
| **Performance Awareness** | `performance-awareness` | Render-path work, re-render churn, async correctness, payload |
| **Naming & Readability** | `naming-readability` | Naming clarity, method length, cognitive complexity, conventions |
| **Error Handling** | `error-handling` | Boundary safety, silent failures, validation, graceful degradation |
| **SOLID Principles** | `solid-principles` | SRP, OCP, LSP, ISP, DIP applied to the layered .NET context |
| **Interface Hygiene** | `interface-hygiene` | Public API surfaces, access modifiers, minimal exposure, layer contracts |

Each specialist's full checklist lives in its own skill — apply those checklists; do not re-derive them here.

## Code Review Mode Procedure

### Phase 1: Scope Discovery

1. Identify all files in the target feature or path
2. Classify each file by logical layer:
   - **Domain/logic** (`Models/`, `Helpers/`, `Data/`)
   - **Services** (`Services/`, `Services/Storage/`)
   - **UI** (`Pages/`, `Shared/`, `Layout/`)
   - **Tests** (`CthulhuSheets.Tests`)
3. List all classes/components, their base types, and relationships

### Phases 2–9: Specialist Passes

Run each of the eight specialists' full checklists in Code Review Mode, in this order: `blazor-component-quality` (UI targets), `dry-principles`, `code-simplification`, `performance-awareness`, `naming-readability`, `error-handling`, `solid-principles`, `interface-hygiene`. Skip `blazor-component-quality` only if the target contains no UI code.

### Phase 10: Cross-Cutting Concerns

After all specialist passes, check for issues that span areas:

1. **Layer discipline** — does UI code embed rules math that belongs in `Helpers`/`Models`? Does a pure Model/Helper pull in Blazor/`IJSRuntime`/storage types?
2. **Consistency** — are similar features (creation steps, sheet tabs) implemented with consistent patterns?
3. **Rules fidelity** — for rules-bearing domain logic, does it match `references/rules_condensed/`? Defer to the **`rules-review`** skill for a deep rules audit; here just flag anything that looks rules-wrong. Note: a wrong value in `Data/Occupations.cs` or `Data/DefaultSkills.cs` silently corrupts every character.
4. **Persistence compatibility** — if the change touches a persisted model (anything reachable from `Investigator`/`Roster`), do previously saved characters still deserialize via `ICharacterStore`? Watch for renamed/retyped properties, non-nullable additions without defaults, and removed enum values.
5. **Tests** — new domain logic (`Models`/`Helpers`/`Services`) should come with unit tests; that's the point of keeping logic out of components.
6. **Styling tokens** — `.razor.css` changes use `--space-*`/`--radius-*`/`--icon-*` design tokens and MudBlazor palette colors (per `CLAUDE.md`), not raw px.
7. **Living documentation** — if a system was added, significantly modified, or removed, do `CLAUDE.md`'s architecture notes still hold?

### Phase 11: Compile Report

Merge all findings into a single unified report, deduplicating overlapping issues (report each issue once under the most specific specialist).

## Code Review Mode Output Format

```
# Code Review: [Feature Name]

## File Inventory
| File | Layer | Type | Lines |
|------|-------|------|-------|
| X.cs | Domain/logic | class | 120 |
| Y.razor | UI | component | 85 |

## Critical Issues (must fix)
1. **[Specialist Area]** [file:line] — Description
   → Fix: Concrete suggestion

## Warnings (should fix)
1. **[Specialist Area]** [file:line] — Description
   → Fix: Concrete suggestion

## Suggestions (consider)
1. **[Specialist Area]** [file:line] — Description
   → Note: Why and what to consider

## Cross-Cutting Observations
- [Architectural or consistency notes]

## Summary
| Specialist | Critical | Warnings | Suggestions |
|-----------|----------|----------|-------------|
| Blazor Component Quality | N | N | N |
| DRY Principles | N | N | N |
| Code Simplification | N | N | N |
| Performance Awareness | N | N | N |
| Naming & Readability | N | N | N |
| Error Handling | N | N | N |
| SOLID Principles | N | N | N |
| Interface Hygiene | N | N | N |
| Cross-Cutting | N | N | N |
| **Total** | **N** | **N** | **N** |

## Verdict
[PASS / PASS WITH WARNINGS / NEEDS WORK]
Brief overall assessment and top priority items.
```

## Consulting Mode (Plan Review) Procedure

### Phase 1: Plan Intake

1. Read the full plan. Identify what it proposes: new components, services, static rules-data changes, storage/persistence changes, and how they connect.
2. Map proposed elements to layers: Domain/logic / Services / UI / Tests.
3. Gather context to judge the plan against the existing project — consult `CLAUDE.md` and the relevant existing code, so reuse and consistency checks are grounded in reality.
4. Note anything the plan leaves unspecified — silence is often where the gaps are.

### Phase 2: Specialist Consulting Passes

Run each of the eight specialists in its Consulting Mode against the plan:

1. **Blazor Component Quality** — logic planned into components, undefined communication/state ownership, missing disposal
2. **DRY Principles** — reinvention of existing systems, parallel structures, scattered data
3. **Code Simplification** — premature abstraction, unnecessary new systems, speculative generality
4. **Performance Awareness** — render-path computation, re-render churn, async correctness, payload growth
5. **Naming & Readability** — misleading or non-conventional proposed names, oversized planned methods, domain-vocabulary conflicts
6. **Error Handling** — unhandled failure cases, unvalidated boundaries, happy-path-only design, persistence migration
7. **SOLID Principles** — responsibility creep, OCP-violating switches, wrong dependency direction
8. **Interface Hygiene** — over-broad proposed surfaces, mutable state across boundaries, broad dependencies

Each specialist reports **Gaps** (must address), **Concerns** (should address), and **Recommendations** (consider).

### Phase 3: Cross-Cutting Plan Concerns

1. **Layer integrity**: Does the proposed design keep rules math in `Models`/`Helpers`, services in `Services`, and UI in `Pages`/`Shared`/`Layout`?
2. **Completeness**: Failure paths covered, not just happy path? Loading/empty/error states for new UI? Tests planned for new domain logic?
3. **Consistency**: Does the plan follow established patterns, or introduce a competing approach for an existing concept?
4. **Rules fidelity**: If the plan touches rules-bearing logic or data, does it align with `references/rules_condensed/`? Consider invoking `rules-review` for a deep audit.
5. **Persistence safety**: If the plan changes persisted models (anything reachable from `Investigator`/`Roster`), does it account for existing saved characters deserializing correctly?

### Phase 4: Aggregate and Propose Plan Remediations

Merge all findings, deduplicate, then translate the gaps into a concrete, ordered set of **plan remediations** — specific edits to make to the plan before implementation begins.

## Consulting Mode Output Format

```
# Plan Review: [Plan/Feature Name]

## Proposed Scope
| Element | Layer | Type | Notes |
|---------|-------|------|-------|
| X       | Domain/logic | service | new |
| Y.razor | UI | component | new |

## Gaps (must address before implementation)
1. **[Specialist Area]** [proposed element] — Description
   → Remediation: Concrete change to make to the plan

## Concerns (should address)
1. **[Specialist Area]** [proposed element] — Description
   → Remediation: Plan change

## Recommendations (consider)
1. **[Specialist Area]** [proposed element] — Description
   → Note: Why it would strengthen the plan

## Cross-Cutting Observations
- [Layer integrity, completeness, consistency notes]

## Summary
| Specialist | Gaps | Concerns | Recommendations |
|-----------|------|----------|-----------------|
| Blazor Component Quality | N | N | N |
| DRY Principles | N | N | N |
| Code Simplification | N | N | N |
| Performance Awareness | N | N | N |
| Naming & Readability | N | N | N |
| Error Handling | N | N | N |
| SOLID Principles | N | N | N |
| Interface Hygiene | N | N | N |
| Cross-Cutting | N | N | N |
| **Total** | **N** | **N** | **N** |

## Proposed Plan Remediations
1. [What to change in the plan and why]
2. [Remediation]

## Verdict
[READY / READY WITH REVISIONS / NEEDS REWORK]
Brief overall assessment and the top priority gaps to close before coding.
```
