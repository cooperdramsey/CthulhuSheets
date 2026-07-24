---
name: implement
description: "Implement a saved implementation plan end to end: read the plan, build it step by step at high code quality, write and run the tests it needs, document the code, then review it against the plan and report deviations. Use when: executing a plan from plans/, turning a reviewed plan into working code, implementing a planned feature, building from a plans/ document."
argument-hint: "Path to the plan to implement (e.g. plans/skill-improvement-phase.md); omit to pick from plans/"
---

# Implement — Execute a Plan End to End

Take a finished implementation plan (typically produced by `plan-with-review` and saved under `plans/`) and build it: read the plan thoroughly, implement it one step at a time at high code quality, write and run the tests the change actually needs, document the code as you go, then review the result against the plan with the `dotnet-code-review` skill and report what was built and where it diverged.

The deliverable is **working, reviewed, documented code that satisfies the plan** — not a re-plan. If the plan is wrong or ambiguous, surface it (see Deviations), don't silently redesign it.

## When to Use

- A reviewed plan exists (e.g. under `plans/`) and it's time to build the feature
- You want the implementation held to the project's quality bar and verified before it's called done
- You want an explicit record of how the built code differs from the plan it came from

## Inputs

- **Plan path** (the argument): the plan document to implement, e.g. `plans/skill-improvement-phase.md`.
- **No argument given** — resolve the plan before starting, don't guess:
  - List the markdown files in `plans/` (glob `plans/*.md`).
  - If exactly one exists, name it and confirm it's the one to implement before proceeding.
  - If several exist, present them with `AskUserQuestion` and let the user pick.
  - If `plans/` is empty or absent, say so and ask for a plan path (or suggest running `plan-with-review` first to produce one).

## Procedure

### Phase 1: Read the Plan Thoroughly

1. Read the entire plan start to finish before writing any code. Do not skim to the first step and begin.
2. Extract and hold onto: the **goal** and success criteria, the **confirmed design decisions** (a plan-with-review plan records these), the **ordered steps**, the **files/classes/methods** each step touches, the **rules/formulas** each step must satisfy (a plan records the condensed-rules file and exact formula for any mechanic in scope), and the **verification** the plan expects.
3. Note anything the plan leaves genuinely ambiguous or that appears to conflict with the current codebase. If a gap would change *what you build* (not just how), resolve it with `AskUserQuestion` before starting — one round, batched. Otherwise proceed on the plan's stated intent and record any judgment call as a deviation later.

### Phase 2: Ground in the Codebase

Before touching code, orient the same way the plan should have:

