# Extract the "Roll vs Threshold" Check-Icon Component — Implementation Plan

> Item #10 from [docs/refactoring-analysis.md](../docs/refactoring-analysis.md). Tier 3.

## Goal

The pattern *if roll ≤ value show a green check, else a red X (and a placeholder span when
there's no roll yet)* is copy-pasted ~12 times across `SkillsTab.razor` (3×),
`CombatTab.razor` (6×), and `StatsTab.razor` (3×), each ~8 lines. Extract a tiny reusable
`ThresholdCheckIcon` component (params: the roll and the threshold) so the success/fail visual
is defined once and stays identical everywhere. This is the markup-side twin of #5's logic
consolidation. Also fold the duplicated bonus/penalty **label** switch (in `DiceFab` and
`RollButton`) into one shared helper.

## Requirements (as given)

From the analysis, item #10:

> The pattern *if roll ≤ value show green check else red X* is copy-pasted roughly twelve times
> across `SkillsTab.razor` (3×), `CombatTab.razor` (6×), and `StatsTab.razor` (3×), each ~8
> lines with placeholder-span else-branches. A tiny `ThresholdCheckIcon` component (params:
> `Roll`, `Threshold`) removes ~120 lines of markup and guarantees the success/fail visuals
> stay identical everywhere. The bonus/penalty label switch duplicated between `DiceFab` and
> `RollButton` is the same idea in miniature.

## Decisions (resolved via clarification)

> **User unavailable this session — questions recorded, resolved with `[DEFAULT]`.**

1. **Component API.**
   **[DEFAULT] `ThresholdCheckIcon` with `int? Roll`, `int? Threshold`, and an optional
   `Size` (default `Size.Small`).** Renders: nothing/placeholder when `Roll` or `Threshold` is
   null; a green `Check` when `Roll <= Threshold`; a red `Close` otherwise. It encapsulates the
   exact current logic `roll <= threshold ? Check/Success : Close/Error`. **Question for user:**
   should the "no roll yet" case render an empty placeholder span (to preserve grid alignment,
   as the current code does) or truly nothing? Planned: **render the placeholder span by
   default**, with a `ShowPlaceholder` param (default true) so grid layouts stay aligned and
   non-grid callers can opt out.

2. **Where does it live?**
   **[DEFAULT] `CthulhuSheets/Shared/ThresholdCheckIcon.razor`** (+ `.razor.cs` if any logic),
   alongside the other shared UI primitives (`RollButton`, `ConfirmDialog`, `PortraitUpload`).

3. **Does it use the design tokens / MudBlazor colors?**
   **[DEFAULT] Yes** — `Color.Success`/`Color.Error` (MudBlazor palette) exactly as the current
   inline usages; any spacing uses the `--space-*`/`--icon-*` tokens per `CLAUDE.md`. Since the
   current inline versions use `Class="char-check-icon"`/`"roll-check-icon"` etc. for
   positioning, the component should accept an optional `Class` passthrough so each call site
   keeps its existing alignment class. **Preserve the per-site CSS classes** — don't
   regress the layout.

4. **Placeholder-span classes differ per call site** (`char-check-placeholder`,
   `skill-roll-icon-placeholder`, `weapon-roll-icon-placeholder`). How to handle?
   **[DEFAULT] Expose a `PlaceholderClass` param** so each call site passes its existing
   placeholder class; the component renders `<span class="@PlaceholderClass"></span>` when no
   roll. This keeps every grid's spacing pixel-identical.

5. **Bonus/penalty label helper.**
   **[DEFAULT] Extract a `static string BonusPenaltyLabel(int)` and `BonusPenaltyClass(int)`
   into a small `Helpers/DiceModifierFormat.cs`;** `DiceFab` and `RollButton` call it. Their
   switches are identical (2→"+2 Bonus", 1→"+1 Bonus", 0→"Normal", −1→"-1 Penalty", else "-2
   Penalty"). Note the CSS class prefixes differ (`bonus-penalty-chip--*` vs `roll-popup-mod--*`),
   so `BonusPenaltyClass` takes the prefix or returns a bare suffix each site prefixes. Planned:
   helper returns the **label**; each site keeps its own class mapping (the labels are the real
   duplication; the class names are site-specific). Keep it minimal.

6. **Behavior/visual change?**
   **[DEFAULT] None.** Pixel-identical output. This is a markup dedup; if anything shifts
   visually, the per-site `Class`/`PlaceholderClass` passthrough was missed.

## Alternatives considered

- **A shared `RenderFragment` snippet / local helper method instead of a component.** Rejected —
  a component is the idiomatic Blazor reuse unit, is independently testable/inspectable, and
  reads cleanly at call sites (`<ThresholdCheckIcon Roll="..." Threshold="..." />`).
- **Push the roll-vs-threshold *logic* into the component and have call sites pass raw values
  only.** That's the plan (the component owns the comparison). The alternative — passing a
  precomputed bool — was rejected because it leaves the comparison duplicated at call sites,
  defeating the purpose.
- **Also merge the bonus/penalty class switches.** Rejected — the class prefixes are genuinely
  site-specific; only the labels are true duplication (decision #5).

## Assumptions

- All 12 call sites use the identical comparison (`roll <= threshold` → success). Verified in
  the review. Where a site compares against Half/Fifth thresholds, it still passes that
  specific threshold value — the component doesn't care which tier it is.
- Preserving each site's alignment/placeholder CSS class via passthrough keeps layouts
  pixel-identical.
- The component renders only within existing grid/flex cells; it introduces no new layout
  wrapper.

## Rules touched

**None directly** — this is presentation. The comparison it encapsulates (roll ≤ target =
success) reflects the core `ch_5` success check, but the component only *displays* a
pass/fail already decided by the caller's threshold; it defines no rule. (The success-level
nuance — Regular/Hard/Extreme — is already expressed by callers passing Regular/Half/Fifth as
the threshold; the component stays agnostic.)

## Affected code

New:
- `CthulhuSheets/Shared/ThresholdCheckIcon.razor` (+ `.razor.cs` if needed) — the reusable
  check/X/placeholder icon.
- `CthulhuSheets/Helpers/DiceModifierFormat.cs` — `BonusPenaltyLabel(int)` (shared label
  switch).

Changed (call sites replaced with the component; visuals identical):
- `Pages/Home/Components/StatsTab.razor` — the 3 `CheckIcon`/`CheckColor` inline blocks (Regular/
  Half/Fifth) become `<ThresholdCheckIcon>`; the `CheckIcon`/`CheckColor` helper methods in
  `StatsTab.razor.cs` can be removed if fully replaced.
- `Pages/Home/Components/SkillsTab.razor` — the 3 skill Regular/Half/Fifth check blocks.
- `Pages/Home/Components/CombatTab.razor` — the 6 dodge/weapon check blocks.
- `Pages/Home/Components/DiceFab.razor.cs` and `Shared/RollButton.razor.cs` — `BonusPenaltyLabel`/
  `ModifierLabel` delegate to `DiceModifierFormat.BonusPenaltyLabel`.

**No persisted-model changes, no rules logic.**

## Implementation steps

1. **Build `ThresholdCheckIcon`.** Params: `int? Roll`, `int? Threshold`, `Size Size =
   Size.Small`, `string? Class`, `bool ShowPlaceholder = true`, `string? PlaceholderClass`.
   Logic: if `Roll` or `Threshold` is null → render placeholder span (if `ShowPlaceholder`)
   else nothing; else render `MudIcon` `Check`/`Success` when `Roll <= Threshold`, `Close`/
   `Error` otherwise, with the passed `Class`. **Verify:** renders the three states correctly in
   isolation.

2. **Replace `StatsTab`'s three blocks.** Pass `stat.Regular`/`stat.Half`/`stat.Fifth` as
   thresholds and the stat's last roll as `Roll`; pass the existing `char-check-icon`/
   `char-check-placeholder` classes. Remove `CheckIcon`/`CheckColor` if now unused. **Verify:**
   the stats grid looks and behaves identically (roll a stat, see the check on Regular/Half/
   Fifth; placeholder alignment preserved).

3. **Replace `SkillsTab`'s three blocks** (Regular/Half/Fifth) similarly, passing
   `skill-roll-icon-placeholder`. **Verify:** skills grid identical.

4. **Replace `CombatTab`'s six blocks** (dodge Reg/Half/Fifth + weapon Reg/Half/Fifth),
   passing `roll-check-icon` / `weapon-roll-icon-placeholder`. **Verify:** combat grid
   identical.

5. **Extract `DiceModifierFormat.BonusPenaltyLabel`** and repoint `DiceFab.BonusPenaltyLabel`
   and `RollButton.ModifierLabel`. **Verify:** the bonus/penalty chip labels read identically in
   both the FAB and the roll popup.

6. **Grep for leftover inline check-icon patterns** (`Icons.Material.Filled.Check :
   Icons.Material.Filled.Close`) and confirm all display sites now use the component. **Verify:**
   only `ThresholdCheckIcon` renders the check/X pattern.

## Testing / verification

- Every affected grid (stats, skills, combat) renders pixel-identically before/after —
  including the empty-placeholder alignment when no roll has happened.
- Rolling a stat/skill/weapon/dodge shows the correct check/X on each tier exactly as before.
- Bonus/penalty labels identical in `DiceFab` and `RollButton`.
- `git diff` net-removes markup (~100+ lines) with no behavior change.

## Open risks

- **Grid alignment regressions** are the main risk — the placeholder span and per-site classes
  exist to keep grid columns aligned. The `Class`/`PlaceholderClass` passthrough (decisions
  #3/#4) mitigates; verify each grid visually.
- **`StatsTab.CheckIcon/CheckColor` removal** — only delete them once every caller is replaced;
  a lingering caller would break the build (caught immediately) — fine.
- Low overall risk; fully visual and reversible.
