# PRD Quality Review — Proyecto Auto (prd-auto-2026-08-19)

## Overall verdict

The strategic core is solid: one clear thesis (centralize agency/brand/vehicle discovery by location so buyers arrive informed) runs cleanly from Vision through Scope, the single UJ, the FRs, and into Success Metrics with real counter-metrics — no theater, no padding. What's at risk is execution readiness: the comparison feature (FR-10), the saved-comparison identity model (FR-11), and the interaction between the "disponibilidad" filter (FR-9) and the weekly data-refresh cadence (NFR-1) are underspecified in ways that will stall architecture and story creation. The PRD also has zero `[ASSUMPTION]` tags and zero `[NOTE FOR PM]` callouts despite making several unconfirmed inferences (no user-account model, weekly refresh acceptable to users, curation team can scale with geography) — for a document explicitly flagged as chain-top (NFR-5: "Flag para `bmad-architecture`"), that's a real gap, not just a stylistic one.

## Decision-readiness — adequate

Decisions are stated as decisions, not smuggled in as "considerations": the curated-catalog model (§2, §5.5), the MVP city choice (§3), and both Open Questions (§8) are explicit, each with an owner and — for monetization — a re-check trigger ("Revisar una vez validada la adopción en Monterrey"). The two Open Questions are genuinely open, not rhetorical: naming has an owner and an unblocking condition, monetization has an owner and a re-check condition. That's real decision-readiness on the items the PRD chose to flag.

The gap is what it didn't flag. There are no `[NOTE FOR PM]` callouts anywhere in the document, yet at least one real tension goes unacknowledged: NFR-5 commits to a product vision of multi-city expansion ("la visión del producto es multi-ubicación") while the curation model underpinning the entire catalog (FR-14, NFR-1) is a manual, team-driven weekly update process. Nothing in the PRD reconciles how a one-city-at-a-time manual curation process scales alongside the stated geographic ambition — this is exactly the kind of tension the rubric asks to surface with a `[NOTE FOR PM]`, and it's silent instead.

### Findings
- **high** Curated-catalog vs. multi-city ambition tension unflagged (§5.5 FR-14, §7 NFR-1, NFR-5) — NFR-5 explicitly plans for geographic scale while the catalog stays manually curated by "el equipo" on a weekly cadence; the PRD never names this as a trade-off or flags it as a decision point for the PM. *Fix:* Add a `[NOTE FOR PM]` at NFR-5 naming the curation-scaling risk explicitly, or state the plan for scaling curation headcount/tooling alongside city expansion.
- **medium** Curated-catalog trade-off stated as fact, not as a trade-off (§2, §5.5) — "Sin cuentas ni portal para agencias... el catálogo... es curado por el equipo, no autoservicio" explains the decision's consequence (needs a backoffice tool) but never names what's given up (catalog breadth/growth speed, agency-driven freshness) versus what's gained (data quality control). *Fix:* One sentence naming the trade-off explicitly would let a reader who'd push back ("why not let agencies self-serve?") find their objection addressed rather than dodged.

## Substance over theater — strong

No findings needed. Single persona and single UJ match a single-sided, no-agency-self-service MVP — not padding. No Differentiation/Innovation section was forced in where Discovery didn't earn one. NFRs carry real thresholds rather than boilerplate: NFR-2 ("menos de 2 segundos"), NFR-4 ("uptime objetivo de 99%"), and NFR-5 is explicitly tied to a stated product vision and routed to a specific downstream skill ("Flag para `bmad-architecture`") rather than asserted as generic "scalable" language. The Vision statement (§1) is specific to the Mexican multi-agency car-shopping pain point, not a swappable generic paragraph.

## Strategic coherence — strong

No findings needed. The thesis — buyers repeat discovery at every agency and want to compare before visiting — is stated once (§1) and then actually drives scope: FR-1/2 (locate), FR-3–9 (catalog/filter), FR-10–12 (compare/save/share) all trace back to "arrive informed." Success Metrics (§6) validate the thesis rather than measuring raw activity — "Número de comparaciones guardadas" and "compartidas" test whether the comparison actually generates trust, not just traffic. The counter-metrics are well-chosen and specifically guard against the two most plausible ways the primary metrics could be gamed or misleading: **Tasa de discrepancia reportada** guards against growing on stale/wrong data, and **Tasa de retorno (retención)** guards against one-off, non-durable comparisons.

