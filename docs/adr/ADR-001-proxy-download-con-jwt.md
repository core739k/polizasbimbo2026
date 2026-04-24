# ADR-001: Proxy de descarga con JWT opaco de un solo uso

Status: Accepted
Date: 2026-04-24

## Decision

Las descargas de pólizas se sirven por un proxy server-side (`POST /d/{token}`) que autentica con un JWT HMAC-SHA256 firmado por la aplicación, con TTL de 10 minutos y consumo único registrado en `dbo.DownloadTokens`.

## Context

El requerimiento explícito es que las URLs de Azure Blob no sean públicas ni dejen ver el dominio del blob al cliente. Se consideraron tres opciones:

1. **SAS token directo con redirect 302** — expone `mcbwebstorage.blob.core.windows.net` en la barra del navegador.
2. **Proxy con JWT opaco (elegida)** — el cliente solo ve `renovacionbimbo.mcbrokers.com.mx/d/{token}`; el servidor descarga del blob privado y hace streaming.
3. **Proxy con SAS interno** — equivalente a (2) en postura de seguridad, pero agrega complejidad al emitir SAS cortos en cada petición.

## Consequences

- URL completamente enmascarada extremo a extremo.
- Cada archivo se descarga solo una vez por token (defensa contra compartir enlaces).
- Se captura geolocalización por Geotargetly en el momento real de la descarga, no antes.
- El servidor asume el egress del blob; para PDFs pequeños (<5 MB) el costo es trivial.
- Requiere tabla `DownloadTokens` y limpieza periódica (job 004).
