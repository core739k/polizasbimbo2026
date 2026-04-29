-- =====================================================================
-- 003b — Creación de dbo.DownloadTokens (v2, sin FK al padrón)
--
-- Reemplaza el diseño de 003 (que tenía FK hacia PolizasBimboTraspaso).
-- Esta versión almacena en la propia fila todo el contexto del download:
-- archivo, colaborador, contacto. El portal ya no depende del padrón SQL.
--
-- Idempotente: si la tabla ya existe, no la recrea ni altera.
-- Para migrar bases existentes (con la versión 003 + FK), usar 005.
-- =====================================================================

IF OBJECT_ID('dbo.DownloadTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DownloadTokens (
        jti           char(36)      NOT NULL,
        NomArchivo    nvarchar(260) NOT NULL,
        IdColaborador int           NOT NULL,
        Email         nvarchar(100) NOT NULL,
        Telefono      nvarchar(50)  NOT NULL,
        IssuedAt      datetime2(0)  NOT NULL,
        ConsumedAt    datetime2(0)  NULL,
        CONSTRAINT PK_DownloadTokens PRIMARY KEY CLUSTERED (jti)
    ) ON [PRIMARY];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DownloadTokens_IssuedAt' AND object_id = OBJECT_ID('dbo.DownloadTokens'))
    CREATE NONCLUSTERED INDEX IX_DownloadTokens_IssuedAt
        ON dbo.DownloadTokens (IssuedAt);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DownloadTokens_IdColaborador' AND object_id = OBJECT_ID('dbo.DownloadTokens'))
    CREATE NONCLUSTERED INDEX IX_DownloadTokens_IdColaborador
        ON dbo.DownloadTokens (IdColaborador);
GO