## Done-ness clarity — thin

This is the PRD's weakest dimension, and it's the one the rubric says to be unforgiving on. Most FRs are testable at face value (FR-1–9 are concrete filter/list capabilities), but three areas leave "done" ambiguous in ways that will surface as blocking questions during architecture or story creation:

- **FR-10** ("comparar lado a lado (características y precio)") never enumerates which attributes appear in the comparison view. §5.2 defines seven filterable attributes (year, price, body type, transmission/engine, trim, color, availability) — does the comparison surface all seven, a subset, or something else? Without this, "done" for the PRD's flagship feature is not verifiable.
- **FR-11** ("guardar una comparación para consultarla después") implies persistence tied to a user, but §2 explicitly rules out accounts only for agencies, and never states whether the *comprador* has any account/session model. Is a saved comparison tied to a login, a device, a browser cookie, or an anonymous link? This is a foundational gap — it affects data model, security scope in NFRs, and cross-device behavior — not a cosmetic omission.
- **FR-9** ("filtrar... por... disponibilidad (unidad en stock)") reads as a live-inventory filter, but NFR-1 states the catalog refreshes weekly and only commits to flagging *prices* as approximate ("la UI debe comunicar explícitamente que los precios son aproximados") — availability is not mentioned as approximate. A buyer filtering by "in stock" on data that can be up to a week stale is a real correctness gap the PRD doesn't resolve.
- **FR-14** ("El equipo debe contar con una forma de cargar y actualizar agencias, marcas y vehículos") is the least specified FR in the document — "una forma" gives no acceptance bound (spreadsheet import? admin UI? direct DB writes?) and no testable consequence at all, similar to the "system handles X gracefully" pattern the rubric flags.

### Findings
- **critical** Saved-comparison identity/session model undefined (§5.3 FR-11, cf. §2) — no account system is described for the comprador, yet FR-11 requires per-user persistence across visits. This is foundational (affects data model, auth/security NFRs, cross-device UX) and left entirely unstated. *Fix:* Specify whether saved comparisons are anonymous/device-local, link-based (no login), or require a lightweight account, and add the corresponding NFR if security/privacy implications follow.
- **high** "Disponibilidad" filter vs. weekly refresh cadence unreconciled (§5.2 FR-9, §7 NFR-1) — FR-9 offers a stock-availability filter but NFR-1's approximate-data disclosure only covers price, not availability, despite availability being at least as time-sensitive. *Fix:* Either extend the NFR-1 disclosure to cover availability explicitly, or note it as a known MVP limitation with a `[NON-GOAL for MVP]`/`[ASSUMPTION]` tag.
- **high** Comparison table content unspecified (§5.3 FR-10) — no list of which attributes populate the side-by-side comparison. *Fix:* Enumerate the comparison fields (even "all filterable attributes from §5.2" would resolve this) so UX/architecture can scope the view.
- **medium** FR-14 has no testable acceptance condition (§5.5) — "una forma de cargar y actualizar" is unbounded; could mean anything from a spreadsheet import script to a full CRUD admin UI, with very different effort/architecture implications. *Fix:* State the minimum bar (e.g., "internal tool supporting bulk CSV import and manual per-record edit") even at MVP-rough granularity.

## Scope honesty — thin

The Non-Goals section (§3, "Fuera de alcance (MVP)") does real work — seven concrete, specific exclusions (no agency self-registration, no in-app purchase, no financing simulator, no test-drive scheduling, no reviews, no in-app messaging, no monetization) rather than a vague "future work" gesture. Open Questions (§8) are honestly deferred with owners and unblock conditions.

What's missing is any use of `[ASSUMPTION: …]` tagging. The PRD makes several inferential leaps the user likely didn't directly confirm — that weekly data refresh is an acceptable freshness bar for buyers, that no buyer-account system is needed for FR-11 to work, that "el equipo" curation capacity scales with catalog/city growth — none of which are tagged or indexed. Combined with the absence of `[NOTE FOR PM]` callouts (see Decision-readiness), the total open-items count is just the 2 Open Questions — low density for a document that is about to hand off to architecture and evidently contains unresolved inferences beyond naming and monetization.

