---
title: PRD - [Proyecto Auto]
status: final
created: 2026-08-19
updated: 2026-08-19
---

# PRD - [Proyecto Auto]

> Nombre de trabajo. Naming definitivo pendiente — ver Open Questions.

## 1. Visión y Problema

Comprar un vehículo en México implica visitar múltiples agencias sin saber de antemano qué marcas manejan, qué versiones tienen disponibles ni a qué precio, obligando al comprador a repetir el mismo proceso de descubrimiento en cada punto de venta antes de poder comparar realmente sus opciones.

**[Proyecto Auto]** resuelve esto centralizando, para una ubicación dada (ciudad/estado), qué agencias existen, qué marcas comercializan y qué vehículos ofrecen — permitiendo al comprador filtrar y comparar antes de pisar una agencia, para que llegue a la conversación de compra con información concreta en mano en lugar de descubrirla sobre la marcha.

## 2. Usuarios objetivo

- **Comprador de vehículo (usuario primario):** persona en proceso activo de decidir qué auto comprar, que quiere comparar opciones entre agencias antes de visitarlas en persona.

## 3. Alcance del MVP

**Ciudad de lanzamiento:** Monterrey. **Cobertura de catálogo:** solo vehículos nuevos vendidos por agencias oficiales de marca (sin seminuevos/usados en el MVP).

**Dentro de alcance:**
- Ubicar agencias de vehículos por ciudad/estado/país
- Ver qué marcas comercializa cada agencia
- Listar vehículos por agencia
- Filtrar vehículos por año, precio, tipo de carrocería, transmisión/motor, equipamiento/versión, color y disponibilidad
- Comparar vehículos entre sí
- Guardar y compartir comparaciones
- Disponible en web y app móvil

**Fuera de alcance (MVP):**
- Autoregistro o portal de gestión para agencias (el catálogo lo carga el equipo)
- Compra o pago en línea dentro de la app
- Simulador de crédito/financiamiento
- Agendar test drive
- Reseñas/calificaciones de agencias o vendedores
- Mensajería/chat con la agencia dentro de la app
- Monetización (aún no definida — ver Open Questions)

**Trade-off asumido:** el catálogo curado manualmente da control de calidad sobre los datos pero no escala solo — cada ciudad nueva requiere trabajo humano de curación. Aceptable para validar el MVP en una sola ciudad (Monterrey); si el producto crece a más ciudades, este modelo debe revisitarse (ver NFR-5).

## 4. User Journeys

**UJ-1: Laura busca y compara antes de decidir**
- **Protagonista:** Laura, quiere cambiar de auto
- **Paso 1:** Abre la app (web o móvil) y busca vehículos, filtrando por agencias presentes en su estado/municipio, o filtrando directamente por precio
- **Paso 2:** Revisa resultados y compara varias opciones/versiones entre sí (características, precios)
- **Paso 3:** Llega a la agencia ya informada: sabe qué quiere, conoce las versiones existentes, sus características y precios aproximados
- **Resultado:** Laura negocia/compra con confianza, con información en mano

## 5. Features y Requerimientos Funcionales

### 5.1 Localización de agencias
- **FR-1:** El sistema debe permitir ubicar agencias de vehículos filtrando por ciudad, estado y país.
- **FR-2:** El sistema debe mostrar, para cada agencia, las marcas de vehículos que comercializa.

### 5.2 Catálogo de vehículos
- **FR-3:** El sistema debe listar los vehículos disponibles por agencia.
- **FR-4:** El sistema debe permitir filtrar vehículos por año.
- **FR-5:** El sistema debe permitir filtrar vehículos por precio.
- **FR-6:** El sistema debe permitir filtrar vehículos por tipo de carrocería.
- **FR-7:** El sistema debe permitir filtrar vehículos por transmisión y tipo de motor/combustible.
- **FR-8:** El sistema debe permitir filtrar vehículos por nivel de equipamiento/versión.
- **FR-9:** El sistema debe permitir filtrar vehículos por color y disponibilidad estimada (unidad en stock). La disponibilidad refleja la última actualización semanal del catálogo (ver NFR-1) y se etiqueta en la UI como estimada, no en tiempo real.

