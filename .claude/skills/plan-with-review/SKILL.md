---
name: plan-with-review
description: >-
  Turn a feature's requirements into a thorough, step-by-step implementation
  plan for CthulhuSheets, then self-review that plan before saving it. Use when
  the user asks to "plan a feature", "write an implementation plan", "plan out
  <feature> with review", or otherwise wants a vetted build plan rather than
  immediate code. Clarifies every assumption with the user (at least twice) via
  AskUserQuestion, makes the plan honor the Call of Cthulhu 7e rules in
  references/rules_condensed/, reviews the finished plan with the rules-review
  skill plus a scope/gap pass, presents findings + remediations for approval,
  then saves the final plan to plans/.
---

# Plan With Review

Produce a **complete, precise, rules-faithful implementation plan** from a set of
feature requirements — then **review the plan itself** before it is trusted for
implementation. The output is a saved plan document, not code. Do not write
feature code in this skill; the plan is the deliverable.

The bar: someone (you, later) should be able to implement the feature by
following the plan's steps one at a time, in order, without having to re-make any
decisions. Every assumption that could change the design must be resolved **with
the user**, not guessed.

## Inputs and output

- **Input:** the feature requirements — from the skill arguments and/or the
  conversation. If the requirements are thin, that is exactly what the
  clarification step is for; do not pad them with assumptions.
- **Rules source of truth:** `references/rules_condensed/*.md` (Call of Cthulhu
  7e). The plan must conform to these wherever the feature touches game
  mechanics. If a needed rule isn't condensed, fall back to
  `references/rules_md/`. See `rules-review`'s area→chapter table for the mapping.
- **Output:** `plans/<feature-slug>.md` (create the `plans/` folder if missing).
  Use a short kebab-case slug derived from the feature.

## Procedure

### 1. Understand the requirements and the codebase
Read the requirements. Locate the parts of the app the feature will touch — use
the code map in the `rules-review` skill (`CthulhuSheets/Models/`, `Helpers/`,
`Data/`, `Pages/.../Components/`, `Services/`) as a starting index. Read the
condensed rules for any mechanic in scope **before** drafting, so the plan is
grounded in both the real code and the real rules.

### 2. Clarify assumptions — at least twice, always
Before finalizing the plan you **must** call `AskUserQuestion` **at least two
separate times**. This is mandatory even if the feature seems obvious — there is
always more than one reasonable interpretation, and surfacing it is the point.

- Each round: gather the open questions you'd otherwise have to assume, and ask
  them (up to 4 per call, multi-select where the choices aren't exclusive).
- Ask about things that **change the plan**: scope boundaries, rules edge cases
  (rounding, band edges, which edition behavior), UX/placement, data model
  changes, validation, where computed vs. stored, interaction with existing
  features.
- Put a recommended option first and label it "(Recommended)" when you have a
  view, but let the user decide.
- Round two should incorporate round one's answers and dig into whatever they
  opened up. If genuine unknowns remain after two rounds, keep asking — two is
  the floor, not a quota to stop at. Only stop when no decision-changing
  ambiguity is left.

Never collapse an unresolved ambiguity into a silent assumption. If you truly
must record one, list it explicitly in the plan's "Assumptions" section and flag
it in the review.

### 3. Draft the plan
Write the plan to `plans/<feature-slug>.md` using the structure below. Break the
work into **discrete, ordered, individually-implementable steps** — each step
small enough to do and verify on its own, naming the concrete files/types to
touch and the exact rule or formula it must satisfy.

### 4. Review the plan (two passes)
Once drafted, review the plan itself:

1. **Rules review.** Invoke the `rules-review` skill **pointed at the plan
   document** (pass the plan path as the scope, e.g. "review
   `plans/<slug>.md` against the rules"). It checks every formula, threshold,
   rounding rule, and constraint the plan specifies against
   `references/rules_condensed/`. Capture its findings.
2. **Scope / gap pass.** Independently re-read the **original input
   requirements** against the plan and look for: missed scope, requirements with
   no corresponding step, unhandled edge cases, ordering problems (a step that
   depends on a later one), validation gaps, and anything the plan silently
   assumed. 

### 5. Present findings + remediations for approval
Present **everything found** in both passes to the user. For each finding give:
the issue, where in the plan it is, why it matters, and a **concrete proposed
remediation**. Do not fix anything yet — wait for the user's approval. If nothing
was found, say so plainly and still show the user the review was done.

### 6. Remediate on approval
After the user approves, apply the approved remediations to the plan file. If the
user's response opens new ambiguity, clarify again (AskUserQuestion) rather than
assume.

### 7. Save and summarize
Ensure the final, remediated plan is saved at `plans/<feature-slug>.md`. Then
present the user a **summary**: the feature, the key decisions settled during
clarification, the ordered steps at a glance, what the review found and how it
was resolved, and the saved file path as a clickable link.

## Plan document structure

```markdown
# <Feature> — Implementation Plan

## Goal
One-paragraph statement of what the feature does and why.

## Requirements (as given)
The input requirements, verbatim or faithfully restated.

## Decisions (resolved via clarification)
Each decision settled with the user, and the choice made.

## Assumptions
Any remaining assumptions, explicitly flagged. Ideally empty.

## Rules touched
The mechanics in scope and the condensed-rules file(s) that govern them, with
the exact formulas/thresholds the implementation must satisfy.

## Affected code
Files/types/components the plan will create or change, with a one-line role each.

## Implementation steps
1. <discrete step> — files to touch, rule/formula it satisfies, how to verify.
2. ...
(ordered; each independently implementable; dependencies noted.)

## Testing / verification
How to confirm the feature works and stays rules-faithful.

## Open risks
Anything that could still go wrong or need a follow-up decision.
```

## Guardrails

- **Two clarification rounds minimum** — never skip them, even for "simple"
  features. Assumptions made without the user are the failure mode this skill
  exists to prevent.
- The condensed rules win ties for any mechanic; cite the file. The app targets
  7e — don't invent rules from memory.
- This skill **plans and reviews only**. It does not implement the feature.
- Verify file/type names by reading the code; don't guess locations in the plan.
- Don't fix review findings before the user approves them.