### Findings
- **medium** No `[ASSUMPTION]` tags anywhere despite unconfirmed inferences (throughout §5, §7) — e.g., weekly refresh cadence being acceptable to users (NFR-1), no buyer-account system (implied by §2, contradicted by FR-11's persistence requirement), curation team capacity scaling with catalog/geography (FR-14, NFR-5). *Fix:* Tag these inline as `[ASSUMPTION: …]` and add an Assumptions Index, or resolve them directly if already decided.

## Downstream usability — thin

This PRD is chain-top by its own admission — NFR-5 explicitly routes a concern to `bmad-architecture` — so this dimension carries real weight, not the reduced weight the rubric allows for standalone PRDs.

There is no Glossary. Domain terms are used without a pinned definition, and at least one drifts: UJ-1 step 3 (§4, line 52) says "Llega a la agencia o concesionario," using "agencia" and "concesionario" interchangeably for what every other section (§3, §5.1, §5.2, §5.5) calls only "agencia." A downstream reader (UX or architecture) extracting this UJ in isolation can't tell if "concesionario" is a distinct entity or a synonym. FR IDs are contiguous (FR-1–14, no gaps or dupes) and the single UJ has a named, context-carrying protagonist (Laura) — those mechanics are clean. Success Metrics rows, however, have no IDs (just table rows), which is a minor cross-reference weakness if a future doc needs to cite "SM-2" directly.

### Findings
- **medium** No Glossary despite explicit chain-top routing to architecture (whole document; NFR-5 flags `bmad-architecture`) — domain terms (agencia, catálogo, comparación, disponibilidad) are never centrally defined, raising the risk of drift as UX/architecture/stories pull sections independently. *Fix:* Add a short Glossary pinning core nouns, especially "agencia" vs. any other term used for the same entity.
- **low** Terminology drift: "agencia" vs. "concesionario" (§4 UJ-1, line 52) — used interchangeably once, against "agencia" used consistently everywhere else. *Fix:* Standardize on "agencia" throughout, or define both terms in a Glossary if they're meant to be distinct.

## Shape fit — adequate

The core of the PRD is correctly shaped for a consumer product: one named-protagonist UJ (Laura) for a single-sided, no-self-service B2C MVP is right-sized — not padded with extra personas, not under-formalized with zero UJs. NFR-5's explicit routing to architecture rather than trying to solve geographic scaling in the PRD itself is the right call for a PRD-level document.

The one shape mismatch is FR-14: it introduces a second, structurally different stakeholder (the internal curation team) as a single bare FR bolted onto an otherwise consumer-product-shaped document, with no protagonist, workflow, or capability-spec framing of its own. It's a real requirement, but its current one-line treatment undersells how load-bearing it is — the entire product's data quality (and the freshness/availability tension flagged above) rests on this internal tool working well.

### Findings
- **low** FR-14 (internal backoffice tool) lacks any capability-spec framing (§5.5) — it's the operational backbone of the whole catalog but gets one sentence with no workflow detail, while the buyer-facing side gets a full UJ treatment. *Fix:* Not necessarily a full UJ, but a short capability note (who curates, how often, what the workflow looks like) would match its actual importance to the product.

## Mechanical notes

- **Glossary drift:** "agencia" vs. "concesionario" (§4, line 52) — see Downstream usability finding above.
- **ID continuity:** FR-1 through FR-14 are contiguous, unique, no gaps or duplicates. Only one UJ (UJ-1), correctly used. Success Metrics rows are unlabeled (no SM-IDs) — minor, would matter if a later doc needs to cite a specific metric by ID.
- **Assumptions Index roundtrip:** N/A — there are zero inline `[ASSUMPTION]` tags and no index, so there is nothing to roundtrip-check. The absence itself is covered under Scope honesty.
- **UJ protagonist naming:** UJ-1's protagonist (Laura) is named and carries context inline ("quiere cambiar de auto") — meets the bar.
- **Required sections:** Present — Visión y Problema, Usuarios objetivo, Alcance del MVP, User Journeys, Features/FRs, Métricas de éxito, NFRs, Open Questions. Absent: Glossary, Assumptions Index, an explicit Acceptance Criteria section (FRs carry implied acceptance only, uneven in strength per Done-ness clarity above).
