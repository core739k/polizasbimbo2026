# ADR-002: Full-Text Index sobre NombreCompleto

Status: Accepted
Date: 2026-04-24

## Decision

La búsqueda por nombre o apellido usa `CONTAINS(NombreCompleto, @query)` sobre un Full-Text Index con catálogo `ftPolizas` y lenguaje `'Spanish'`.

## Context

El flujo exige búsqueda con coincidencia parcial por nombre/apellido (equivalente a `LIKE '%x%'`). Un `LIKE` con comodín inicial no usa índice B-tree, por lo que genera table scan en cada consulta. Para decenas de miles de filas eso es lento y escala mal.

Alternativas descartadas:

- **LIKE '%x%' sin índice**: table scan por consulta.
- **Columna persistida `UPPER(Nombre)` + índice**: no ayuda al comodín inicial.
- **ElasticSearch / Azure AI Search**: infraestructura adicional desproporcionada para el caso.

## Consequences

- Búsqueda sublineal en tamaño de tabla incluso con comodines.
- Requiere la característica Full-Text habilitada en la instancia SQL (viene por defecto en Azure SQL).
- El `SearchTerm` construye la query como `"TOKEN*" AND "TOKEN*"` — prefix-match por palabra; asume que el usuario escribe palabras completas o prefijos.
- Indexación asíncrona con `CHANGE_TRACKING AUTO` — los cargues de padrón son visibles a búsqueda en segundos.
