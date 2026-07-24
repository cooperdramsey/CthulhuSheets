---
name: dry-principles
description: "Review C#/.NET and Blazor code for DRY principle violations and code duplication. Use when: checking for duplicated code, auditing abstraction quality, reducing copy-paste, refactoring shared logic, improving code reuse, DRY review."
argument-hint: "Path to file, feature folder, or plan to review"
---

# DRY Principles Specialist

Review C# and Blazor code for Don't Repeat Yourself violations: duplicated logic, copy-paste code, missed abstractions, and opportunities for shared utilities.

## Modes

This skill runs in one of two modes. Determine the mode from what is being reviewed:

- **Code Review Mode** (default) — the target is code changes (a diff, a feature folder, a set of `.cs`/`.razor` files). Apply the checklist below to the actual code and report concrete issues at `file:line`.
- **Consulting Mode** — the target is a *plan* or design document. No code exists yet. Pressure-test the plan for **duplication and reuse**: find places where the plan would reinvent or copy logic that already exists, or set up parallel structures that should share a foundation, and propose consolidation before code is written.

When invoked as part of an aggregated review, the orchestrator states which mode to run. Otherwise infer it: a plan/markdown design artifact → Consulting Mode; code → Code Review Mode.

## When to Use

- Reviewing code for duplication across a feature or system
- Auditing whether common patterns have been properly abstracted
- After adding a new feature to check if it duplicates existing code
- Refactoring pass to consolidate repeated logic

## Checklist

### 1. Literal Code Duplication

**Violations to flag:**
- Identical or near-identical method bodies in multiple classes
- Copy-pasted blocks with only variable names changed
- Repeated serialization/deserialization or mapping patterns not extracted to helpers
- Same LINQ query or collection operation written in multiple places
- Duplicate validation logic (null checks, range checks, state guards)

**How to detect:**
- Look for methods with the same structure but different names
- Compare sibling classes and components (e.g., two sheet-tab pages) for duplicated bodies

### 2. Structural Duplication

**Violations to flag:**
- Multiple classes implementing the same interface/pattern without a shared base where they share significant behavior
- Parallel Blazor components repeating the same markup + `@code` shape without a shared component, base class, or `RenderFragment` (e.g., two sheet tabs with identical loading/empty-state markup)
- Repeated event subscription/teardown or `IDisposable` boilerplate that could live in a base component
- Multiple models with overlapping fields that should share a base type
- Repeated storage-read-and-handle-error patterns in components that should be a shared service method on `InvestigatorService`

**Consider:**
- Base class opportunity (N≥2 classes share significant behavior)
- Utility/extension method opportunity (shared code is small and stateless)
- Composition opportunity (inheritance would be forced/awkward)

### 3. Data Duplication

**Violations to flag:**
- Same constant or magic number defined in multiple files
- Configuration values scattered across classes instead of centralized (a constants class or static rules data in `Data/`)
- Identical string literals (route paths, storage keys, CSS class names) repeated without constants
- Rules vocabulary (skill names, characteristic codes, occupation IDs) duplicated between `Data/DefaultSkills.cs` or `Data/Occupations.cs` and code instead of defined once and referenced
- Enum values or state definitions duplicated between layers

**Fixes:**
- Extract to a shared constants class or the static rules data in `Data/`
- Use `const`, `static readonly`, or `WellKnownSkills.cs` references
- Route templates and storage keys defined once and referenced

### 4. Pattern Duplication

**Violations to flag:**
- Same try/catch or error handling pattern repeated instead of a helper
- Repeated async patterns (load-set-loading-flag-render) that should be a shared helper or component
- Same validate-then-map-then-return shape duplicated across multiple methods or components
- Identical skill-filtering or characteristic-lookup patterns not extracted (e.g., the same LINQ filter appearing in both `SkillsTab.razor.cs` and `CreationOccupationSkillsStep.razor.cs`)
- Derived-stat formulas (HP, MP, Damage Bonus/Build, half/fifth values) recalculated inline instead of delegating to `CharacteristicHelper.cs` or `SkillRules.cs`

