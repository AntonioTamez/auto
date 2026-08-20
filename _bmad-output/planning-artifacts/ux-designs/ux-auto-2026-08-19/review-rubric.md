# Spine Pair Review — auto

## Overall verdict

The spine pair is a clean, source-extractable contract. Every color/typography/rounded/spacing token in DESIGN.md's frontmatter is defined with light/dark hex pairs and resolves from every `{path.to.token}` reference in both files' prose; every component named anywhere has both a DESIGN.md visual-spec row and an EXPERIENCE.md behavioral-spec row with real rules, not placeholders. Canonical section order is honored exactly in DESIGN.md, and EXPERIENCE.md carries all required defaults plus correctly-triggered optional sections (Inspiration & Anti-patterns, Responsive & Platform). The single sourced UJ (UJ-1) is verbatim-matched from the PRD and fully flowed with protagonist/steps/climax/failure; two invented flows (Diego, Ana) are explicitly memlog-justified to cover FR-11/12/15/16 gate and share behavior UJ-1 doesn't reach. No critical or high-severity issues found. Two medium-severity gaps are worth a cleanup pass: an orphaned `.working/` mockup documenting a superseded mobile-table hypothesis, and two IA surfaces (agency profile, saved comparisons) that have full component/state specs but no Key Flow walkthrough.

## 1. Flow coverage — strong
Checked: EXPERIENCE.md sources frontmatter (`{planning_artifacts}/prds/prd-auto-2026-08-19/prd.md`, resolves) against the PRD's single UJ (section 4) and all 13 user-facing FRs (FR-1–FR-3, FR-4–FR-9, FR-10–FR-12, FR-15–FR-16; FR-13 is a platform constraint, FR-14 is internal-tooling and correctly out of UX scope).

- UJ-1 ("Laura busca y compara antes de decidir") is verbatim-matched from PRD §4 in EXPERIENCE.md Key Flows → Flujo 1, with named protagonist, 6 numbered steps, an explicit **Clímax** beat, and a failure path (search/filter failure, non-blocking banner).
- Flujo 2 (Diego) and Flujo 3 (Ana) are inventions confirmed in `.memlog.md` line 19/15 to cover FR-11/FR-15 (save-triggers-account-gate) and FR-12 (public share view) — both have protagonist, numbered steps, climax, and failure path.

### Findings
- **medium** Two IA surfaces — "Perfil de agencia" (FR-2, FR-3) and "Mis comparaciones guardadas" (FR-16) — have full State Patterns rows and are named in Component Patterns, but no Key Flow ever walks a user into either surface (EXPERIENCE.md § Key Flows only visits Búsqueda/Resultados, Detalle de vehículo, Comparación, and the account gate). *Fix:* add a short flow (or a step folded into Flujo 1) that reaches agency profile and saved-comparisons, or note explicitly why they're considered adequately specified without one.

## 2. Token completeness — strong
Checked: every frontmatter key in DESIGN.md (16 color tokens, 9 typography roles, 6 rounded scale steps, 11 spacing values, 10 component token blocks) against every `{path.to.token}` reference in both files' prose (grep-verified, ~140 references in DESIGN.md, 22 in EXPERIENCE.md) — all resolve. All 16 color tokens ship hex values with light/dark pairs (per `design-md-spec.md` type rules); none are missing a pair. Contrast targets for load-bearing combinations are stated with verified ratios in EXPERIENCE.md → Accessibility Floor (not just declared — the tightest pair, `success` on `surface` at 4.8:1, is called out with a usage constraint).

### Findings
- **low** `{spacing.gutter}` (`24px`) is defined in DESIGN.md frontmatter but never referenced via `{spacing.gutter}` anywhere in either file's prose — a dead token. *Fix:* either cite it where a gutter value is used (e.g., comparison-table column gaps) or drop it from frontmatter.

## 3. Component coverage — strong
Extracted 10 component names (`vehicle-result-card`, `filter-chip`, `compare-select-button`, `compare-footer-bar`, `comparison-table`, `availability-badge`, `account-form-field`, `button-primary`, `button-secondary`, `empty-state-panel`) from DESIGN.md frontmatter/Components section. All 10 have a matching visual-spec entry in DESIGN.md.Components and a behavioral row in EXPERIENCE.md.Component Patterns (button-primary/button-secondary share one combined behavioral row, which still names both). Rules are concrete (anatomy, states, thresholds like the 3/3 comparison cap), not one-word descriptions. No misses.

## 4. State coverage — strong
Walked all 7 IA surfaces + 2 named "Estado" rows against empty/cold-load/error/freshness/focus/offline/permission-denied. Empty, cold-load, error, and data-freshness are covered per-surface in the State Patterns table, including a deliberately-unreachable empty state for agency profile (memlog-confirmed: agencies with 0 vehicles never list/link) and a never-expires-only-orphans-on-delete link-invalid state for shared comparisons.

### Findings
- **low** No dedicated "Focus" row in the State Patterns table. Focus behavior exists (Accessibility Floor: accent-ring focus indicator; `account-form-field`: validate-on-blur) but isn't tabulated as a state the way the Quill reference example does (`experience-example-mobile.md` → State Patterns has an explicit Focus row). Not a functional gap, just a coverage-table completeness note.
- **low** No offline state or explicit reasoning for its absence. This product's core actions (search, filter, compare) require live catalog data, so offline may genuinely not apply — but unlike the Quill example (which explicitly states "Offline write: save locally, no banner, sync on foreground"), Auto's spine never says this out loud. *Fix:* one line in State Patterns or Foundation noting connectivity loss is handled via the generic error/retry banners, no offline mode by design.

