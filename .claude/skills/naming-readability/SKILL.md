---
name: naming-readability
description: "Review C#/.NET and Blazor code for naming quality and readability in the CthulhuSheets investigator-sheet app. Use when: auditing naming conventions, checking method length, reviewing code clarity, assessing cognitive complexity, checking file organization, naming review."
argument-hint: "Path to file, feature folder, or plan to review"
---

# Naming & Readability Specialist

Review C# and Blazor code for clear, intention-revealing names; appropriate method, class, and component size; low cognitive complexity; and consistent organization within files. Unreadable code is a maintenance liability regardless of whether it is correct.

## Modes

This skill runs in one of two modes. Determine the mode from what is being reviewed:

- **Code Review Mode** (default) — the target is code changes (a diff, a feature folder, a set of `.cs`/`.razor` files). Apply the checklist below to the actual code and report concrete issues at `file:line`.
- **Consulting Mode** — the target is a *plan* or design document. No code exists yet. Pressure-test the plan for **naming, method size, and readability**: find proposed names that mislead or break convention and steps that imply oversized, hard-to-read methods, and propose better names and decomposition before code is written.

When invoked as part of an aggregated review, the orchestrator states which mode to run. Otherwise infer it: a plan/markdown design artifact → Consulting Mode; code → Code Review Mode.

## Checklist

### 1. Naming Clarity

**Violations to flag:**
- Single-letter variable names outside loop counters or lambda conventions (`x => x.Name` is fine for one-liners)
- Abbreviations that are not universally understood (`cfg`, `mgr`, `svc`, `cb`)
- Names describing implementation rather than intent (`list2`, `tempData`, `helperMethod`, `doStuff`)
- Booleans that don't read as a yes/no question (`IsActive` ✅ vs `Active` ❌ vs `Status` ❌)
- Methods that lie — name says `Get` but the method mutates state; name says `Is` but has side effects
- Async methods without the `Async` suffix (project convention: `LoadInvestigatorAsync`)
- Event/callback names that don't describe the occurrence (`OnSkillSaved` ✅ vs `SaveSkill` for a notification ❌)
- Collection names that don't indicate plurality (`skill` holding a list instead of `skills`)
- Domain terms that conflict with or diverge from the project vocabulary — see `CLAUDE.md` for the CoC 7e terms in use: Characteristic (STR/CON/SIZ/DEX/APP/INT/POW/EDU/LUCK), Skill, Occupation, Sanity, Investigator, Weapon, Damage Bonus, Build, Credit Rating. Prefer these exact terms in code; invent no competing synonyms.

**Project conventions to enforce:**
- `PascalCase` for classes, methods, properties, events, public members, and Blazor component names
- `_camelCase` with underscore prefix for private fields; `camelCase` for locals and parameters
- `I`-prefix for interfaces
- `Async` suffix for async methods
- Razor component files `PascalCase.razor`; code-behind `PascalCase.razor.cs`; scoped styles `PascalCase.razor.css`
- Route templates lowercase-kebab (`@page "/character-creation"`)

### 2. Method Length and Focus

**Violations to flag:**
- Methods longer than 40 lines — likely more than one responsibility
- Methods mixing high-level orchestration and low-level detail
- Methods whose name doesn't cover everything they do (`ApplyOccupationSkills` that also saves and navigates)
- `OnInitializedAsync` doing significant work beyond loading/wiring initial state
- Methods with more than 5 parameters — indicates a missing type
- `.razor` files where the `@code` block dwarfs the markup — move to code-behind or extract components

### 3. Cognitive Complexity

**Rule of thumb:** a method's logic should be explainable in one sentence. If it needs "and then", "but only if", "except when" more than twice, it's too complex.

**Violations to flag:**
- Understanding requires tracking more than 3 boolean conditions simultaneously
- Switch/if-else chains longer than 5 cases that are not data-driven
- Complex boolean expressions not extracted to named helpers (e.g. `if (investigator.Skills.Any(s => s.Value >= 90) && investigator.Sanity.Current < 10)` → `if (investigator.IsElderlyAndBroken)`)
- Guard-clause and nested styles mixed in the same method
- The happy path buried inside multiple `if` blocks

### 4. Code Organization Within Files

**Violations to flag:**
- Inconsistent member ordering across the codebase
- Related methods not grouped together
- `#region` used to hide complexity rather than address it
- More than one public class per file (except small records/DTOs that belong together)
- Razor markup with significant inline logic that belongs in the code-behind or a computed property

