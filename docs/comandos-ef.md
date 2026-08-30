# Comandos de EF Core para el esquema

Todos los comandos se corren desde la **raíz del repo** (`TaskManager`), salvo que se indique otra cosa.

La CLI es `dotnet-ef` (herramienta local en `.config/dotnet-tools.json`). Si el comando no existe:

```powershell
dotnet tool restore
```

El proyecto de la API es `TaskManagementApi`. Las migraciones viven en `TaskManagementApi/Data/Migrations`.

---

## Preparación

| Comando | Para qué |
| --- | --- |
| `dotnet tool restore` | Instala `dotnet-ef` según `.config/dotnet-tools.json` |
| `docker compose up -d postgres` | Levanta PostgreSQL (hace falta para aplicar migraciones) |
| `dotnet build --project TaskManagementApi` | Comprueba que el modelo compile antes de generar migraciones |

---

## Crear y listar migraciones

```powershell
dotnet ef migrations add NombreDeLaMigracion --project TaskManagementApi --output-dir Data/Migrations
```

Genera el `Up`/`Down` y actualiza `ApplicationDbContextModelSnapshot.cs`. El nombre debe describir el cambio: `InitialCreate`, `AddAssignedTo`, `MakeDueDateRequired`.

```powershell
dotnet ef migrations list --project TaskManagementApi
```

Lista las migraciones del proyecto. Si hay conexión a la base, marca cuáles ya están aplicadas.

```powershell
dotnet ef migrations remove --project TaskManagementApi
```

Borra **solo la última** migración y revierte el snapshot. Úsalo si esa migración **aún no** se aplicó a una base que te importa. Si ya corrió, no la borres: crea otra migración que corrija.

---

## Aplicar y revertir en la base

En este proyecto la API ya aplica lo pendiente al arrancar (`MigrateAsync` en `Program.cs`). Estos comandos sirven para hacerlo a mano o para revertir.

```powershell
dotnet ef database update --project TaskManagementApi
```

Aplica **todas** las migraciones pendientes (equivalente a lo que hace `MigrateAsync`).

```powershell
dotnet ef database update NombreDeLaMigracion --project TaskManagementApi
```

Aplica o revierte hasta esa migración concreta. Útil para probar un `Down`.

```powershell
dotnet ef database update 0 --project TaskManagementApi
```

Revierte **todas** las migraciones. Las tablas creadas por EF desaparecen. Los datos de esas tablas se pierden.

---

## Inspeccionar

```powershell
dotnet ef dbcontext info --project TaskManagementApi
```

Muestra el contexto, el proveedor (`Npgsql`) y la cadena de conexión que usará la CLI.

```powershell
dotnet ef dbcontext list --project TaskManagementApi
```

Lista los `DbContext` del proyecto. Aquí solo hay `ApplicationDbContext`.

```powershell
dotnet ef migrations script --project TaskManagementApi
```

Genera el SQL de **todas** las migraciones (útil para revisar o para un DBA).

```powershell
dotnet ef migrations script MigracionDesde MigracionHasta --project TaskManagementApi
```

SQL solo del rango indicado.

```powershell
dotnet ef migrations script --idempotent --project TaskManagementApi
```

SQL que se puede correr más de una vez: comprueba `__EFMigrationsHistory` antes de cada cambio. Típico para aplicar en un servidor a mano.

---

## Flujo habitual al cambiar el esquema

1. Editas la entidad (`Models/`) y, si hace falta, `OnModelCreating`.
2. Compilas: `dotnet build --project TaskManagementApi`.
3. Generas: `dotnet ef migrations add DescripcionDelCambio --project TaskManagementApi --output-dir Data/Migrations`.
4. Revisas el `Up()`/`Down()` generado.
5. Aplicas: reinicias la API **o** `dotnet ef database update --project TaskManagementApi`.

Si el `add` falla, casi siempre es porque el proyecto no compila o porque no hay `DbSet` de la entidad nueva.

---

## Errores frecuentes

| Situación | Qué hacer |
| --- | --- |
| `dotnet ef` no se reconoce | `dotnet tool restore` en la raíz |
| No puede conectar a Postgres | `docker compose up -d postgres` y espera al healthcheck |
| “pending model changes” al hacer `list` / `update` | Cambiaste el modelo y no generaste migración: haz `migrations add` |
| Quieres deshacer la última y **no** está aplicada | `dotnet ef migrations remove --project TaskManagementApi` |
| Quieres deshacer la última y **sí** está aplicada | `dotnet ef database update MigracionAnterior --project TaskManagementApi` y después `migrations remove` solo si esa base es descartable |
| `EnsureCreated` vs migraciones | No uses `EnsureCreated` en este proyecto. Choca con el historial de `MigrateAsync` |

---

## Qué no hace falta en el día a día

- `dotnet ef dbcontext scaffold`: genera entidades **desde** una base existente (*database-first*). Este repo es *code-first*.
- Editar a mano `ApplicationDbContextModelSnapshot.cs`: lo mantiene la CLI.
- Crear tablas a mano en Postgres para entidades nuevas: la migración es la fuente de verdad.
