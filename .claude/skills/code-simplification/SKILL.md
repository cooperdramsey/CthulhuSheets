---
name: code-simplification
description: "Review C#/.NET and Blazor code for unnecessary complexity, dead code, and over-engineering. Use when: auditing code for bloat, checking for dead code, reviewing abstraction necessity, simplifying overly complex logic, reducing nesting depth, simplification review."
argument-hint: "Path to file, feature folder, or plan to review"
---

# Code Simplification Specialist

Review C# and Blazor code for unnecessary complexity, dead code, over-engineering, and logic that can be expressed more simply. The goal is code that does exactly what is needed — nothing more, nothing less.

## Modes

This skill runs in one of two modes. Determine the mode from what is being reviewed:

- **Code Review Mode** (default) — the target is code changes (a diff, a feature folder, a set of `.cs`/`.razor` files). Apply the checklist below to the actual code and report concrete issues at `file:line`.
- **Consulting Mode** — the target is a *plan* or design document. No code exists yet. Pressure-test the plan for **complexity and over-engineering**: find places where the plan proposes more machinery than the problem needs, and propose simpler alternatives before code is written.

When invoked as part of an aggregated review, the orchestrator states which mode to run. Otherwise infer it: a plan/markdown design artifact → Consulting Mode; code → Code Review Mode.

## When to Use

- Reviewing a feature for leftover scaffolding or prototype code
- Auditing a class or component that has grown too large or complex
- After a refactor to check for stranded code
- Identifying abstractions that turned out to be unnecessary

## Checklist

### 1. Dead Code

**Violations to flag:**
- Private methods, fields, or properties that are never called or referenced
- Commented-out code blocks left in files
- Classes, interfaces, or enums that are never instantiated or used
- `using` directives with no referenced types (beyond `_Imports.razor` defaults)
- Parameters declared but never read; variables assigned then never read
- Leftover template scaffolding (e.g., `Counter.razor`, `Weather.razor`) once real features exist
- Routes/pages no longer linked from anywhere and not intended as deep links

**How to detect:**
- Trace each `private` symbol for usages within the file/class
- Check whether every class in a folder is referenced somewhere (DI registration, component usage, route)

### 2. Unnecessary Abstractions

**Violations to flag:**
- Interfaces with exactly one implementing class, created for no testability or boundary reason
- Abstract base classes where all subclasses override every method (empty base)
- Generic types parameterized over a type that is always the same concrete type
- Wrapper classes that add no logic — every call delegates to the wrapped type
- Repository/service layers over data that is a static rules constant (e.g., wrapping `Data/DefaultSkills.cs` or `Data/Occupations.cs` in an extra indirection when a direct call suffices)
- Factory methods that always construct the same type with the same arguments

**When abstractions are acceptable:**
- The interface exists to keep domain logic free of I/O (e.g., `ICharacterStore` keeping `Models/` and `Helpers/` free of IndexedDB/JS interop) — a real boundary
- There is a documented plan for a second implementor within the current feature
- The abstraction enables unit testing of domain logic (e.g., swapping `IndexedDbCharacterStore` for an in-memory store in tests)

### 3. Excessive Nesting

**Violations to flag:**
- Methods with more than 3 levels of indentation from the class body
- `if` blocks that could be inverted to an early return (guard clause pattern)
- Nested loops where the inner body could be extracted to a helper
- LINQ chains inside LINQ chains producing unreadable one-liners
- Deeply nested ternary expressions
- Deeply nested `@if`/`@foreach` blocks in Razor markup that should be extracted to child components or computed properties (e.g., complex skill-filtering logic inlined in `SkillsTab.razor` instead of in `SkillsTab.razor.cs`)

### 4. Over-Engineered Solutions

**Violations to flag:**
- Custom data structures where `List<T>` / `Dictionary<K,V>` suffice
- State machine implementations for logic with only 2–3 states
- Mediator/message-bus indirection between classes that only ever run in one fixed order
- Premature generalization — code written to handle cases that don't exist yet
- Configuration/feature flags for behavior nothing toggles
- Extra projects/assemblies split off before anything needs the seam

**How to detect:**
- Ask "what is the simplest code that would make this work?" and compare to what's there
- Look for design patterns that add ceremony but no flexibility

### 5. Redundant Logic

