# Small Cleanups (Batched) — Implementation Plan

> Item #12 from [docs/refactoring-analysis.md](../docs/refactoring-analysis.md). Tier 3.
> A grab-bag of small, independent fixes. Each sub-item is separately shippable; they're
> batched only for convenience. **Two sub-items (D startup-redirect, E offline fonts) carry
> real behavior questions and should not be lumped into a "trivial" commit blindly.**

## Goal

Clear out the low-cost debris and minor correctness/robustness nits found during the review:
a dead empty file, a tracked build artifact, a double JS-interop round-trip, an inconsistent
`StateHasChanged` pattern, a possible startup-redirect race, an offline-fragile fonts CDN, and
a stale `TODO`. None are architectural; all are worth doing when nearby.

## Requirements (as given)

From the analysis, item #12 (verbatim sub-items):

> - Dead files/artifacts: `Helpers/StringOrNumberJsonConverter.cs` is an empty file;
>   `CthulhuSheets.csproj.lscache` is a tracked build artifact (add to .gitignore).
> - `eval` interop in RollButton: `OpenPopover` calls `JSRuntime.InvokeAsync<double>("eval",
>   "window.innerWidth")` twice. Add a one-line `getViewport()` helper to app.js — one
>   round-trip instead of two, and `eval` will bite you the day you add a CSP to the PWA.
> - `StateHasChanged` from service events: `MainLayout` and `Home` invoke `StateHasChanged`
>   directly from `OnChanged`; `Roster` correctly wraps in `InvokeAsync`. Harmless on WASM
>   today, but worth unifying on the correct pattern.
> - Startup redirect worth double-checking: `Home.OnInitialized` redirects to the roster when
>   `Current is null`, but MainLayout's `RestoreActiveAsync` is still awaiting at that moment —
>   so a returning user with an active character may land on the roster instead of their sheet.
> - Google Fonts CDN in index.html: an offline PWA loses Roboto; self-hosting it makes offline
>   rendering consistent.
> - Occupations.cs `TODO` about JSON storage: resolve it the other way — keep it as C# and
>   delete the TODO.

## Sub-items, decisions, and steps

> **User unavailable this session — questions recorded, resolved with `[DEFAULT]`.**
> Each sub-item is independent; implement/verify separately.

### A. Delete the empty `StringOrNumberJsonConverter.cs`
- **Fact (verified):** the file is 0 bytes and referenced nowhere.
- **[DEFAULT]** `git rm CthulhuSheets/Helpers/StringOrNumberJsonConverter.cs`.
- **Question for user:** was this a placeholder for a converter you intended to write (e.g. to
  tolerate string-or-number JSON on import)? If so, that's a real feature (note it), not this
  cleanup. Planned: **delete** as dead.
- **Verify:** solution builds; nothing referenced it.

### B. Stop tracking `CthulhuSheets.csproj.lscache`
- **Fact (verified):** it's tracked and `lscache` is **not** in `.gitignore`.
- **[DEFAULT]** `git rm --cached CthulhuSheets/CthulhuSheets.csproj.lscache` and add
  `*.lscache` to `.gitignore`.
- **Verify:** `git status` shows it removed from tracking and ignored going forward; it's a
  local build artifact so removing from the index doesn't affect builds.

### C. Replace the double `eval` interop with a single `getViewport()` JS helper
- **Fact (verified):** `RollButton.OpenPopover` calls `eval` twice (`window.innerWidth`,
  `window.innerHeight`).
- **[DEFAULT]** Add to `wwwroot/js/app.js`:
  `window.getViewport = () => ({ width: window.innerWidth, height: window.innerHeight });`
  and change `OpenPopover` to one `await JSRuntime.InvokeAsync<Viewport>("getViewport")` call
  (with a small `record Viewport(double Width, double Height)` or a `double[]`). Removes the
  `eval` usage (CSP-friendly) and halves the interop round-trips.
- **Verify:** the roll-modifier popup still positions correctly, including the flip-left/flip-
  below edge cases (open it near the right and top edges of the window).

### D. Unify `StateHasChanged`-from-event on the `InvokeAsync` pattern — **and check the
startup-redirect race** (these two are related; do together)
- **Fact (verified):** `MainLayout` subscribes `OnChanged += StateHasChanged` directly; `Home`
  uses `HandleChanged` which calls `StateHasChanged`/navigates; `Roster` correctly wraps in
  `InvokeAsync`. Also, `MainLayout.OnInitializedAsync` `await`s `InitializeAsync()` then
  `RestoreActiveAsync()`; `Home.OnInitialized` redirects to `roster` when `Current is null`.
