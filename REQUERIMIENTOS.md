# REQUERIMIENTOS — Migración y Renovación
## PolizasBimbo2025 → ASP.NET Core Razor

---

## Contexto

Sistema web para que clientes externos de Bimbo descarguen sus pólizas de seguro.
La versión actual está en C# Framework con MVC simulado en producción en:
https://polizasbimbo2025.mcb.uno/

Se requiere migrar a **ASP.NET Core con Razor Pages** para la renovación de vigencia 2026,
con los nuevos archivos de pólizas almacenados en **Azure Blob Storage** (privado, como Red Hospitalaria).

---

## Lo que hace el sistema actual

1. El cliente ingresa su **teléfono, correo y nombre**
2. El sistema busca el nombre en SQL Server — tabla con clientes y sus archivos relacionados
3. Si encuentra el cliente, "pinta" en el DOM los documentos disponibles para descarga
4. El cliente descarga sus pólizas

**No usa autenticación Google** — es para clientes externos, no colaboradores internos.

---

## Diferencias clave con Red Hospitalaria

| Aspecto | Red Hospitalaria | PolizasBimbo |
|---|---|---|
| Usuarios | Colaboradores internos | Clientes externos de Bimbo |
| Autenticación | Google OAuth (dominio mcbrokers) | Teléfono + correo + nombre |
| Acceso a archivos | Proxy con JWT | Por definir en migración |
| Stack actual | Java 21 Spring Boot | C# Framework MVC |
| Stack objetivo | — | ASP.NET Core Razor |

---

## Flujo de autenticación del cliente

```
Cliente ingresa: teléfono + correo + nombre
        ↓
Sistema busca en SQL Server por nombre
        ↓
Si existe → muestra documentos disponibles
Si no existe → mensaje de error amigable
        ↓
Cliente hace clic en documento
        ↓
Sistema descarga de Azure Blob (privado) y lo entrega al cliente
```

---

## Estructura de datos en SQL Server

Por diagnosticar en el código actual. Se espera encontrar:
- Tabla de clientes (nombre, teléfono, correo)
- Tabla o columnas de archivos relacionados (path o nombre del archivo)

---

## Almacenamiento de archivos

- **Nueva vigencia**: Azure Blob Storage (privado) — misma cuenta `mcbwebstorage`
- **Contenedor**: por definir (probablemente `polizas` o similar)
- **Acceso**: mediante proxy autenticado — NO URLs públicas directas
  (aplicar el mismo patrón de token de un solo uso que Red Hospitalaria si aplica,
  o SAS token de corta duración dado que son clientes externos)

---

## Stack objetivo

- **Framework**: ASP.NET Core 8 (.NET 8 o .NET 10)
- **UI**: Razor Pages
- **Base de datos**: SQL Server (mismas tablas, sin migración de datos)
- **Almacenamiento**: Azure Blob Storage SDK para .NET
- **Autenticación**: propia — validación por nombre en SQL Server
- **Despliegue**: Azure App Service (mismo patrón que HerramientasPDF) o Mochahost IIS

---

## Lo que el agente debe hacer

### PASO 1 — Diagnóstico del sistema actual
Leer todo el proyecto en `C:\Proyectos\IA\Repositorio\proyectos\PolizasBimbo2025` y reportar:

1. **Estructura actual**: ¿cómo está organizado el código C# Framework?
2. **Tablas de SQL Server**: ¿qué tablas usa, qué columnas, qué relaciones?
3. **Lógica de búsqueda**: ¿cómo busca al cliente y trae sus archivos?
4. **Cómo se muestran los documentos**: ¿URLs directas, descarga desde servidor?
5. **Calidad del código**: tests, SOLID, Clean Code
6. **Deuda técnica**: credenciales expuestas, dependencias obsoletas

### PASO 2 — Plan de migración
Con base en el diagnóstico, proponer:

1. Arquitectura de la nueva versión en ASP.NET Core Razor
2. Estructura de carpetas y archivos
3. Cómo manejar el acceso a Azure Blob (privado)
4. Qué reutilizar del código actual vs qué reescribir
5. Estimación de complejidad

### PASO 3 — Aprobación antes de implementar
Presentar el plan completo para aprobación antes de escribir una sola línea de código nuevo.

---

## Restricciones importantes

- Los clientes son **externos** — no tienen cuenta Google ni acceso a sistemas internos
- La nueva versión debe ser **simple** — el cliente solo busca y descarga, sin registro
- Las URLs de Azure Blob **nunca deben ser públicas** — usar proxy o SAS token
- Las credenciales de SQL Server y Azure **nunca en el código** — variables de entorno
- El sistema actual sigue en producción durante la migración — no romper nada

---

## Preguntas pendientes para aclarar con el equipo

1. ¿El nombre del cliente debe coincidir exactamente o permite búsqueda parcial/fuzzy?
2. ¿Hay validación adicional con el teléfono o correo, o solo con el nombre?
3. ¿Los archivos de nueva vigencia ya están en Azure Blob o se subirán después?
4. ¿El contenedor de Azure para pólizas ya existe o hay que crearlo?
5. ¿El dominio polizasbimbo2025.mcb.uno se mantiene o cambia para 2026?