## 5. Visual reference coverage — adequate
`mockups/` and `wireframes/` do not exist; `imports/` exists and is empty. `.working/` holds 6 files. 5 of 6 are linked inline at the relevant section in one or both spines with a named illustration purpose: `direction-premium.html` (DESIGN.md Brand & Style + Components; EXPERIENCE.md IA), `color-themes-premium.html` (DESIGN.md Colors + Components; EXPERIENCE.md IA), and the three rejected directions — `direction-fresco-directo.html`, `direction-confiable-sobrio.html`, `direction-hibrido-premium-fresco.html` — each linked in EXPERIENCE.md → Inspiration & Anti-patterns with rationale for rejection. The external Claude Design artifact URL (`https://claude.ai/code/artifact/d7fe43d6-4b26-4b6d-8906-288e16833697`) is present in DESIGN.md → Components, correctly used for the mobile comparison-table (Propuesta B), as expected.

Both spines state "spines win over mocks in conflict" exactly once each (DESIGN.md line 191 area / blockquote; EXPERIENCE.md blockquote), which is correct — not repeated to the point of clutter, not missing.

### Findings
- **medium** `.working/key-comparison-table-mobile.html` is an orphan — not referenced from DESIGN.md or EXPERIENCE.md anywhere. Its own header frames it as an undecided hypothesis ("Este archivo NO decide el layout... para que el fundador la apruebe o la rechace") for a scroll-horizontal + fixed-label-column mobile table layout. `.memlog.md` (line 18) confirms this hypothesis was superseded: the mobile table layout was ultimately resolved via a 4-proposal Claude Design canvas exploration, and "Propuesta B" (stacked cards, not this file's scroll-horizontal approach) was chosen and is the one now linked from DESIGN.md. This file is stale and unpromoted — it neither reflects the shipped decision nor is marked rejected like the three direction files it sits alongside. *Fix:* either delete it (fully superseded, higher-fidelity replacement already linked) or add one line in EXPERIENCE.md → Inspiration & Anti-patterns treating it as a rejected mobile-table hypothesis, matching the treatment given the three rejected brand directions.

## 6. Bloat & overspecification — adequate
No pixel specs bypass tokens; every color/type/radius/spacing value in Components is expressed as a `{path.to.token}` reference, never a bare literal. No restatement of PRD personas, FR text, or scope sections — FRs are cited by number, not copied. DESIGN.md's prose carries genuine editorial voice tied to actual decisions (why the cold palette over the rejected warm/leather one, why serif is confined to price/name) — appropriate per the design-example-editorial.md calibration. EXPERIENCE.md prose stays behavioral, not decorative.

### Findings
- **low** EXPERIENCE.md → Interaction Primitives renders the filter pattern and the comparison-cap pattern as dense paragraphs rather than the bulleted format used in the shadcn/mobile reference examples (`experience-example-shadcn.md`, `experience-example-mobile.md`). Readable and information-complete, just denser to scan than the reference shape. Optional tightening, not a defect.

## 7. Inheritance discipline — strong
`sources: [{planning_artifacts}/prds/prd-auto-2026-08-19/prd.md]` resolves to the actual PRD file. UJ-1's name is verbatim-identical between PRD §4 and EXPERIENCE.md Key Flows. Component names are identical, character-for-character, across DESIGN.md frontmatter, DESIGN.md.Components prose headers, and EXPERIENCE.md.Component Patterns rows — no drift observed across the 10 components. Every EXPERIENCE.md `{path.to.token}` reference (Accessibility Floor's contrast list, Responsive & Platform's `{rounded.full}`) resolves to a DESIGN.md-defined token by name. Domain terms (agencia, marca, versión, comparación) are used consistently with PRD §8's glossary in both spines; neither spine restates a glossary section, which is fine — not a required section.

## 8. Shape fit — strong
DESIGN.md section order is exactly canonical: Brand & Style → Colors → Typography → Layout & Spacing → Elevation & Depth → Shapes → Components → Do's and Don'ts, nothing omitted, nothing reordered.

EXPERIENCE.md has all required defaults (Foundation, IA, Voice and Tone, Component Patterns, State Patterns, Interaction Primitives, Accessibility Floor, Key Flows) plus both required-when-applicable sections correctly triggered: Inspiration & Anti-patterns (memlog shows 3 rejected reference directions — triggered, present, well-used) and Responsive & Platform (multi-surface web+mobile with FR-13/NFR-3 parity — triggered, present). Section ordering matches the shadcn reference example's pattern (Responsive & Platform before Inspiration & Anti-patterns, before Key Flows last). No invented sections beyond the standard set.

## Mechanical notes

- **Label-format drift (cosmetic):** IA table uses "Búsqueda / Resultados" (spaced slash) and "Comparación compartida (vista pública)"; State Patterns, Component Patterns, and Key Flows prose consistently use "Búsqueda/Resultados" (no space) and "Comparación compartida (pública)". Same referent, different formatting — not a resolution failure, just inconsistent spacing/wording worth a pass.
- **Frontmatter completeness:** DESIGN.md has name, description, colors, typography, rounded, spacing, components, status, updated — complete per spec. EXPERIENCE.md has name, status, sources, updated — complete per spec, sources path resolves.
- **Cross-refs:** all 5 referenced `.working/*.html` files exist on disk and match their linked filenames exactly; the 1 unreferenced file is `key-comparison-table-mobile.html` (see Finding, §5). No broken relative links found.
- **`[ASSUMPTION]` tags:** only one remains in either file (EXPERIENCE.md → Foundation, mobile packaging), and `.memlog.md` confirms this is a deliberate, justified deferral to `bmad-architecture` (mirrors PRD's own NFR-5 flag), not an oversight. All other assumptions logged in memlog were resolved and folded into the current text.
- **Mermaid:** no Mermaid diagrams present in either file — N/A, nothing to validate.
