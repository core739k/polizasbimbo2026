# PolizasBimbo2026 — CLAUDE.md

Proyecto: Renovación del portal de descarga de pólizas Bimbo para vigencia 2026.
Stack: **ASP.NET Core (.NET 10) + Razor Pages + SQL Server + Azure Blob + Docker**.

> El proyecto legado está en `../PolizasBimbo2025` (ASP.NET Web Forms 4.7.2). No se modifica.

---

## Estructura

```
PolizasBimbo2026/
├── PolizasBimbo.slnx
├── src/
│   ├── PolizasBimbo.Domain/          # Entidades, Value Objects. Sin dependencias externas.
│   ├── PolizasBimbo.Application/     # Use cases y abstracciones (interfaces).
│   ├── PolizasBimbo.Infrastructure/  # EF Core, Azure Blob, JWT, CsvHelper.
│   └── PolizasBimbo.Web/             # Razor Pages + endpoints + DI.
├── tests/
│   ├── PolizasBimbo.Domain.Tests/
│   ├── PolizasBimbo.Application.Tests/
│   └── PolizasBimbo.Integration.Tests/
├── db/migrations/                    # DDL idempotente (001..004)
├── docs/adr/                         # Decisiones arquitectónicas
└── Dockerfile
```

Regla de dependencias: **Web → Application → Domain**, **Infrastructure → Application**. Domain no depende de nada.

---

## Comandos frecuentes

### Build y tests
```bash
dotnet build
dotnet test --logger "console;verbosity=minimal"
dotnet test tests/PolizasBimbo.Domain.Tests/PolizasBimbo.Domain.Tests.csproj
```

### Correr en local
```bash
dotnet run --project src/PolizasBimbo.Web
```
Escucha en `https://localhost:5001` (puerto por defecto de .NET 10 minimal webapp).

### Docker
```bash
docker build -t polizasbimbo2026 .
docker run -p 8080:8080 \
  -e ConnectionStrings__Default="..." \
  -e BlobStorage__ConnectionString="..." \
  -e TokenSigner__SigningKey="..." \
  -e Admin__ApiKey="..." \
  polizasbimbo2026
```

### Aplicar DDL
Ejecutar en orden contra la base `prodmacooley_desarrollo`:
```
db/migrations/001_alter_polizas_bimbo_traspaso.sql
db/migrations/002_alter_consulta_polizas_bimbo_traspaso.sql
db/migrations/003_create_download_tokens.sql
db/migrations/004_cleanup_job.sql   (opcional — job diario)
```
Los tres primeros son idempotentes; se pueden re-ejecutar sin efecto.

### Cargar padrón (admin)
```bash
curl -X POST "https://renovacionbimbo.mcbrokers.com.mx/admin/load-padron" \
  -H "X-Admin-Key: <clave>" \
  -F "file=@padron_2026.csv"
```
El CSV debe ser UTF-8, separado por comas, **sin cabecera**, columnas: `NumColaborador,NombreCompleto,NomArchivo`.

---

## Configuración (nunca en Git)

Las siguientes claves deben venir por variables de entorno o Azure Key Vault — no van en `appsettings.json`:

| Clave | Ejemplo |
|---|---|
| `ConnectionStrings__Default` | `Server=...;Database=...;Authentication=Active Directory Default` |
| `BlobStorage__ConnectionString` | `DefaultEndpointsProtocol=https;AccountName=mcbwebstorage;...` |
| `BlobStorage__Container` | `polizas-bimbo-2026` |
| `BlobStorage__Prefix` | `` (vacío o ruta dentro del contenedor) |
| `TokenSigner__SigningKey` | string ≥ 32 caracteres aleatorios |
| `Admin__ApiKey` | string aleatorio largo |

---

## Recursos pendientes

- **Fuente Gojal**: colocar `gojal.woff2` / `gojal.woff` / `gojal.ttf` en `src/PolizasBimbo.Web/wwwroot/fonts/gojal/`. El CSS ya hace referencia vía `@font-face`.
- **Contenedor Azure Blob**: crear manualmente `polizas-bimbo-2026` (privado) antes del primer deploy.
- **App Service Azure**: `renovacionbimbo2026` en Linux + custom domain `renovacionbimbo.mcbrokers.com.mx`.

---

## Flujo funcional

```
1. Usuario → GET /                           (form con nombre/email/teléfono)
2. Usuario → POST /api/search { nombre }     (FULLTEXT, TOP 5, rate-limit 10/min/IP)
            ← [{ policyId, fileName, downloadToken }]
3. Usuario → POST /d/{token}                  (Body: email, telefono, pais, ciudad)
            Backend: valida JWT + jti no consumido, UPDATE email/tel en padrón,
                     INSERT auditoría con geo, stream del blob privado.
```

`token` es un JWT HMAC-SHA256 con `{ jti, polId, exp }` y TTL 10 min. Ver ADR-001.

---

## Decisiones arquitectónicas
Ver `/docs/adr/`:
- ADR-001: Proxy de descarga con JWT opaco de un solo uso.
- ADR-002: Full-Text Index sobre NombreCompleto.
- ADR-003: Recarga del padrón con DELETE + INSERT transaccional.