**Violations to flag:**
- Null checks on values that can never be null
- Boolean flag fields tracking state already derivable from other state (`_isLoaded` when `_items != null`)
- Re-computing a value in the same method that already has it in a local
- Defensive casts or type checks that cannot fail given static types
- Empty `catch` blocks that swallow exceptions without handling or logging
- Manual `StateHasChanged()` calls where Blazor already re-renders (after event handlers, after awaited work in `EventCallback` handlers)
- Re-deriving a stat (e.g., Damage Bonus, HP, MP) that `CharacteristicHelper.cs` already exposes — never duplicate derived-stat formulas

## Decision Framework

| Observation | Action |
|---|---|
| Code exists but is provably unreachable | Critical — delete it |
| Abstract layer with one implementor, no boundary/test reason | Major — flatten it |
| Nesting > 3 levels deep | Major — guard clauses or extract method |
| Abstraction exists "just in case" without a real use case | Minor — note as watch item |
| Wrapper adds zero value | Major — remove the indirection |

**When complexity is acceptable:**
- It satisfies an architectural contract (domain logic / services / UI separation)
- The "simple" solution would couple layers that must remain decoupled (e.g., a `Model` directly calling JS interop)
- The pattern is established and consistent across the codebase
- Performance requires it (document why with a comment)

## Procedure

1. Read all files in the target feature or file set
2. Identify all symbols; cross-reference usages — flag any with zero usages
3. Measure nesting depth and control-flow complexity per method
4. For each abstraction (interface, base class, generic, extra layer), count implementors/uses and evaluate necessity
5. Check conditionals and computations for redundancy given surrounding state
6. Categorize findings:
   - **Critical**: Dead code that could mislead; logic silently swallowing errors; unreachable branches from a bug
   - **Warning**: Unnecessary abstraction; excessive nesting; redundant flags
   - **Suggestion**: Minor simplification opportunities
7. Provide concrete simplification suggestions, not just flags

## Consulting Mode (Plan Review)

Catch over-engineering *before* it is built — the cheapest time to simplify. Name the proposed step, abstraction, or system from the plan.

**Interrogate the plan for:**
- **Premature abstraction**: Interfaces, base classes, or generics with only one foreseeable concrete case? A "framework" where a single class would do?
- **Unnecessary new systems**: A new service, cache, or project the current scope doesn't justify? Could an existing system absorb the work? (e.g., adding a new service when `InvestigatorService.cs` could be extended)
- **Over-built control flow**: Configurable strategies or observer indirection where a straight-line method would suffice?
- **Speculative generality**: Cases, parameters, or extensibility points no current requirement asks for ("so we can later...")? For this project, watch especially for machinery the plan doesn't need yet — rules data is static C# in `Data/` and user data lives in localStorage/IndexedDB via `ICharacterStore`.
- **Simpler alternative**: For each significant piece, ask "what is the simplest design that satisfies the stated requirements?"

**For each gap, propose a concrete remediation to the plan**: collapse an abstraction, fold a proposed system into an existing one, replace a pattern with a direct call, or defer a speculative feature until a real need appears.

### Consulting Output Format

```
## Code Simplification Plan Review: [Plan/Feature Name]

### Gaps (must address before implementation)
- **[Over-engineering type]**: [What the plan over-builds]
  - Risk: [Wasted effort / maintenance burden / complexity if built as planned]
  - Remediation: [Simpler design to adopt instead]

### Concerns (should address)
- **[Type]**: [Description]
  - Remediation: [Plan change]

### Recommendations (consider)
- **[Type]**: [Description]
  - Note: [Why the simpler path is worth it]

### Summary
- Gaps: N | Concerns: N | Recommendations: N
- Design readiness: [Ready / Needs Revision / Major Gaps]
- Estimated complexity avoided: [low / medium / high]
```

## Output Format

```
## Code Simplification Review: [Feature/File]

### Critical Issues
- **Dead Code / [Type]**: [Description]
  - Location: [file:line]
  - Action: [Delete / Inline / Flatten]

### Warnings
- **Unnecessary Abstraction / [Type]**: [Description]
  - Location: [file:line]
  - Suggested simplification: [Concrete approach]

### Suggestions
- **Redundant Logic / [Type]**: [Description]
  - Location: [file:line]
  - Note: [Why this can be simplified]

### Summary
- Critical: N | Warnings: N | Suggestions: N
- Estimated lines removed by addressing critical issues: ~N
```
