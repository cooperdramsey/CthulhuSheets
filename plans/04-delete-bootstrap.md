# Delete Bootstrap — Implementation Plan

> Item #4 from [docs/refactoring-analysis.md](../docs/refactoring-analysis.md). Tier 1.
> Smallest, safest Tier-1 item — a good first merge.

## Goal

Remove the unused Bootstrap dependency (8.5 MB in `wwwroot/lib/bootstrap/`, linked from
`index.html`) from the app. MudBlazor provides all component styling; **zero Bootstrap
classes are used anywhere** in the app markup (verified — every `row`/`container` match is a
custom scoped-CSS class name like `age-input-row`/`combat-container`). Because this is a PWA
whose published service worker precaches the entire asset manifest, Bootstrap is currently
downloaded and cached by every user on first visit and revalidated after every deploy.
Removing it is a direct first-load/payload win, not just repo hygiene. Also prune the
dead Blazor-template CSS leftovers in `app.css`.

## Requirements (as given)

From the analysis, item #4:

> `index.html` links `bootstrap.min.css`, and `wwwroot/lib/bootstrap/` is 8.5 MB. Verified
> zero Bootstrap classes are used anywhere; MudBlazor provides everything. The published
> service worker precaches the entire asset manifest, so Bootstrap is downloaded and cached by
> every user. Removing it is a direct payload/first-load win. While there: the Blazor-template
> leftovers in `app.css` (`.btn-primary`, `.valid.modified`, etc.) — keep `.validation-message`
> if you want it for the EditForm in the profile step, though MudBlazor renders its own errors.

## Decisions (resolved via clarification)

> **User unavailable this session — questions recorded, resolved with `[DEFAULT]`.**

1. **Delete the whole `wwwroot/lib/bootstrap/` folder, or just stop linking it?**
   **[DEFAULT] Delete the folder** and remove the `index.html` link. Leaving 8.5 MB of
   unreferenced files in the repo and in the published output (where the service worker will
   still precache anything in `wwwroot` that ends up in the asset manifest) defeats the
   purpose. Full removal.

2. **Which `app.css` template leftovers to remove vs. keep?**
   **[DEFAULT] Remove the Bootstrap-coupled leftovers, keep validation styling.** Remove:
   `a, .btn-link { color:… }`, `.btn-primary {…}`, the `.btn:focus/.btn-link.nav-link:focus/
   .form-control:focus/.form-check-input:focus` box-shadow rule, and `.content { padding-top
   }` — these target Bootstrap/Blazor-template classes the app doesn't use. **Keep**
   `.valid.modified`, `.invalid`, and `.validation-message` — the profile step uses an
   `EditForm` + `DataAnnotationsValidator`, and these style Blazor's built-in validation
   output. **Question for user:** MudBlazor renders its own field validation; do you rely on
   the Blazor `.validation-message` styling anywhere visible? Planned to **keep** the three
   validation rules (harmless if unused, actively useful if the EditForm shows messages);
   remove only the clearly Bootstrap-targeted rules.

3. **Anything else linked that's actually Bootstrap-dependent?**
   **[DEFAULT] No.** Verified `lib/` contains only `bootstrap/`; the only reference in tracked
   source is the single `index.html` `<link>`. MudBlazor CSS/JS and the app/scoped CSS are
   independent. No open-iconic or other transitive Bootstrap asset is referenced.

4. **`.gitattributes` / LFS or other config referencing the lib?**
   **[DEFAULT] Check and clean if present.** The repo has a `.gitattributes`; verify it has no
   bootstrap-specific rule to remove (step includes the check). No `.csproj` `<Content>`
   entry references it (WASM auto-includes `wwwroot/**`), so removing the files is sufficient.

## Alternatives considered

- **Keep Bootstrap "just in case."** Rejected — it's 8.5 MB shipped to every user for zero
  used classes; "just in case" is what the analysis already disproved by verifying usage.
- **Replace with a smaller CSS reset.** Rejected as unnecessary — MudBlazor ships its own
  baseline; `app.css` already has the minimal `html, body` font rule and the app renders
  entirely through Mud components. No reset is needed. (If a stray unstyled element appears
  after removal, add a targeted rule then — not a whole framework.)
