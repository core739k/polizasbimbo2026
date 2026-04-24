-- =====================================================================
-- 002 — Ajustes a dbo.ConsultaPolizasBimboTraspaso
-- Agrega PolizaId (FK), PaisOrigen, CiudadOrigen, PK e índices.
-- Preserva filas históricas rellenando 'Desconocido' antes de NOT NULL.
-- =====================================================================

IF COL_LENGTH('dbo.ConsultaPolizasBimboTraspaso', 'PolizaId') IS NULL
    ALTER TABLE dbo.ConsultaPolizasBimboTraspaso ADD PolizaId int NULL;
GO
IF COL_LENGTH('dbo.ConsultaPolizasBimboTraspaso', 'PaisOrigen') IS NULL
    ALTER TABLE dbo.ConsultaPolizasBimboTraspaso ADD PaisOrigen nvarchar(100) NULL;
GO
IF COL_LENGTH('dbo.ConsultaPolizasBimboTraspaso', 'CiudadOrigen') IS NULL
    ALTER TABLE dbo.ConsultaPolizasBimboTraspaso ADD CiudadOrigen nvarchar(100) NULL;
GO

-- Back-fill para históricos
UPDATE c
   SET c.PolizaId = p.id
  FROM dbo.ConsultaPolizasBimboTraspaso c
  JOIN dbo.PolizasBimboTraspaso p
    ON p.NumColaborador = c.NumColaborador
   AND p.NomArchivo     = c.NomArchivo
 WHERE c.PolizaId IS NULL;
GO

UPDATE dbo.ConsultaPolizasBimboTraspaso
   SET PaisOrigen   = ISNULL(PaisOrigen,   N'Desconocido'),
       CiudadOrigen = ISNULL(CiudadOrigen, N'Desconocido')
 WHERE PaisOrigen IS NULL OR CiudadOrigen IS NULL;
GO

ALTER TABLE dbo.ConsultaPolizasBimboTraspaso ALTER COLUMN PaisOrigen   nvarchar(100) NOT NULL;
ALTER TABLE dbo.ConsultaPolizasBimboTraspaso ALTER COLUMN CiudadOrigen nvarchar(100) NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_ConsultaPolizasBimboTraspaso')
    ALTER TABLE dbo.ConsultaPolizasBimboTraspaso ADD CONSTRAINT PK_ConsultaPolizasBimboTraspaso PRIMARY KEY CLUSTERED (id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_ConsultaPolizasBimbo_FechaCreacion')
    ALTER TABLE dbo.ConsultaPolizasBimboTraspaso
        ADD CONSTRAINT DF_ConsultaPolizasBimbo_FechaCreacion
        DEFAULT (SYSUTCDATETIME()) FOR FechaCreacion;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ConsultaPolizasBimbo_Poliza')
    ALTER TABLE dbo.ConsultaPolizasBimboTraspaso WITH NOCHECK
        ADD CONSTRAINT FK_ConsultaPolizasBimbo_Poliza
            FOREIGN KEY (PolizaId) REFERENCES dbo.PolizasBimboTraspaso(id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ConsultaPolizasBimboTraspaso_FechaCreacion' AND object_id = OBJECT_ID('dbo.ConsultaPolizasBimboTraspaso'))
    CREATE NONCLUSTERED INDEX IX_ConsultaPolizasBimboTraspaso_FechaCreacion
        ON dbo.ConsultaPolizasBimboTraspaso (FechaCreacion DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ConsultaPolizasBimboTraspaso_NumColaborador' AND object_id = OBJECT_ID('dbo.ConsultaPolizasBimboTraspaso'))
    CREATE NONCLUSTERED INDEX IX_ConsultaPolizasBimboTraspaso_NumColaborador
        ON dbo.ConsultaPolizasBimboTraspaso (NumColaborador);
GO
