# PolizasBimbo2026 — CLAUDE.md

Proyecto: Renovación del portal de descarga de pólizas Bimbo para vigencia 2026.
Stack: **ASP.NET Core (.NET 10) + Razor Pages + SQL Server + Azure Blob Storage + Docker**.
Los archivos PDF de pólizas viven en un contenedor Azure Blob **privado** (`mcbwebstorage / archivos / bimbo/renovacion2026/`); el portal los enumera y stream-ea por nombre del colaborador, jamás expone URLs directas.

> El proyecto legado está en `../PolizasBimbo2025` (ASP.NET Web Forms 4.7.2). No se modifica.

---

## Estructura

```
PolizasBimbo2026/
├── PolizasBimbo.slnx
├── src/
│   ├── PolizasBimbo.Domain/          # Entidades, Value Objects. Sin dependencias externas.
│   ├── PolizasBimbo.Application/     # Use cases y abstracciones (interfaces).
│   ├── PolizasBimbo.Infrastructure/  # EF Core, Azure Blob, JWT.
│   └── PolizasBimbo.Web/             # Razor Pages + endpoints + DI.
├── tests/
│   ├── PolizasBimbo.Domain.Tests/
│   ├── PolizasBimbo.Application.Tests/
│   └── PolizasBimbo.Integration.Tests/
├── db/migrations/                    # DDL idempotente
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
Escucha en el puerto definido por `launchSettings.json` (Development: `http://localhost:5003`).

### Docker
```bash
docker build -t polizasbimbo2026 .
docker run -p 8080:8080 \
  -e ConnectionStrings__Default="..." \
  -e BlobStorage__ConnectionString="..." \
  -e TokenSigner__SigningKey="..." \
  polizasbimbo2026
```

### Aplicar DDL
Bases nuevas — ejecutar en orden contra la base `prodmacooley_desarrollo`:
```
db/migrations/003_create_download_tokens.sql
db/migrations/004_cleanup_job.sql               (opcional — job diario)
db/migrations/005_simplify_tokens_and_audit.sql
```
Bases que ya tenían 001/002 aplicadas: correr únicamente `005_simplify_tokens_and_audit.sql` — desacopla `DownloadTokens` y `ConsultaPolizasBimboTraspaso` de `PolizasBimboTraspaso` (esa tabla queda intacta como legado).

001 y 002 están obsoletos: alteraban `PolizasBimboTraspaso` y `ConsultaPolizasBimboTraspaso` para un flujo basado en padrón SQL que ya no aplica.

---

## Configuración (nunca en Git)

Las siguientes claves deben venir por User Secrets (Development), variables de entorno o Azure Key Vault — no van en `appsettings.json`:

| Clave (User Secrets / `:`) | Variable de entorno (`__`) | Ejemplo |
|---|---|---|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | `Server=...;Database=prodmacooley_desarrollo;Authentication=Active Directory Default` |
| `BlobStorage:ConnectionString` | `BlobStorage__ConnectionString` | `DefaultEndpointsProtocol=https;AccountName=mcbwebstorage;AccountKey=...;EndpointSuffix=core.windows.net` |
| `TokenSigner:SigningKey` | `TokenSigner__SigningKey` | string ≥ 32 caracteres aleatorios |

`BlobStorage:Container` (`archivos`) y `BlobStorage:Prefix` (`bimbo/renovacion2026/`) viven en `appsettings.json` con default; sobrescribirlos solo si cambia la nomenclatura.

---

## Recursos pendientes

- **Fuente Gojal**: colocar `gojal.woff2` / `gojal.woff` / `gojal.ttf` en `src/PolizasBimbo.Web/wwwroot/fonts/gojal/`. El CSS ya hace referencia vía `@font-face`.

---

## Deploy a Azure

- **App Service**: `programasegurovoluntario` (Windows), RG `Default-SQL-SouthCentralUS`, suscripción `a7ba4133-5d5d-475b-8c37-5efdd82471df`.
- **Hostname**: `programasegurovoluntario.azurewebsites.net`.
- **Comando** (zip deploy):
  ```powershell
  dotnet publish src/PolizasBimbo.Web/PolizasBimbo.Web.csproj -c Release -o publish
  Compress-Archive -Path publish/* -DestinationPath polizasbimbo.zip -Force
  az webapp deploy --subscription a7ba4133-5d5d-475b-8c37-5efdd82471df `
    --resource-group Default-SQL-SouthCentralUS `
    --name programasegurovoluntario `
    --src-path polizasbimbo.zip --type zip --async false
  ```

---

## Flujo funcional

```
1. Usuario → GET /                                                (form con ID colaborador / email / teléfono)
2. Usuario → POST /api/search { idColaborador, email, telefono }  (rate-limit 10/min/IP)
            Backend: valida campos, lista blobs con prefijo
                     "{Prefix}{idColaborador}_", emite un JWT por
                     archivo y persiste fila en DownloadTokens.
            ← [{ fileName, displayName, downloadToken }]
3. Usuario → GET  /d/{token}
            Backend: valida JWT + jti no consumido, abre blob
                     privado, marca consumido, registra auditoría
                     en ConsultaPolizasBimboTraspaso (NumColaborador,
                     Email, Telefono, NomArchivo, FechaCreacion),
                     stream del archivo como FileStreamResult.
```

`token` es un JWT HMAC-SHA256 con `{ jti, exp }` (TTL 10 min). El contexto del download (archivo, idColaborador, email, teléfono) vive en la fila `DownloadTokens` referenciada por `jti`. Ver ADR-001.

---

## Decisiones arquitectónicas
Ver `/docs/adr/`:
- ADR-001: Proxy de descarga con JWT opaco de un solo uso.
- ADR-002: Full-Text Index sobre NombreCompleto. *(obsoleto — la búsqueda se hace por listado en Blob Storage; ya no se consulta `PolizasBimboTraspaso`.)*
- ADR-003: Recarga del padrón con DELETE + INSERT transaccional. *(obsoleto — sin padrón SQL.)*

---

## Corrections Log

- **[2026-04-30]** No crear rutinas, schedules, jobs ni tareas automáticas sin instrucción explícita del usuario — aunque parezca útil o se proponga en el plan. Solo ejecutar lo que fue aprobado explícitamente.