- **Only remove the `<link>`, leave files.** Rejected — see decision #1.

## Assumptions

- No app markup depends on any Bootstrap class (verified: only custom scoped-CSS names match
  `row`/`container`/`btn`; the sole `btn` hit is the custom `roll-btn-root`).
- The three validation CSS rules are worth keeping (decision #2); removing them would only
  matter if the EditForm's native validation messages are styled by them.
- Removing files from `wwwroot` cleanly drops them from the published asset manifest and thus
  from service-worker precaching (this is the payoff; confirm at verification).

## Rules touched

**None.** Styling/dependency change only; no Call of Cthulhu mechanic is involved.

## Affected code

- `CthulhuSheets/wwwroot/index.html` — remove the
  `<link rel="stylesheet" href="lib/bootstrap/dist/css/bootstrap.min.css" />` line (line 10).
- `CthulhuSheets/wwwroot/lib/bootstrap/` — delete the entire directory (8.5 MB).
- `CthulhuSheets/wwwroot/css/app.css` — remove the Bootstrap/template-targeted rules
  (`a,.btn-link`; `.btn-primary`; the `.btn:focus…` box-shadow rule; `.content`); keep
  `.valid.modified`, `.invalid`, `.validation-message`, and the `html, body` / `h1:focus`
  base rules.
- `.gitattributes` — remove any bootstrap-specific line if one exists (verify).

**No persisted-model changes, no rules logic.** Saved characters are entirely unaffected.

## Implementation steps

1. **Remove the stylesheet link.** Delete line 10 of `index.html`. **Verify:** app still
   builds; visually the app is unchanged (MudBlazor styles everything).

2. **Delete the Bootstrap folder.** `git rm -r CthulhuSheets/wwwroot/lib/bootstrap`.
   **Verify:** `dotnet build` succeeds; `git status` shows the deletion; grep confirms no
   remaining tracked reference to `lib/bootstrap` outside build output.

3. **Prune `app.css` template leftovers** (decision #2). Remove the four Bootstrap-targeted
   rules; keep the validation trio and base rules. **Verify:** the profile-step EditForm still
   shows validation state on required fields (Name/Birthplace/Pronouns/Residence) — confirm
   the required-field validation UX is intact.

4. **Check `.gitattributes`** for any bootstrap-specific entry (e.g. LFS or linguist rule);
   remove if present. **Verify:** `git check-attr` sanity or just inspect the file.

5. **Run the app and click through every screen** — roster, creation (all steps), the sheet
   (all tabs), dialogs (portrait, confirm) — confirming nothing lost styling. **Verify:** no
   visual regression; browser devtools Network shows Bootstrap no longer requested.

6. **Confirm the published payload shrank.** `dotnet publish -c Release` and confirm
   `bootstrap` no longer appears in `wwwroot`/the service-worker asset manifest
   (`service-worker-assets.js`). **Verify:** manifest has no bootstrap entries; published
   output is ~8.5 MB lighter.

## Testing / verification

- Full click-through of the running app shows **no** visual regression on any screen or
  dialog (MudBlazor + scoped CSS carry all styling).
- The profile-step required-field validation still behaves (the kept `.validation-message`/
  `.invalid` rules).
- `dotnet publish` output and `service-worker-assets.js` contain no `bootstrap` references;
  first-load transfer size drops accordingly.
- `git grep -i bootstrap` on tracked source returns nothing (build artifacts excluded).

## Open risks

- **A hidden reliance on a Bootstrap class.** Mitigated by the verified-zero-usage grep, but
  the step-5 click-through is the real safety net — if any element looks unstyled, add a
  targeted scoped-CSS rule rather than restoring Bootstrap.
- **Validation styling.** If the profile EditForm's native validation messages turn out to be
  visibly styled only by the removed rules, they were among the *kept* trio — so this is
  covered; just confirm in step 3.
- **Trivial to revert.** If anything regresses, the change is a single link line + a folder +
  four CSS rules — fully reversible from git.
