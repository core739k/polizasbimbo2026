# ADR-003: Recarga del padrón con TRUNCATE + INSERT transaccional

Status: Accepted
Date: 2026-04-24

## Decision

El endpoint `POST /admin/load-padron` recibe el layout CSV/Excel completo del área usuaria, ejecuta `TRUNCATE TABLE dbo.PolizasBimboTraspaso` seguido de `INSERT` masivo, todo en una transacción.

## Context

El área usuaria entrega un layout completo (snapshot del padrón) cada vez que hay cambios; no envía incrementales. Conciliar incrementales con `MERGE` agrega complejidad sin valor si el archivo siempre es el estado autoritativo.

Alternativas consideradas:

- **MERGE por (NumColaborador, NomArchivo)**: maneja actualizaciones sin borrar, pero requiere definir qué hacer con filas ausentes y complica el contrato con el área.
- **Drop-recreate de tabla**: rompe la FK de `ConsultaPolizasBimboTraspaso`.

## Consequences

- Simple, atómico y fácil de auditar: después de cada carga la tabla es exactamente el layout entregado.
- La FK `FK_ConsultaPolizasBimbo_Poliza` puede impedir `TRUNCATE` — el script desactiva la constraint, recarga, y reactiva `WITH NOCHECK`. Alternativa: migrar a `DELETE` en la transacción (más lento para 40k filas, pero respeta FK sin trucos).
- Endpoint protegido por `X-Admin-Key`; la clave vive en Key Vault.
- El Full-Text Index (CHANGE_TRACKING AUTO) se reconstruye automáticamente.
