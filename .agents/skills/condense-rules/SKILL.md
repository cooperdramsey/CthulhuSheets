---
name: condense-rules
description: >-
  Condense a Call of Cthulhu rule chapter into a searchable, rules-only
  reference. Use when the user asks to condense / trim / "handle" one of the
  rules files (e.g. "condense ch_6 combat", "make a condensed version of the
  sanity rules", "do the remaining rules files"). Reads the full markdown in
  references/rules_md/ (plus the coordinate dump in references/rules_ocr/ for
  table fidelity) and writes a condensed file to references/rules_condensed/.
---

# Condense Rules

Turn a faithful-but-verbose rules chapter into a lean reference that keeps
**every mechanic** and **cuts all flavor**. Optimized for an LLM (or player) to
search during a session.

## Inputs and output

- **Source markdown:** `references/rules_md/<name>.md` — the programmatic
  conversion. Read it in full.
- **Coordinate ground truth:** `references/rules_ocr/<name>.txt` — `[y=..] x=..`
  cell dumps. Use this to rebuild any table the markdown fragmented (see below).
  These dumps are **generated, not committed**: `python
  references/extract_layout.py <pdf>` produces them from the source rulebook
  PDFs. If `rules_ocr/` is missing, regenerate it from the PDFs; if the PDFs
  aren't on this machine either, ask the user to restore them (see the fallback
  in step 2 — never guess table values).
- **Output:** `references/rules_condensed/<name>.md` (create the folder if
  missing). Keep the same `<name>` as the source.

Do **not** modify `rules_md/` or `rules_ocr/` — they are the full-fidelity
copies. Note that **everything under `references/` is gitignored** (copyrighted
source material), so never try to commit inputs or outputs — the files live
only on this machine. The gold-quality example to match is
`references/rules_md/appendix_equipment.md` (tables) and
`references/rules_condensed/ch_3_creating_investigators.md` (condensation style).

## Procedure

1. **Read** the whole `rules_md/<name>.md`.
2. **Spot fragmented tables.** The programmatic converter mangles multi-line
   table cells and mixes prose columns into grids. Any place you see broken
   pipe rows, stray `Build`/number fragments, or a table that doesn't parse,
   open `rules_ocr/<name>.txt`, find the rows by their section heading, and
   **rebuild the table from the x-banded coordinate cells** (cells are already
   sorted left-to-right; merge cells that wrap across consecutive `y` rows).
   Cross-check numbers — never guess a value the geometry doesn't support.
   **Fallback:** if the coordinate dump is unavailable and can't be regenerated
   (no PDFs), do not invent values — keep only what the markdown states
   unambiguously, mark each affected table with
   `<!-- UNVERIFIED: rebuilt without coordinate dump -->`, and list those
   tables in your report so the user can verify them against the book.
3. **Rewrite** into condensed markdown following Keep/Cut and Formatting below.
4. **Verify** against the checklist, then write the output file.
5. Report what was kept vs. cut and any table you rebuilt from coordinates.

## Keep (the rules)

- Every dice formula, roll, threshold, modifier, and derived value.
- All mechanical tables (characteristics, damage, costs, stat blocks, vehicle
  charts, etc.) — rebuild fragmented ones from the coordinate dump.
- Step-by-step procedures, difficulty rules, success/failure conditions.
- Optional/alternative rules (mark them "(optional)").
- Cross-references to other systems — but rewrite "(see page 94)" as
  "(see <System>)" or drop the page number; page numbers aren't useful here.

## Cut (the fluff)

- Worked examples / sample characters (e.g. the "Harvey Walters" walkthrough).
- Sidebars, advice, and "things to consider" boxes.
- Narrative/flavor quotes and Lovecraft excerpts.
- Descriptive flavor scales ("STR 90 = strongest person you've met").
- Decorative captions and repeated chapter furniture.
- Long random/inspiration tables that aren't required to play: note that they
  exist in one line rather than reproducing them. (If a random table *is* a core
  mechanic — e.g. Bouts of Madness — keep it.)

## Formatting conventions

- H1 title: `# <Topic> — Condensed Rules`.
- Lead with a one-line summary of the chapter's procedure/structure when useful.
- Prefer **tables** for any structured/repeated data; **bullet lists** for
  options and conditions; short bold run-in labels for definitions.
- Terse, declarative phrasing. One line per rule where possible.
- Use `---` to separate major sections. Keep `¢`, en-dashes, and dice notation
  (`1D10`, `2D6+1`) literal.
- Aim for roughly half the source length or less, with zero rules lost.

## Quality checklist

- [ ] Every number/formula/table from the source is present and correct.
- [ ] Fragmented tables were rebuilt from `rules_ocr/` and the values verified.
- [ ] No worked examples, sidebars, flavor, or page-number cross-refs remain.
- [ ] Output is in `references/rules_condensed/<name>.md`; sources untouched.

## Remaining files

Condense each `references/rules_md/*.md` not yet in `references/rules_condensed/`
— diff the two folders' file lists first to see what's actually left rather than
assuming. Table-heavy chapters (combat, spells, monsters, equipment) need the
most coordinate-dump verification; prose chapters (intro, mythos) condense
fastest.
