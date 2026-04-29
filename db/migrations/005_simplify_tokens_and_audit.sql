-- =====================================================================
-- 005 — Simplificación: archivos vienen de Azure Blob, no del padrón SQL.
-- Migra DownloadTokens (si fue creada por la 003 con FK al padrón) y
-- amplía el ancho de NomArchivo en ConsultaPolizasBimboTraspaso para
-- que admita la ruta completa del blob.
--
-- Tras esta migración:
--   - DownloadTokens almacena (jti, NomArchivo, IdColaborador, Email,
--     Telefono, IssuedAt, ConsumedAt). El archivo lo identifica el
--     nombre del blob, no un PolizaId.
--   - ConsultaPolizasBimboTraspaso conserva su forma original
--     (id, NumColaborador, Email, Telefono, NomArchivo, FechaCreacion).
--     Solo se asegura que NomArchivo soporte hasta nvarchar(260).
--   - PolizasBimboTraspaso queda intacta (legado).
-- Idempotente: re-ejecutar es seguro.
-- =====================================================================

-- ----- DownloadTokens ------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DownloadTokens_Poliza')
    ALTER TABLE dbo.DownloadTokens DROP CONSTRAINT FK_DownloadTokens_Poliza;
GO

IF COL_LENGTH('dbo.DownloadTokens', 'NomArchivo') IS NULL
    ALTER TABLE dbo.DownloadTokens ADD NomArchivo nvarchar(260) NULL;
GO
IF COL_LENGTH('dbo.DownloadTokens', 'IdColaborador') IS NULL
    ALTER TABLE dbo.DownloadTokens ADD IdColaborador int NULL;
GO
IF COL_LENGTH('dbo.DownloadTokens', 'Email') IS NULL
    ALTER TABLE dbo.DownloadTokens ADD Email nvarchar(100) NULL;
GO
IF COL_LENGTH('dbo.DownloadTokens', 'Telefono') IS NULL
    ALTER TABLE dbo.DownloadTokens ADD Telefono nvarchar(50) NULL;
GO

-- Back-fill desde el padrón si quedan filas con PolizaId pero sin los nuevos campos.
IF COL_LENGTH('dbo.DownloadTokens', 'PolizaId') IS NOT NULL
BEGIN
    UPDATE t
       SET t.NomArchivo    = COALESCE(t.NomArchivo, p.NomArchivo),
           t.IdColaborador = COALESCE(t.IdColaborador, p.NumColaborador),
           t.Email         = COALESCE(t.Email, N''),
           t.Telefono      = COALESCE(t.Telefono, N'')
      FROM dbo.DownloadTokens t
      LEFT JOIN dbo.PolizasBimboTraspaso p ON p.id = t.PolizaId
     WHERE t.NomArchivo IS NULL OR t.IdColaborador IS NULL OR t.Email IS NULL OR t.Telefono IS NULL;
END
GO

UPDATE dbo.DownloadTokens
   SET NomArchivo    = ISNULL(NomArchivo,    N''),
       IdColaborador = ISNULL(IdColaborador, 0),
       Email         = ISNULL(Email,         N''),
       Telefono      = ISNULL(Telefono,      N'')
 WHERE NomArchivo IS NULL OR IdColaborador IS NULL OR Email IS NULL OR Telefono IS NULL;
GO

ALTER TABLE dbo.DownloadTokens ALTER COLUMN NomArchivo    nvarchar(260) NOT NULL;
ALTER TABLE dbo.DownloadTokens ALTER COLUMN IdColaborador int           NOT NULL;
ALTER TABLE dbo.DownloadTokens ALTER COLUMN Email         nvarchar(100) NOT NULL;
ALTER TABLE dbo.DownloadTokens ALTER COLUMN Telefono      nvarchar(50)  NOT NULL;
GO

IF COL_LENGTH('dbo.DownloadTokens', 'PolizaId') IS NOT NULL
    ALTER TABLE dbo.DownloadTokens DROP COLUMN PolizaId;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DownloadTokens_IdColaborador' AND object_id = OBJECT_ID('dbo.DownloadTokens'))
    CREATE NONCLUSTERED INDEX IX_DownloadTokens_IdColaborador
        ON dbo.DownloadTokens (IdColaborador);
GO

-- ----- ConsultaPolizasBimboTraspaso ----------------------------------
-- La tabla original solo tiene: id, NumColaborador, Email, Telefono,
-- NomArchivo, FechaCreacion. No requiere alteraciones de esquema más
-- allá de garantizar el ancho de NomArchivo.

IF EXISTS (
    SELECT 1
      FROM sys.columns
     WHERE object_id = OBJECT_ID('dbo.ConsultaPolizasBimboTraspaso')
       AND name = 'NomArchivo'
       AND max_length < 520  -- nvarchar(260) = 520 bytes
)
    ALTER TABLE dbo.ConsultaPolizasBimboTraspaso ALTER COLUMN NomArchivo nvarchar(260) NOT NULL;
GO