### 5. Test / Validation Duplication

**Violations to flag:**
- Same guard clause pattern at the top of many methods (extract to a validator)
- Repeated null-check-and-early-return chains
- Same state validation checked identically in multiple places
- Test setup/fixture code copy-pasted across test classes instead of shared builders

## Decision Framework

Before flagging something as a DRY violation, apply this filter:

| Duplication Count | Action |
|-------------------|--------|
| 2 instances | Flag as potential — may be fine if logic could diverge |
| 3+ instances | Flag as definite — extract to shared code |
| Different layers (domain logic / services / UI) | Be cautious — duplication across layers may be intentional separation |

**When duplication is acceptable:**
- Test code (clarity over DRY)
- Cross-layer boundaries (domain models should not absorb UI or storage concerns just to deduplicate)
- Premature abstraction would make code harder to understand
- Two things that look the same today but have different change reasons

## Procedure

1. Read all files in the target feature or file set
2. Build an inventory of repeated patterns, methods, and logic blocks
3. For each duplication found: identify all locations, apply the decision framework, propose a specific refactoring (base class, utility, constants, shared component, composition)
4. Categorize findings:
   - **Critical**: 3+ copies of identical logic, scattered magic values affecting behavior, derived-stat formulas duplicated outside `CharacteristicHelper.cs`
   - **Warning**: 2 copies of similar logic, repeated patterns that may diverge
   - **Suggestion**: Minor style duplication, potential future consolidation
5. Provide concrete refactoring suggestions with code examples

## Consulting Mode (Plan Review)

Catch duplication before it is written — and spot where the plan reinvents something the codebase already has. Check the plan *against the existing systems* (consult `CLAUDE.md` and the relevant code), not just against itself. Name the proposed step or system from the plan.

**Interrogate the plan for:**
- **Reinvention**: Does the plan propose building something the project already provides (a bespoke derived-stat formula instead of delegating to `CharacteristicHelper.cs`, a new skill lookup instead of `WellKnownSkills.cs`, a second storage path instead of `ICharacterStore`)?
- **Parallel structures**: Does the plan introduce sibling components/services/models that will duplicate the shape of existing ones without a shared base or helper?
- **Repeated logic across steps**: Do multiple steps describe the same validation, mapping, or setup pattern that should be defined once?
- **Scattered data/constants**: Does the plan introduce the same magic values, string keys, or rules constants in more than one place?
- **Cross-layer caution**: Where the plan repeats a shape across domain logic/services/UI, confirm whether that is intentional separation (acceptable) or true duplication (consolidate).

**For each gap, propose a concrete remediation to the plan**: reuse the existing system, introduce a shared base/utility up front, centralize a constant or config, or define a single helper the plan's steps call.

### Consulting Output Format

```
## DRY Principles Plan Review: [Plan/Feature Name]

### Gaps (must address before implementation)
- **[Duplication/Reinvention type]**: [What the plan would duplicate or reinvent]
  - Existing equivalent: [System/class that already covers this, if any]
  - Remediation: [Reuse / shared base / centralization to adopt in the plan]

### Concerns (should address)
- **[Type]**: [Description]
  - Remediation: [Plan change]

### Recommendations (consider)
- **[Type]**: [Description]
  - Note: [Why consolidating up front pays off]

### Summary
- Gaps: N | Concerns: N | Recommendations: N
- Design readiness: [Ready / Needs Revision / Major Gaps]
- Reuse opportunities identified: N
```

## Output Format

```
## DRY Principles Review: [Feature/File]

### Critical Duplications
- **Pattern**: [Description of duplicated code]
  - Found in: [file1:line], [file2:line], ...
  - Suggested refactoring: [Concrete approach with code example]

### Warnings
- **Pattern**: [Description]
  - Found in: [locations]
  - Suggested refactoring: [Approach]

### Suggestions
- **Pattern**: [Description]
  - Found in: [locations]
  - Note: [Why this might be acceptable or worth watching]

### Summary
- Critical: N | Warnings: N | Suggestions: N
- Estimated lines saved by refactoring critical issues: ~N
```