- **[DEFAULT]**
  1. Wrap event-driven `StateHasChanged` in `InvokeAsync(StateHasChanged)` in `MainLayout` and
     `Home` (match `Roster`'s correct pattern), so a callback raised off the render thread is
     safe. Low risk, correctness hygiene.
  2. **Startup redirect:** confirm whether a returning user with a saved active character can
     briefly/incorrectly land on the roster because `Home.OnInitialized` runs while
     `RestoreActiveAsync` is still in flight. **[DEFAULT] fix defensively:** gate `Home`'s
     redirect so it only fires once initialization has completed (e.g. `Home` reacts to
     `OnChanged`/an "initialized" signal rather than redirecting eagerly in `OnInitialized`
     when `Current` may not yet be populated). If, on inspection, the ordering already
     guarantees `RestoreActiveAsync` completes before `Home` renders (MainLayout wraps the
     router, so its `OnInitializedAsync` may complete first), then this is a no-op — **document
     which it is** rather than changing behavior blindly.
- **Question for user:** is landing on the roster (vs. the last active sheet) on reload the
  *intended* behavior? If yes, D.2 is "document, don't change." Planned: **investigate first,
  then either fix the race or document that the landing page is intentional.**
- **Verify:** on reload with an active character, the app lands where intended (sheet if that's
  the goal), with no flash of the roster; the render-thread wrapping causes no regression.

### E. Self-host the Roboto font (offline PWA consistency)
- **Fact (verified):** `index.html` links `https://fonts.googleapis.com/...Roboto...`.
- **[DEFAULT]** Download the Roboto weights used (300/400/500/700), place them under
  `wwwroot/fonts/`, and add an `@font-face`/local stylesheet; remove the CDN `<link>`. This
  keeps typography consistent offline (a PWA goal) and is CSP-friendlier.
- **Question for user:** is offline fidelity of the font important enough to add ~100–200 KB of
  font files to the payload, or is falling back to MudBlazor's system font stack when offline
  acceptable? **This is a real trade-off (payload vs. offline fidelity)** — planned as
  **self-host**, but flag it; if payload matters more, the alternative is to drop the CDN link
  and rely on MudBlazor's fallback fonts (zero bytes, slightly different look offline).
- **Verify:** with network throttled to offline, the app renders in Roboto (self-host path) or
  in a clean fallback (drop-CDN path), per the chosen option; no layout shift.

### F. Resolve the `Occupations.cs` TODO
- **Fact (verified):** `Occupations.cs` has `// TODO may find alternative means of storage for
  this? Maybe static config json?`.
- **[DEFAULT]** **Keep it as C# and delete the TODO.** Rationale (per the analysis): compile-time
  typo-proofing plus plan #1's data cross-validation test beats runtime JSON parsing; there's no
  benefit to externalizing static, versioned game data that ships with the app.
- **Verify:** the TODO comment is gone; `Occupations.All` unchanged.

## Alternatives considered

- **Batch everything into one commit.** Rejected — A/B/C/F are trivial and can batch, but D and
  E carry behavior/UX decisions and deserve their own commits (and possibly the user's input).
  Split accordingly.
- **(C) keep `eval` but call once with a combined expression.** Rejected — a named
  `getViewport()` helper is CSP-safe and clearer than `eval("[window.innerWidth,
  window.innerHeight]")`.
- **(E) leave the CDN.** Rejected as the default for an offline-first PWA, but kept as the
  explicit fallback if payload is the priority.

## Assumptions

- (A) The empty converter is dead, not an intended stub (flagged as a question).
- (D) Wrapping `StateHasChanged` in `InvokeAsync` is behavior-neutral on WASM (it is) and the
  startup redirect's correct behavior is TBD pending the user's intent (flagged).
- (E) Adding self-hosted font files is acceptable payload (flagged as a trade-off).

## Rules touched

**None.** All sub-items are infrastructure/hygiene/UX; no Call of Cthulhu mechanic is involved.
(`Occupations.cs` holds rules *data*, but F only removes a comment — the data is untouched, and
plan #1's cross-validation test is the guard on its correctness.)

## Affected code

- `CthulhuSheets/Helpers/StringOrNumberJsonConverter.cs` — deleted (A).
- `.gitignore` (+ untrack) — `*.lscache` (B).
- `CthulhuSheets/wwwroot/js/app.js` + `Shared/RollButton.razor.cs` — `getViewport` (C).
- `CthulhuSheets/Layout/MainLayout.razor.cs`, `Pages/Home/Home.razor.cs` — `InvokeAsync`
  wrapping + startup redirect (D).
- `CthulhuSheets/wwwroot/index.html` (+ `wwwroot/fonts/` + a font CSS) — fonts (E).
- `CthulhuSheets/Data/Occupations.cs` — delete TODO (F).

**No persisted-model changes.** (D) touches render/navigation timing only; no data shape
changes.

## Testing / verification

- Build clean after A/B/F.
- (C) roll-modifier popup positions correctly incl. edge flips; no `eval` remains in the app.
- (D) reload-with-active-character lands where intended, no roster flash; no render warnings.
- (E) offline render matches the chosen option.
- `git grep -n 'TODO' CthulhuSheets/Data/Occupations.cs` returns nothing.

## Open risks

- **(D) is the only sub-item with genuine behavior nuance** — the startup-redirect intent is a
  user decision; default is investigate-then-fix-or-document, not a blind change.
- **(E) payload vs. offline-fidelity** is a real trade-off; don't add font files without
  acknowledging the size cost, and offer the zero-byte fallback.
- **(A)** deleting the empty converter is safe only if it wasn't a deliberate stub — flagged.
- Everything else (B, C, F) is trivial and reversible.
