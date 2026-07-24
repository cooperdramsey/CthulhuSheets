---
name: solid-principles
description: "Review C#/.NET code for SOLID principle adherence in CthulhuSheets. Use when: auditing class responsibilities, checking for OCP violations, reviewing dependency direction across logical layers, assessing coupling, checking for LSP violations, SOLID review."
argument-hint: "Path to file, feature folder, or plan to review"
---

# SOLID Principles Specialist

Review C# code for adherence to the five SOLID principles adapted to this project's logical-layer architecture. Poor SOLID adherence leads to classes that are hard to extend, test, and reason about.

This project's architecture already mandates some SOLID compliance (the domain/logic layer in `Models/`, `Helpers/`, and `Data/` must not depend on Blazor or storage types), so flag violations that go against both SOLID *and* the project's established layering.

## Modes

This skill runs in one of two modes. Determine the mode from what is being reviewed:

- **Code Review Mode** (default) — the target is code changes (a diff, a feature folder, a set of `.cs`/`.razor` files). Apply the checklist below to the actual code and report concrete issues at `file:line`.
- **Consulting Mode** — the target is a *plan* or design document. No code exists yet. Pressure-test the plan from the perspective of **class responsibilities, coupling, and dependency direction**: find gaps and propose concrete changes to the plan before code is written.

When invoked as part of an aggregated review, the orchestrator states which mode to run. Otherwise infer it: a plan/markdown design artifact → Consulting Mode; code → Code Review Mode.

## Project Layer Rules

The dependency direction is fixed across three logical layers within the single `CthulhuSheets` assembly:

- **Domain/logic layer** (`Models/`, `Helpers/`, `Data/`) — models and pure rules logic (derived-stat computation in `CharacteristicHelper.cs`, success-level resolution in `SkillRules.cs`, sanity logic in `SanityRules.cs`, static data in `Data/Occupations.cs` and `Data/DefaultSkills.cs`). References no Blazor types (`ComponentBase`, `NavigationManager`), `IJSRuntime`, or storage. This is the DIP boundary.
- **Services layer** (`Services/`) — orchestration and persistence (`InvestigatorService.cs`, `DiceRollService.cs`); may touch storage and JS interop. Consumed by UI. Depends on domain/logic through its public types; depends on storage through `ICharacterStore`.
- **UI layer** (`Pages/`, `Shared/`, `Layout/`) — thin Blazor components. Talk to logic/services via injected services; communicate with each other via `[Parameter]`/`EventCallback`, not direct reach-ins.

Since this is one assembly, layering is not enforced by project references — these are the violations worth flagging: a Model or Helper pulling in `IJSRuntime`/Blazor types; a component embedding rules math that belongs in a Helper or the domain layer.

## Checklist

### 1. Single Responsibility Principle (SRP)

**A class should have one reason to change.**

**Violations to flag:**
- Service classes that contain domain rules AND persistence AND presentation formatting in the same class
- Blazor components that fetch data AND contain domain logic AND format output (components should render; rules logic lives in `Helpers/` or `Models/`)
- Classes whose name has "And" in it, or whose responsibilities require multiple unrelated `using` directives
- A single class handling many unrelated concerns (e.g., a helper doing derived-stat computation AND skill-point allocation AND sanity-loss resolution all in one place)

**How to detect:**
- Ask: "What is the one reason this class would need to change?" If there are two or more unrelated reasons, flag it
- Count the distinct verbs in a class's method names — unrelated clusters signal multiple responsibilities
- Look for long files (> 200 lines for a plain class, > 150 lines of `@code`/code-behind for a component) as a smell

### 2. Open/Closed Principle (OCP)

**Code should be open for extension, closed for modification.**

**Violations to flag:**
- Switch or if-else chains over an occupation name or skill type that must be edited to add a new variant (e.g., hardcoded `if (occupation == "Doctor")` scattered through creation code — adding an occupation means editing every branch)
- Hardcoded type checks (`is T`, `as T`, `typeof(T)`) in core orchestration logic
- Logic that enumerates known occupations or skills inline rather than reading from `Data/Occupations.cs` or `Data/DefaultSkills.cs` (adding a new occupation or skill should be a data edit, not a code edit)
- Copy-paste subclasses where the only difference should be a polymorphic method or a data value

**Good patterns to recognize:**
- Data-driven dispatch: behavior selected by rules data (occupation skill lists, skill-point formulas) rather than hardcoded per-item logic
- Strategy via interfaces registered in DI rather than switch chains
- Adding the next occupation or skill specialization requires only a data entry in `Data/Occupations.cs` or `Data/DefaultSkills.cs`, not code changes elsewhere

**When hardcoded type checks are acceptable:**
- Serialization/deserialization infrastructure
- One-off migration or tooling code that won't be extended

### 3. Liskov Substitution Principle (LSP)

**Subtypes must be substitutable for their base types without breaking correctness.**

**Violations to flag:**
- Subclass overrides a base method but does less than the base promises (weakens postconditions)
- Subclass throws exceptions the base contract says it won't
- Subclass requires stricter preconditions than the base
- Base class methods that all subclasses immediately override entirely — the hierarchy is wrong
- Interface methods implemented with `throw new NotImplementedException()` in production code

### 4. Interface Segregation Principle (ISP)

**Clients should not depend on interfaces they don't use.**

**Violations to flag:**
- A single interface with many methods where typical implementors only use a subset
- Classes passed as a large interface when the method only calls one or two members — accept a narrower interface
- God interfaces that mix data retrieval, mutation, and event subscription in one contract
- DI registrations where a consumer receives a broad service but needs one capability