1. Read `CLAUDE.md` and the existing code the plan builds on, so the implementation *extends existing systems and matches established patterns* instead of reinventing them. Note the house conventions you'll be held to: design tokens for all CSS spacing/radius/icon sizing, MudBlazor palette colors, scoped `.razor.css` per component, and the partial-class pattern (`X.razor` markup + `X.razor.cs` logic).
2. For any game mechanic in scope, read the governing condensed rule file(s) in `references/rules_condensed/` **before** implementing the formula, so the code matches the rule rather than memory. (See the `rules-review` skill's area→chapter table for the mapping.)
3. Confirm the plan's assumptions about existing code still hold (files, class/method names, the seams it expects). If reality has drifted from the plan, note it — it becomes a deviation and may need a Phase 3 judgment call.

### Phase 3: Implement One Step at a Time

Work the plan's steps **in order**. For each step, complete it fully — code, build, and any tests it warrants — before moving to the next. Do not build the whole feature in one undifferentiated pass; a clean per-step cadence keeps the code reviewable and makes it obvious where a problem entered.

**For each step:**

1. **Build the smallest correct version of the step.** Follow the plan's file paths, class/method names, and layer placement — **domain/logic** (`Models/`, `Helpers/`, `Data/`) → **services** (`Services/`, `Services/Storage/`) → **UI** (`Pages/`, `Shared/`, `Layout/`). Where the plan and the project's conventions are silent, follow the existing code's shape.
2. **Hold the quality bar as you write** — the same standards the review skills enforce, applied at authoring time so the later review finds little:
   - **Simplicity first** is the tiebreaker: the fewest moving parts that works, minimal indirection, no abstraction that hasn't earned its place. When a quality principle (SOLID, DRY-at-2, thin-component purity) fights simplicity or readability, simplicity wins — *except* the project's hard rules below.
   - **SOLID / DRY / clean boundaries** where they make the code shorter or clearer — not as ceremony. Single clear responsibility per class, no needless duplication that will drift, keep rules math in `Helpers`/`Models` (never inline in a component), clean public surfaces (`private`/`internal` by default, `IReadOnlyList<T>` over exposed `List<T>` where mutation isn't needed).
   - **Naming & readability**: conventional names (`PascalCase` public, `_camelCase` private fields, `Async` suffix on async methods), guard clauses over deep nesting, methods that stay short enough to read at a glance. Component logic lives in the `.razor.cs` code-behind, not inline in markup.
   - **Error handling**: validate new boundaries — user input (creation forms, point-buy, manual stat edits) and storage reads via `ICharacterStore` — with no silently swallowed failures; fail fast on developer mistakes (`ArgumentNullException.ThrowIfNull`, `ArgumentOutOfRangeException`), and never leave a saved sheet in a corrupt state.
   - **Project hard rules (never traded away), from `CLAUDE.md`:**
     - Styling lives in scoped `.razor.css` (or global `app.css`); use the design tokens (`--space-*`, `--radius-*`, `--icon-*`) for every padding/margin/gap/radius/icon size and MudBlazor palette tokens (`--mud-palette-*`) for color — no new raw px for spacing or corners.
     - Components stay thin: markup in `X.razor`, logic in the `X.razor.cs` partial class.
     - Game mechanics must conform to `references/rules_condensed/` (7e) — a wrong value in `Data/Occupations.cs` or `Data/DefaultSkills.cs` silently corrupts every character.
     - Anything reachable from `Investigator` or `Roster` is a persisted shape that round-trips as JSON through `ICharacterStore`; a schema change must keep previously saved characters deserializing (migration via `StorageMigrator`, or a safe default).
     - `dotnet build` stays at **0 warnings / 0 errors**.
   - Reference the specialist skills (`solid-principles`, `dry-principles`, `code-simplification`, `naming-readability`, `error-handling`, `blazor-component-quality`, `interface-hygiene`, `performance-awareness`) when a step raises a question in that area — but apply them through the Simplicity-First lens above.
3. **Edit code with the normal file tools** (Edit/Write). After a step's edits, build (`dotnet build`) and confirm a clean compile — **0 warnings, 0 errors** — before moving on. Don't let warnings accumulate across steps.
4. **Write tests the step actually warrants** — following the project's testing doctrine, not coverage for its own sake:
   - **Test wrongness that is silent**: arithmetic (Damage Bonus/Build from STR+SIZ bands, `HP = (CON+SIZ)/10`, `MP = POW/5`, MOV bands, skill points, half/fifth, rounding and band-edge `≥` vs `>`), rule tables (`Data/Occupations.cs`, `Data/DefaultSkills.cs`), and round-trips (character save/restore through `ICharacterStore` / `CthulhuJson`). Add or extend **xUnit** tests in `CthulhuSheets.Tests`, using real models and data — **no fakes, no mocks, no seams added just to test**. Existing tests are the pattern: `CharacteristicHelperTests`, `SkillRulesTests`, `GameDataConsistencyTests`, `ModelDerivedValueTests`, `DiceRollServiceTests`.
   - **Skip wrongness a click reveals**: which tab opens, creation-wizard flow, styling, layout. Running the app is the test for those; don't pin them with brittle tests.
   - If a step seems to *need* a test double, treat that as a signal the logic is choreography, not arithmetic — reconsider before adding one.
5. **Delegation (optional, per the global tiering policy):** a well-scoped, decided step may be handed to the `implementer` subagent with an exact brief (files, acceptance criteria, verification command, relevant conventions). Keep architecture and ambiguous calls at this level, and review whatever comes back before continuing. If you delegate, warn the agent not to revert unrelated uncommitted work. For a single-file or trivial step, just do it inline.

### Phase 4: Document the Code

Once the build is complete, make sure the code carries enough explanation to be understood later without re-deriving it:

1. **In-code documentation** — clear XML/`///` summaries on new public types and non-obvious methods, and inline comments explaining *why* for any decision that isn't self-evident (a chosen trade-off, a rounding rule from the condensed rules, a persistence invariant, a non-obvious ordering). Match the surrounding code's comment density; don't narrate the obvious.
2. **Living documentation** — if the change adds, significantly modifies, or removes a system, update `CLAUDE.md` so its architecture/convention notes still hold (and add a `docs/backlog.md` follow-up entry if the change opens one). Do **not** edit anything under `references/` — it is copyrighted source (gitignored, machine-local), not project documentation you own.

### Phase 5: Verify

Confirm the whole thing actually works before calling it done:

1. **Clean build** — `dotnet build` succeeds with **0 warnings, 0 errors**.
2. **Run the full test suite** — `dotnet test`. It must be **green** (existing tests plus any you added). If a change legitimately invalidates a test that pinned a now-changed design, updating or deleting that test is a valid change — say so.
3. **Check the plan's own verification** — perform whatever the plan listed as its acceptance/verification. For behavior a test can't catch (a tab renders correctly, the creation flow works end to end), run the app and check it (see the `run` skill / `dotnet run`) rather than asserting it works.
4. If anything fails, fix it and re-verify. Do not proceed to review with a red suite, a failing build, or new warnings.

### Phase 6: Review Against the Plan (dotnet-code-review)

Run the `dotnet-code-review` skill in **Code Review Mode** against the code you wrote (the changed files / feature folder), reviewing it *against the plan's intent*. Don't re-derive its checklists here — invoke the skill and use its aggregated report. If the change is rules-bearing, also run the `rules-review` skill over the same scope to confirm the formulas match `references/rules_condensed/`. Then:

- **Fix** Critical findings and clear Warnings that are genuine (weighed against Simplicity First — a checklist "violation" that reads clearly, works, and doesn't break a hard rule is not a finding).
- **Re-run verification** (Phase 5) after applying fixes so the build stays clean and the suite stays green.
- If a finding is contested or you're unsure whether to act on it, raise it in the summary rather than silently accepting or dropping it.

### Phase 7: Summarize & Report Deviations

Report back to the user with:

1. **What was built** — a concise summary of the implemented steps and the key files/systems added or changed.
2. **Deviations from the plan** — every place the implementation diverged from what the plan said, each with a one-line *why*. Cover: steps done differently than written, steps skipped or added, design decisions the plan left open that you resolved, and cases where codebase reality forced a change. If there were none, say so explicitly.
3. **Verification result** — build status (warnings/errors), the `dotnet test` result, and what still needs manual checking in the running app.
4. **Review outcome** — the `dotnet-code-review` verdict (and `rules-review` findings if run), what you fixed, and any findings left open for the user to decide on.

## Notes

- This is an **implementation** skill: it builds the plan's code. It does not re-plan the feature — genuine plan problems are surfaced as questions (Phase 1/3) or recorded as deviations (Phase 7), not silently redesigned.
- Quality is applied **at authoring time**, then confirmed by review — the Phase 6 review should be a light pass, not a rescue.
- Simplicity First is the tiebreaker throughout, and the hard rules in `CLAUDE.md` are the only things it never overrides.
- A green `dotnet test` suite and a clean `dotnet build` (0 warnings, 0 errors) are the floor for "done"; they gate Phase 6, and Phase 6's fixes must not break them.
