# Flujo BMad de punta a punta

Basado en el catálogo real del proyecto (`_bmad/_config/bmad-help.csv`), módulo **BMad Method**.

## Fase 1 — Ideación (opcional)
1. `bmad-brainstorming` — generar ideas si aún no tienes una dirección clara.
2. `bmad-forge-idea` (Core) — opcional, para presionar/probar la idea antes de comprometerte.

## Fase 2 — Planning
3. `bmad-product-brief` **o** `bmad-prfaq` — define el concepto de producto (brief simple vs. reto Working Backwards). Elige uno, no ambos.
4. `bmad-prd` — crea el PRD (requiere `bmad-product-brief` como precedente). **Requerido.**
5. `bmad-ux` — diseño UX, solo si el proyecto tiene UI relevante. Opcional.
6. `bmad-architecture` — la arquitectura "spine" (invariantes técnicas). **Requerido.** Necesita PRD (+UX si aplica).
7. `bmad-create-epics-and-stories` — descompón en épicas e historias. **Requerido.** Precedido por arquitectura.
8. `bmad-sprint-planning` (acción Sprint Planning) — gate de "listo para implementar" (PASS/CONCERNS/FAIL) + genera el sprint status. **Requerido.**

## Fase 3 — Ship / Implementación
9. `bmad-build` — el loop oficial de implementación (clarificar → planear → implementar → revisar → presentar). **Requerido.** Precedido por sprint planning.
10. `bmad-code-review` — revisión adicional ad-hoc tras `bmad-build`. Opcional pero recomendado.
11. `bmad-qa-generate-e2e-tests` — genera pruebas E2E/API sobre lo implementado. Opcional.
12. `bmad-checkpoint-preview` — walkthrough humano de un cambio/PR. Opcional.
13. `bmad-retrospective` — al cerrar la épica, lecciones aprendidas. Opcional, precedido por code review.

## Atajo alternativo
Si el proyecto es pequeño o no necesitas todo el ceremonial PRD→Arquitectura, puedes usar `bmad-spec` (Core) "anytime" para destilar directamente cualquier intent (brief, transcript, etc.) en un `SPEC.md` contrato, y saltar a `bmad-build`.

## Utilidades "anytime" (no forman parte de la secuencia lineal)
- `bmad-correct-course` — cuando algo cambia a mitad de camino.
- `bmad-review` (Core) — revisar cualquier doc/diff con distintos lentes.
- `bmad-advanced-elicitation` — profundizar un borrador recién producido.
- `bmad-party-mode` — discusión multi-agente.
- `bmad-customize` — override de comportamiento de agentes/workflows.
- `bmad-sprint-planning` (acción Status) — checar estado del sprint en cualquier momento.

## Sobre los "agentes con nombre" (Mary, John, Winston, Sally, Amelia)
Estos (`bmad-agent-analyst`, `bmad-agent-pm`, `bmad-agent-architect`, `bmad-agent-ux-designer`, `bmad-agent-dev`) son **personas conversacionales**, no pasos adicionales del flujo — son la forma "interactiva" de invocar las mismas etapas de arriba (John=PM lidera el PRD, Winston=Architect lidera la arquitectura, Sally=UX lidera bmad-ux, Amelia=Dev lidera bmad-build). Se pueden usar para conversar con esa fase en vez de invocar el skill directamente.

## Orden mínimo recomendado (todo lo `required=true`)
`bmad-prd → bmad-architecture → bmad-create-epics-and-stories → bmad-sprint-planning → bmad-build`