**How to detect:**
- For each interface, count methods (> 5 may be too broad) and check each implementor provides meaningful implementations (not empty or throw)

### 5. Dependency Inversion Principle (DIP)

**High-level modules should not depend on low-level modules. Both should depend on abstractions.**

**Violations to flag:**
- Domain classes in `Models/` or `Helpers/` referencing Blazor types (`ComponentBase`, `NavigationManager`, `IJSRuntime`)
- Domain logic reading browser storage or performing JS interop directly (storage and interop should be behind `ICharacterStore` at the edges)
- A Blazor component reaching into another component's internals instead of communicating via parameters/`EventCallback`/shared service
- Lower-level utilities depending on higher-level orchestrators
- Static/singleton state used where a DI-injected dependency belongs

**Good patterns to recognize:**
- Domain logic as pure functions/classes (e.g., `CharacteristicHelper.RecomputeDerived`, `SanityRules`, `SkillRules`), dependencies passed via parameters
- Services consume the domain through its public API; UI consumes services through injection
- Side effects (storage, JS interop) live at the edges behind `ICharacterStore`; `IndexedDbCharacterStore` and `LocalStorageCharacterStore` are the concrete implementations

## Decision Framework

| Principle | Violation | Severity |
|---|---|---|
| SRP | Class has 2+ unrelated reasons to change | Major |
| SRP | Method mixes abstraction levels significantly | Minor |
| OCP | Occupation/skill switch that must be edited for new data variants | Major |
| OCP | Copy-paste subclass for minor variation | Warning |
| LSP | `NotImplementedException` in production code | Critical |
| LSP | Subclass weakens base contract | Major |
| ISP | Interface > 8 methods with partial implementors | Warning |
| DIP | Domain (`Models/`/`Helpers/`) references Blazor/JS types | Critical |
| DIP | Domain does I/O directly (storage, JS interop) | Critical |

**When SOLID is acceptable to bend:**
- Small, self-contained classes with one clear lifetime often don't need abstraction
- The logical-layer separation already enforces many DIP/SRP contracts — don't add redundant abstractions on top
- If introducing an interface would thread it through many constructors for no testability gain, the cost may outweigh the benefit

## Procedure

1. Read all files in the target feature or file set
2. Classify each class by logical layer: Domain/logic (`Models/`, `Helpers/`, `Data/`) / Services (`Services/`) / UI (`Pages/`, `Shared/`, `Layout/`)
3. **SRP pass**: identify each class's responsibilities; flag multiple unrelated change drivers
4. **OCP pass**: find occupation/skill-type switches and hardcoded data enumeration; assess whether adding a new occupation or skill requires code edits outside `Data/`
5. **LSP pass**: review inheritance hierarchies and interface implementations
6. **ISP pass**: review interfaces for size and partial implementation
7. **DIP pass**: verify dependency direction (UI → Services → Domain/logic); flag Domain referencing Blazor/JS/storage types
8. Categorize findings: **Critical** (DIP layer violations, `NotImplementedException`), **Warning** (SRP creep, OCP-closed switches), **Suggestion** (interface fat, minor SRP concerns)
9. Provide specific refactoring proposals, not just principle labels

## Consulting Mode (Plan Review)

Apply the same five principles as forward-looking questions about the proposed design. Name the proposed class, system, or step from the plan.

**Interrogate the plan for:**
- **SRP**: Does any proposed class own more than one reason to change (a service doing orchestration *and* derived-stat calculation *and* persistence)?
- **OCP**: Does adding the *next* occupation or skill specialization under this plan require editing core code, or just adding a data entry to `Data/Occupations.cs` or `Data/DefaultSkills.cs`?
- **LSP**: Does the plan propose a hierarchy or interface a subtype won't fully honor?
- **ISP**: Does the plan define a broad interface most implementors will only partially use?
- **DIP**: Does the proposed dependency direction respect UI → Services → Domain/logic? Does any planned domain class touch Blazor types, `IJSRuntime`, or storage directly? Are dependencies injected or reached for?

**For each gap, propose a concrete remediation to the plan**: a class split, data-driven dispatch instead of a switch, a narrower interface, or a corrected dependency direction.

### Consulting Output Format

```
## SOLID Principles Plan Review: [Plan/Feature Name]

### Gaps (must address before implementation)
- **[Principle]**: [What the plan is missing or will get wrong]
  - Risk: [What breaks or becomes hard to extend if built as planned]
  - Remediation: [Concrete change/addition to the plan]

### Concerns (should address)
- **[Principle]**: [Description]
  - Remediation: [Plan change]

### Recommendations (consider)
- **[Principle]**: [Description]
  - Note: [Why it would strengthen the plan]

### Summary
- Gaps: N | Concerns: N | Recommendations: N
- Design readiness: [Ready / Needs Revision / Major Gaps]
- Principle outlook: SRP [✓/✗] OCP [✓/✗] LSP [✓/✗] ISP [✓/✗] DIP [✓/✗]
```

## Output Format

```
## SOLID Principles Review: [Feature/File]

### Critical Issues
- **[Principle] Violation**: [class/method] in [file:line]
  - Problem: [Concrete description of the violation]
  - Fix: [Specific refactoring approach]

### Warnings
- **[Principle] Violation**: [class/method] in [file:line]
  - Problem: [Description]
  - Fix: [Approach]

### Suggestions
- **[Principle] Concern**: [file:line]
  - Note: [What and why it could become a problem]

### Summary
- Critical: N | Warnings: N | Suggestions: N
- Principle health: SRP [✓/✗] OCP [✓/✗] LSP [✓/✗] ISP [✓/✗] DIP [✓/✗]
```
