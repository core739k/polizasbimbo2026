-- =====================================================================
-- 004 — Limpieza periódica de DownloadTokens (referencia)
-- Ejecutar como job diario de Azure SQL Elastic Job Agent o Logic App.
-- =====================================================================

DELETE FROM dbo.DownloadTokens
 WHERE IssuedAt < DATEADD(DAY, -1, SYSUTCDATETIME());
