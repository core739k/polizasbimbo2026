-- =====================================================================
-- 003 — Creación de dbo.DownloadTokens
-- Tokens de un solo uso (TTL 10 min) para el proxy de descarga.
-- =====================================================================

IF OBJECT_ID('dbo.DownloadTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DownloadTokens (
        jti         char(36)     NOT NULL,
        PolizaId    int          NOT NULL,
        IssuedAt    datetime2(0) NOT NULL,
        ConsumedAt  datetime2(0) NULL,
        CONSTRAINT PK_DownloadTokens PRIMARY KEY CLUSTERED (jti),
        CONSTRAINT FK_DownloadTokens_Poliza
            FOREIGN KEY (PolizaId) REFERENCES dbo.PolizasBimboTraspaso(id)
    ) ON [PRIMARY];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DownloadTokens_IssuedAt' AND object_id = OBJECT_ID('dbo.DownloadTokens'))
    CREATE NONCLUSTERED INDEX IX_DownloadTokens_IssuedAt
        ON dbo.DownloadTokens (IssuedAt);
GO
