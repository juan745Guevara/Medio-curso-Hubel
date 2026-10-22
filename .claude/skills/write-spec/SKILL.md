---
name: write-spec
description: Method to write a spec-first spec.md — analysis, glossary, user stories, FRs with EARS patterns, measurable NFRs, Given/When/Then ACs, out of scope and clarifications.
---

# Writing a spec

## 1. Analyze before writing
Read `specs/problem-analysis.md`. If the feature brings new information, add rows (actors, entities, constraints, gaps) citing where they come from.

## 2. Glossary first
Every business noun you use must be in `specs/glossary.md`. If it is not, add it with a one-line definition before using it.

## 3. User stories
`US-NN: As a <glossary role>, I want <action> so that <benefit>.` One per user goal, not per screen.

## 4. Functional requirements (EARS)
Always "shall". State the pattern in parentheses:

| Pattern | Template | Example |
|---|---|---|
| Ubiquitous | The system shall … | The system shall sign the UBL XML before sending it. |
| Event-driven | When <event>, the system shall … | When the Salesperson confirms issuance, the system shall generate the UBL XML. |
| State-driven | While <state>, the system shall … | While the Sales receipt total exceeds S/ 700, the system shall require a document. |
| Unwanted behaviour | If <failure>, then the system shall … | If the XML does not validate against the XSD, then the system shall stop the submission. |
| Optional feature | Where <condition>, the system shall … | Where the Buyer has an e-mail, the system shall send them the PDF. |

Forbidden: "could", "ideally", "fast", "easy", "etc.", technology names.

## 5. Measurable NFRs
Number + unit + condition: "≤ 2 s at p95 for 50 Items", "5 years without modification (hash)".

## 6. Acceptance criteria
```
AC-NN (FR-NN) — <short title>
  Given <initial state with concrete data>
  When <action>
  Then <observable result>
```
- Concrete data (amounts, RUC, dates), not "a valid value".
- At least one negative AC per FR that can fail.
- For calculations, the result must be checkable by hand.

## 7. Out of scope
Explicit list of what the feature does NOT do, to close discussions.

## 8. Clarifications
At least 3, each with a closed question a human can answer: `[NEEDS CLARIFICATION] …?`. They come from the analysis gaps; never invent them.

## 9. Wrap-up
- Update `specs/traceability/traceability.md` (one row per AC).
- If the FR touches regulations, check it has a row in `research.md` and in `regulatory-matrix.md`.
- Ask the `spec-reviewer` agent for a review.