### 5.3 Comparación
- **FR-10:** El sistema debe permitir seleccionar dos o más vehículos y compararlos lado a lado, mostrando al menos: precio y año, tipo de carrocería, transmisión y motor/combustible, y nivel de equipamiento/versión.
- **FR-11:** El sistema debe permitir a un usuario con cuenta guardar una comparación para consultarla después.
- **FR-12:** El sistema debe permitir compartir una comparación mediante un link público de solo lectura, visible sin necesidad de cuenta para quien lo recibe.

### 5.4 Cuenta de usuario
- **FR-15:** El sistema debe permitir crear una cuenta simple (email o teléfono) para guardar comparaciones.
- **FR-16:** El sistema debe permitir iniciar sesión para acceder a las comparaciones guardadas previamente.

### 5.5 Plataforma
- **FR-13:** La aplicación debe estar disponible en web y en app móvil, con paridad funcional entre ambas.

### 5.6 Administración de catálogo
- **FR-14:** El equipo debe contar con una herramienta interna (backoffice) para cargar y actualizar agencias, marcas y vehículos — no hay autoservicio de agencias en el MVP. Consecuencia directa de la decisión de catálogo curado por el equipo (ver trade-off en sección 3). Criterio de aceptación: una persona del equipo, sin apoyo de ingeniería, puede dar de alta una agencia nueva con su catálogo de vehículos en menos de 30 minutos.

## 6. Métricas de éxito

| Métrica | Qué mide |
|---|---|
| Usuarios activos en Monterrey (semanales/mensuales) | Adopción real del MVP en su mercado de lanzamiento |
| Número de comparaciones guardadas | El comparador genera valor suficiente para querer conservarlo |
| Número de comparaciones compartidas | El usuario confía en la información al punto de compartirla (con pareja, familia, etc.) |

**Contra-métricas:**
- **Tasa de discrepancia reportada** entre precio/disponibilidad mostrado en la app y lo real en agencia — evita que crezcan usuarios/comparaciones a costa de datos desactualizados o poco confiables.
- **Tasa de retorno de usuarios (retención)** — evita optimizar por comparaciones de un solo uso que no reflejan valor sostenido.

## 7. Requerimientos No Funcionales

- **NFR-1 — Frescura de datos:** el catálogo (precios, disponibilidad) se actualiza semanalmente por el equipo de curación; la UI debe comunicar explícitamente que los precios son aproximados y pueden variar respecto a agencia.
- **NFR-2 — Rendimiento de búsqueda/filtrado:** resultados de búsqueda y combinaciones de filtros deben responder en menos de 2 segundos.
- **NFR-3 — Paridad funcional web/móvil:** ambas superficies exponen el mismo set de funcionalidades del MVP.
- **NFR-4 — Disponibilidad:** uptime objetivo de 99% en producción.
- **NFR-5 — Escalabilidad geográfica:** aunque el MVP lanza solo en Monterrey, el diseño de datos/carga debe permitir agregar ciudades/estados sin rediseño mayor, dado que la visión del producto es multi-ubicación. Flag para `bmad-architecture`.

## 8. Glosario

- **Agencia:** punto de venta físico de vehículos nuevos (concesionario). Término único usado en todo el documento.
- **Marca:** fabricante de vehículos (ej. Nissan, Toyota) comercializado por una o más agencias.
- **Versión:** variante específica de un modelo de vehículo (ej. nivel de equipamiento, motorización).
- **Comparación:** selección de dos o más vehículos vista lado a lado (FR-10), guardable (FR-11) y compartible vía link (FR-12).
- **Catálogo curado:** agencias, marcas y vehículos cargados y mantenidos por el equipo interno, sin autoservicio de las agencias (ver FR-14).

## 9. Open Questions

- **Naming definitivo:** se exploraron ~30 opciones en varias rondas sin converger; el producto avanza con el placeholder `[Proyecto Auto]`. Owner: Antonio. Resolver antes de cualquier trabajo de branding/UX visual o registro de marca/dominio — no bloquea PRD, arquitectura ni epics/stories.
- **Modelo de monetización:** aún no definido; el MVP es puramente informativo/de validación. Owner: Antonio. Revisar una vez validada la adopción en Monterrey.