**Preferred member order:**
1. Fields, then properties (including `[Parameter]` / `[Inject]` properties first in components)
2. Lifecycle methods (`OnInitializedAsync`, `OnParametersSet`, ..., `Dispose`)
3. Public API methods
4. Private methods
5. Event handlers (grouped, named `On[Event]` or `Handle[Event]`)

### 5. Comments and Documentation

**Violations to flag:**
- Comments that describe *what* rather than *why*
- Stale comments that no longer match the code
- TODO/FIXME older than one iteration without action
- Methods complex enough to require line-by-line comments (simplify instead)

**When comments are valuable:**
- Explaining *why* a non-obvious decision was made
- Documenting domain significance (a formula from CoC 7e rules, a constraint from the static rules data in `Data/`)
- Warning about known edge cases or external dependencies

## Decision Framework

| Observation | Action |
|---|---|
| Misleading name (says Get but mutates) | Critical — rename immediately |
| Method > 70 lines | Major — extract and decompose |
| Method > 40 lines | Warning — assess single-responsibility |
| Async method without `Async` suffix | Warning — rename |
| Unclear abbreviation | Warning — expand |
| Boolean not phrased as question | Warning — rename |
| Complex inline boolean not extracted | Minor — extract to named property |
| TODO/FIXME > 2 iterations old | Minor — action or delete |

**When brevity is acceptable:**
- Short private helpers with clear call sites
- Well-known abbreviations (`DTO`, `UI`, `CoC`, `STR`, `DEX`, `HP`, `MP`)

## Procedure

1. Read all files in the target feature or file set
2. Check naming conventions for every symbol
3. Measure method lengths; flag outliers
4. For methods > 20 lines, assess single-responsibility and abstraction-level consistency
5. Check complex conditionals for extraction opportunities
6. Check file-level organization and Razor markup/logic balance
7. Review comments for staleness and necessity
8. Categorize: **Critical** (actively misleading names), **Warning** (abbreviations, length, complexity), **Suggestion** (organization, polish)
9. Provide specific rename suggestions, not just flags

## Consulting Mode (Plan Review)

Lock in clear names and right-sized units before they propagate — a misleading name chosen in the plan tends to spread. Name the proposed class, method, or step from the plan.

**Interrogate the plan for:**
- **Proposed names**: Do the class, method, component, and static-data names follow conventions and reveal intent? Do they match the CoC 7e domain vocabulary in `CLAUDE.md` rather than inventing competing terms?
- **Misleading names**: Does any proposed name promise one thing while the described behavior does another?
- **Method scope**: Do planned methods describe doing several things ("validate, apply, save, and navigate")? Propose splitting in the plan.
- **Consistency**: Do proposed names align with vocabulary already used in sibling features and the existing models (`Investigator.cs`, `Skill.cs`, `Occupation.cs`, `CharacteristicHelper.cs`)?
- **Cognitive load**: Does any step imply complex nested conditions or long switch chains that should be data-driven or extracted to named predicates?

**For each gap, propose a concrete remediation to the plan**: a specific better name, a method split, a shared term to adopt, or a named predicate/lookup.

### Consulting Output Format

```
## Naming & Readability Plan Review: [Plan/Feature Name]

### Gaps (must address before implementation)
- **[Category]**: [Proposed name or unit that misleads or breaks convention]
  - Risk: [Misunderstanding / inconsistency if built as planned]
  - Remediation: [Specific name or decomposition to adopt in the plan]

### Concerns (should address)
- **[Category]**: [Description]
  - Remediation: [Plan change]

### Recommendations (consider)
- **[Category]**: [Description]
  - Note: [Why it improves clarity]

### Summary
- Gaps: N | Concerns: N | Recommendations: N
- Design readiness: [Ready / Needs Revision / Major Gaps]
- Naming convention outlook: [Good / Needs Attention / Poor]
```

## Output Format

```
## Naming & Readability Review: [Feature/File]

### Critical Issues
- **Misleading Name**: `[currentName]` in [file:line]
  - Problem: [Why the name is wrong]
  - Suggested name: `[betterName]`

### Warnings
- **[Category]**: `[symbol]` in [file:line]
  - Problem: [Description]
  - Suggested fix: [Concrete name or approach]

### Suggestions
- **[Category]**: [file:line]
  - Note: [What and why]

### Summary
- Critical: N | Warnings: N | Suggestions: N
- Naming convention compliance: [Good / Needs Attention / Poor]
```
