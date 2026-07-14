# CthulhuSheets — Project Guide

A Call of Cthulhu 7e investigator sheet app.

## Tech & styling architecture

- **Blazor WebAssembly + MudBlazor** (Material Design component library).
- Styling lives in **scoped `.razor.css` files** (one per component, auto-scoped by Blazor) plus the global
  stylesheet [CthulhuSheets/wwwroot/css/app.css](CthulhuSheets/wwwroot/css/app.css).
- **Colors** come from MudBlazor palette tokens (`--mud-palette-*`), e.g. `var(--mud-palette-divider)`,
  `var(--mud-palette-surface)`.
- CSS custom properties declared in `:root` in `app.css` are **globally visible to every scoped `.razor.css`**
  (scoping only rewrites selectors, not `var()` lookups), so the design tokens below work everywhere.

## Spacing / radius / icon scale

Defined in `:root` in [app.css](CthulhuSheets/wwwroot/css/app.css). Use a token for **every padding, margin,
gap, border-radius, and custom icon size** — do not introduce new raw px spacing values.

### Spacing (4px grid; `2xs` is the one deliberate sub-grid value for tight internal rows)

| Token         | Value | Use for |
|---------------|-------|---------|
| `--space-2xs` | 2px   | Micro — tight internal rows (stat-card internals, sidebar rows) |
| `--space-xs`  | 4px   | Tiny gaps |
| `--space-sm`  | 8px   | Small gaps / compact padding |
| `--space-md`  | 12px  | **Standard** — the default gap between peer elements |
| `--space-lg`  | 16px  | Large — card padding, grid gaps |
| `--space-xl`  | 20px  | Extra large |
| `--space-2xl` | 24px  | Massive — section / content padding |

### Border radius

`--radius-sm` 6px · `--radius-md` 8px · `--radius-lg` 10px · `--radius-xl` 12px

### Icon size

`--icon-sm` 16px · `--icon-md` 20px · `--icon-lg` 24px

### Snapping rule (for off-grid legacy values you encounter)

`1–3px → --space-2xs` · `6px → --space-sm` · `10px → --space-sm or --space-md` · `14px → --space-lg` ·
`18px → --space-lg`. Pick the neighbor that best preserves the layout.

### When raw px is still allowed

The scale governs **spacing and corners**, not every dimension. Keep px for:
- Structural/layout dimensions — grid track sizes, `max-width`, fixed component widths/heights
  (e.g. `width: 250px`).
- `font-size` — typography is a separate concern; leave it in `rem`.
- `1px` borders.

### Designer's intent

Prefer **consistency over precision** — when unsure, pick the nearest token. Keep `--space-md` (standard) as
the default gap between peer elements, and reserve larger steps for hierarchically important blocks
(e.g. stat cards) where readability matters more.

### Migration status

The scale is established and applied in `InvestigatorSheet.razor.css` (tab rail, tab buttons, content pane,
outer padding). Other component CSS files still use legacy hardcoded px — **migrate them to tokens whenever you
touch them**, using the snapping rule above.
