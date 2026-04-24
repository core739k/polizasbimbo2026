-- =====================================================================
-- 001 — Ajustes a dbo.PolizasBimboTraspaso
-- Agrega NombreCompleto/Email/Telefono/UpdatedAt, PK y Full-Text Index.
-- Idempotente: cada paso revisa su existencia antes de ejecutar.
-- =====================================================================

IF COL_LENGTH('dbo.PolizasBimboTraspaso', 'NombreCompleto') IS NULL
    ALTER TABLE dbo.PolizasBimboTraspaso ADD NombreCompleto varchar(200) NULL;
GO
IF COL_LENGTH('dbo.PolizasBimboTraspaso', 'Email') IS NULL
    ALTER TABLE dbo.PolizasBimboTraspaso ADD Email varchar(100) NULL;
GO
IF COL_LENGTH('dbo.PolizasBimboTraspaso', 'Telefono') IS NULL
    ALTER TABLE dbo.PolizasBimboTraspaso ADD Telefono varchar(50) NULL;
GO
IF COL_LENGTH('dbo.PolizasBimboTraspaso', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.PolizasBimboTraspaso ADD UpdatedAt datetime2(0) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_PolizasBimboTraspaso')
    ALTER TABLE dbo.PolizasBimboTraspaso ADD CONSTRAINT PK_PolizasBimboTraspaso PRIMARY KEY CLUSTERED (id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PolizasBimboTraspaso_NumColaborador' AND object_id = OBJECT_ID('dbo.PolizasBimboTraspaso'))
    CREATE NONCLUSTERED INDEX IX_PolizasBimboTraspaso_NumColaborador
        ON dbo.PolizasBimboTraspaso (NumColaborador)
        INCLUDE (NomArchivo, NombreCompleto);
GO

-- Full-Text Index (requiere la característica Full-Text habilitada)
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'ftPolizas')
    CREATE FULLTEXT CATALOG ftPolizas AS DEFAULT;
GO

IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.PolizasBimboTraspaso'))
    CREATE FULLTEXT INDEX ON dbo.PolizasBimboTraspaso(NombreCompleto LANGUAGE 'Spanish')
        KEY INDEX PK_PolizasBimboTraspaso
        ON ftPolizas
        WITH CHANGE_TRACKING AUTO;
GO
