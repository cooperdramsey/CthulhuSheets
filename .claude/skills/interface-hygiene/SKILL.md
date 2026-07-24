---
name: interface-hygiene
description: "Review C#/.NET and Blazor code for clean public API surfaces and layer boundaries in CthulhuSheets. Use when: auditing public APIs, checking access modifiers, reviewing system contracts, checking for boundary leaks, verifying encapsulation, interface hygiene review."
argument-hint: "Path to file, feature folder, or plan to review"
---

# Interface Hygiene Specialist

Review C# and Blazor code for clean, intentional public API surfaces. Every class communicates a contract through its public interface — what it exposes, what it hides, and how other code is expected to interact with it. Poor interface hygiene leads to accidental coupling, hidden dependencies, and APIs that are hard to use correctly.

"Interface" here means *the public surface of a class* (its public members, not just C# `interface` types), as well as the contracts between logical layers (Domain/logic ↔ Services ↔ UI) and between Blazor components.

## Modes

This skill runs in one of two modes. Determine the mode from what is being reviewed:

- **Code Review Mode** (default) — the target is code changes (a diff, a feature folder, a set of `.cs`/`.razor` files). Apply the checklist below and report concrete issues at `file:line`.
- **Consulting Mode** — the target is a *plan* or design document. No code exists yet. Pressure-test the plan for **public API surfaces and layer boundaries**: find where the plan would expose too much or leak internals across layers, and propose minimal, intentional contracts before code is written.

When invoked as part of an aggregated review, the orchestrator states which mode to run. Otherwise infer it: a plan/markdown design artifact → Consulting Mode; code → Code Review Mode.

## Checklist

### 1. Access Modifier Discipline

**Violations to flag:**
- Fields that are `public` (use properties; fields expose storage directly)
- Methods `public` because it was convenient, not because they're part of the contract
- `internal` or `protected` used where `private` suffices
- Component members `public` when Blazor only needs them accessible to the component itself (`@code` members and handlers can be private)
- `static` mutable state shared between consumers where a DI-scoped service belongs

**How to detect:**
- Every `public` field is a violation unless it's a `const`/`static readonly` constant on a constants type
- Check every `public` method: is it called from outside the class? If not, make it `private`

### 2. Minimal Public Surface

**Violations to flag:**
- Classes exposing more methods than consumers actually call
- Helpers made `public` "in case they're useful later" with no current caller
- Read-write properties where callers only read (`{ get; private set; }` or `init`)
- Collections exposed as `List<T>` or `T[]` where `IReadOnlyList<T>` / `IEnumerable<T>` conveys read-only intent
- Domain results exposing mutable internals the UI could corrupt
- Persisted models (`Investigator`, `Roster`) with setters nothing legitimately sets after construction — prefer `init` where JSON round-trip does not require open setters (System.Text.Json requires public setters or `init` for deserialization, which is the acceptable-exception case)

### 3. Clean Layer and Component Boundaries

**In this project, the boundaries are:**
- **Domain/logic layer** (`Models/`, `Helpers/`, `Data/`) → exposes models and pure rules logic; consumed by Services and UI; references neither Blazor types nor storage
- **Services layer** (`Services/`) → exposes a narrow surface (`InvestigatorService`, `DiceRollService`, `ICharacterStore`); concrete storage implementations (`IndexedDbCharacterStore`, `LocalStorageCharacterStore`) are the edge and are not exposed to UI directly
- **UI layer** (`Pages/`, `Shared/`, `Layout/`) → components communicate via `[Parameter]` / `EventCallback` downward-in, events-out; siblings talk through a shared service or common parent, not direct references

**Violations to flag:**
- Domain returning mutable collections that UI components then modify in place as their state-management strategy
- Components mutating objects received as `[Parameter]` (parameters flow down; changes flow up via `EventCallback`)
- A component reaching into another component instance's members instead of using parameters/callbacks/shared state
- UI code depending on concrete storage types (`IndexedDbCharacterStore`, `LocalStorageCharacterStore`) directly rather than going through `InvestigatorService` or `ICharacterStore`

### 4. Consistent Contracts

**Violations to flag:**
- Two methods that do the same thing with slightly different names (`Apply` vs `Execute`, `Remove` vs `Delete`)
- Sibling classes with different failure contracts (one returns null, one empty, one throws)
- Public methods that mutate state and return a value — unclear whether to use the value or re-check state
- Mixed naming styles for callbacks/events in the same feature (`OnSaved`, `SavedEvent`, `NotifySaved`)

### 5. Encapsulation of Implementation Details

**Violations to flag:**
- Implementation types leaked in public signatures: `public Dictionary<string, Skill> SkillMap` when callers only iterate
- Internal coordination types exposed publicly when used inside one system
- Raw indexes passed across boundaries instead of stable IDs (`Investigator.Id` is a `Guid`; raw list indexes break on reorder)
- Lifecycle flags (`IsInitialized`, `IsLoaded`) exposed publicly — callers shouldn't manage another object's lifecycle
- Storage/serialization shapes used directly as UI models when they've started to diverge

## Decision Framework

| Observation | Action |
|---|---|
| `public` mutable field | Critical — property or private |
| Component mutating its `[Parameter]` object | Critical — copy-in or EventCallback up |
| Mutable collection exposed as `public List<T>` | Major — `IReadOnlyList<T>` or expose methods |
| `public` method with no external callers | Warning — make private |
| Read-write property never set externally | Warning — private set / init |
| Inconsistent sibling contracts | Warning — align them |
| Helper public "just in case" | Suggestion — private until needed |

**When wider visibility is acceptable:**
- Serialization requires public setters (System.Text.Json needs them or `init` for `Investigator`, `Roster`, and related models)
- Data-only records with no invariants to protect
- A boundary adapter whose entire purpose is bridging layers

## Procedure

1. Read all files in the target feature or file set
2. For each `public` member, tag it: justified (external callers), questionable (none), or wrong (violates boundary rules)
3. Check all `public` fields and property setter visibility
4. Check exposed types: minimal and appropriate? Read-only where consumers only read?
5. Trace layer rules (UI → Services → Domain/logic) and component communication patterns; flag inversions and parameter mutation
6. Look for inconsistent contracts across sibling classes
7. Categorize: **Critical** (boundary violations, mutable state across layers, public fields), **Warning** (broad surfaces, setter visibility, inconsistency), **Suggestion** (minor tightening)
8. Provide the exact visibility/type change for each finding

## Consulting Mode (Plan Review)

Shape clean contracts before they harden — it is far cheaper to define a minimal surface on paper than to retract an over-exposed one later. Name the proposed class, service, or contract from the plan.

**Interrogate the plan for:**
- **Boundary contracts**: For each new service/component, what exactly does it expose to consumers? Is the contract narrow and explicit?
- **Mutable state exposure**: Does the plan hand out collections or state that callers could mutate? Plan read-only exposure where consumers only read.
- **Over-broad dependencies**: Does the plan pass whole services/objects where a consumer needs one piece? Propose a narrower dependency.
- **Component communication**: Do planned components communicate via parameters/EventCallback/shared service, or does the plan imply direct reach-ins or parameter mutation?
- **Contract consistency**: Will new siblings match the established shape (naming, nullability, failure behavior)?

**For each gap, propose a concrete remediation to the plan**: define the public contract explicitly, switch an exposed collection to read-only, narrow a dependency, or mark members private-by-default.

### Consulting Output Format

```
## Interface Hygiene Plan Review: [Plan/Feature Name]

### Proposed Boundary Contracts
| New System/Class | Exposes To | Proposed Surface | Concern |
|------------------|-----------|------------------|---------|
| X                | UI        | 2 methods + 1 event | clean / leaky |

### Gaps (must address before implementation)
- **[Category]**: [Where the plan over-exposes or inverts a boundary]
  - Risk: [Coupling / leaked internals if built as planned]
  - Remediation: [Contract/visibility/type to specify in the plan]

### Concerns (should address)
- **[Category]**: [Description]
  - Remediation: [Plan change]

### Recommendations (consider)
- **[Category]**: [Description]
  - Note: [Why a tighter surface helps]

### Summary
- Gaps: N | Concerns: N | Recommendations: N
- Design readiness: [Ready / Needs Revision / Major Gaps]
- API surface outlook: [Minimal / Acceptable / Too Broad / Leaking]
```

## Output Format

```
## Interface Hygiene Review: [Feature/File]

### Critical Issues
- **[Category]**: `[member]` in [file:line]
  - Problem: [Concrete description of boundary violation or leaked detail]
  - Fix: [Exact access modifier or type change]

### Warnings
- **[Category]**: `[member]` in [file:line]
  - Problem: [Description]
  - Fix: [Approach]

### Suggestions
- **[Category]**: [file:line]
  - Note: [What and why it matters]

### Summary
- Critical: N | Warnings: N | Suggestions: N
- API surface rating: [Minimal / Acceptable / Too Broad / Leaking]
```
