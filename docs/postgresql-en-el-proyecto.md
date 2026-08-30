# PostgreSQL en Task Management API

Este documento explica cómo quedó configurado PostgreSQL en el proyecto, qué pieza hace qué, y **cómo se crea de verdad una entidad (tabla) en la base**.

En este proyecto no se escribe el esquema a mano en SQL. Se usa **Entity Framework Core (EF Core)** en modo *code-first*: el modelo vive en C#, las **migraciones** lo traducen a SQL, y Postgres solo recibe esos cambios.

---

## Piezas involucradas

| Pieza | Dónde | Rol |
| --- | --- | --- |
| Servidor PostgreSQL 16 | `docker-compose.yml` (servicio `postgres`) | Motor de base de datos |
| Cadena de conexión | `TaskManagementApi/appsettings.json` | Host, puerto, base, usuario y contraseña |
| Paquetes NuGet | `TaskManagementApi.csproj` | ORM + proveedor de Postgres |
| Entidad | `Models/TaskItem.cs` | Forma de una fila en C# |
| DbContext | `Data/ApplicationDbContext.cs` | Mapa clases ↔ tablas |
| Migraciones | `Data/Migrations/` | Historial de cambios de esquema |
| Arranque | `Program.cs` | Registra el contexto y aplica migraciones |

---

## 1. El servidor: Docker, no una instalación local

PostgreSQL corre en un contenedor. Desde la raíz del repo:

```powershell
docker compose up -d postgres
```

Compose hace esto:

1. Baja la imagen `postgres:16`.
2. Crea el usuario `postgres`, la contraseña `postgres` y la base `task_management`.
3. Expone el puerto `5432` en `localhost` (la API local se conecta ahí).
4. Persiste los datos en el volumen `postgres_data` para que no se borren al parar el contenedor.
5. Espera a que el healthcheck (`pg_isready`) marque la base como lista.

La API **no instala** Postgres. Solo se conecta por TCP.

Si también levantas el servicio `api` de Compose, la cadena usa `Host=postgres` (nombre del servicio en la red de Docker). En desarrollo local (`dotnet watch run`) el host es `localhost`.

---

## 2. La conexión: `appsettings` + `UseNpgsql`

`appsettings.json` define la cadena que EF Core usa:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=task_management;Username=postgres;Password=postgres"
}
```

En `Program.cs` se registra el contexto:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);
```

Qué significa cada parte:

- `AddDbContext<ApplicationDbContext>`: registra el contexto en la inyección de dependencias. Cada request HTTP recibe su propia instancia (`scoped`).
- `UseNpgsql(...)`: elige el proveedor de PostgreSQL. Sin esto, EF no sabría generar SQL de Postgres (`timestamp with time zone`, `identity`, etc.).
- `GetConnectionString("DefaultConnection")`: lee la clave de configuración. En Docker se puede sobreescribir con la variable `ConnectionStrings__DefaultConnection`.

Los paquetes que lo hacen posible:

- `Microsoft.EntityFrameworkCore` — API del ORM (`DbContext`, `DbSet`, `SaveChangesAsync`).
- `Npgsql.EntityFrameworkCore.PostgreSQL` — traducción a PostgreSQL.
- `Microsoft.EntityFrameworkCore.Design` — usado por la CLI `dotnet ef` para generar migraciones.

La herramienta CLI está en `.config/dotnet-tools.json` como `dotnet-ef`. La primera vez en una máquina: `dotnet tool restore` desde la raíz del repo.

---

## 3. El modelo: entidad + DbContext

### Entidad (`TaskItem`)

Una **entidad** es una clase C# que representa **una fila** de una tabla. No es la tabla todavía.

```csharp
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

EF Core aplica convenciones:

| En C# | En PostgreSQL (si no se configura otra cosa) |
| --- | --- |
| Propiedad `Id` de tipo `int` | Primary key `integer` con identity (autoincrement) |
| `string` | `text` |
| `string?` | `text` nullable |
| `DateTime` / `DateTime?` | `timestamp with time zone` (Npgsql exige UTC) |
| Enum | Por defecto un número; en este proyecto se guarda como texto |

### DbContext (`ApplicationDbContext`)

El contexto es el mapa entre el modelo y la base:

```csharp
public DbSet<TaskItem> Tasks => Set<TaskItem>();
```

`DbSet<TaskItem> Tasks` significa: “existe un conjunto de `TaskItem` que se persiste en una tabla llamada `Tasks`”. En los servicios se usa `_db.Tasks`.

`OnModelCreating` ajusta lo que las convenciones no cubren (Fluent API):

- `Title` obligatorio y máximo 100 → `varchar(100) NOT NULL`.
- `Status` y `Priority` se guardan como string (`"Pending"`, `"High"`) con `HasConversion<string>()`, no como `0` o `1`.

Hasta aquí **solo existe el modelo en memoria**. Postgres todavía no tiene la tabla.

---

## 4. Cómo se crea una entidad en PostgreSQL (paso a paso)

Crear una entidad **no** es abrir pgAdmin y hacer `CREATE TABLE`. El camino completo es este.

### Paso 1 — Clase C#

Creas (o cambias) la clase en `Models/`, por ejemplo `TaskItem`. Eso define columnas, tipos y nulabilidad a nivel de código.

### Paso 2 — Registrarla en el DbContext

Añades un `DbSet<T>`:

```csharp
public DbSet<TaskItem> Tasks => Set<TaskItem>();
```

Sin `DbSet`, EF no incluye esa clase en el modelo y **no generará tabla**.

Si hace falta, configuras reglas extra en `OnModelCreating` (longitudes, conversiones, índices, relaciones).

### Paso 3 — Generar la migración

La CLI compara:

1. El modelo actual (`TaskItem` + `OnModelCreating`).
2. El snapshot `Data/Migrations/ApplicationDbContextModelSnapshot.cs` (última versión conocida del esquema).

Y escribe un archivo C# en `Data/Migrations/`:

```powershell
dotnet ef migrations add InitialCreate --project TaskManagementApi --output-dir Data/Migrations
```

Ese archivo **no es SQL suelto**: es C# que, al ejecutarse, emite SQL. El `Up()` de `InitialCreate` hace `CreateTable("Tasks", ...)` con cada columna. El `Down()` hace `DropTable("Tasks")` por si hay que revertir.

También actualiza el snapshot para que la próxima migración solo vea el *diff*.

Todavía **no hay tabla en Postgres**. Solo hay un plan versionado en el repo.

### Paso 4 — Aplicar la migración a la base

Al arrancar la API, `Program.cs` ejecuta:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}
```

`MigrateAsync()`:

1. Se conecta con `DefaultConnection`.
2. Crea la tabla `__EFMigrationsHistory` si no existe (historial de EF).
3. Lee qué migraciones ya corrieron.
4. Ejecuta el `Up()` de las pendientes.

Ahí es cuando PostgreSQL recibe el equivalente a:

```sql
CREATE TABLE "Tasks" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "Title" character varying(100) NOT NULL,
    "Description" text,
    "Status" character varying(20) NOT NULL,
    "Priority" character varying(20) NOT NULL,
    "DueDate" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);
```

Y registra `20260830200530_InitialCreate` en `__EFMigrationsHistory`.

También se puede aplicar a mano, sin arrancar la API: `dotnet ef database update` (ver `docs/comandos-ef.md`).

### Paso 5 — Usar la tabla desde el servicio

A partir de ahí, `TaskService` no crea esquema. Solo lee y escribe filas:

- `_db.Tasks.Add(task)` + `SaveChangesAsync()` → `INSERT` (Postgres rellena `Id`).
- Cambiar propiedades de una entidad tracked + `SaveChangesAsync()` → `UPDATE`.
- `ToListAsync()` / `FirstOrDefaultAsync()` → `SELECT`.
- `ExecuteDeleteAsync()` → `DELETE`.

`Add` **no** crea la tabla. Crea una **fila**. La tabla ya tuvo que existir por la migración.

### Resumen de “crear una entidad”

```
Clase C#  →  DbSet + Fluent API  →  dotnet ef migrations add  →  MigrateAsync / database update  →  tabla en PostgreSQL
```

Si omites el `DbSet`, no entra en el modelo.  
Si omites la migración, el snapshot y Postgres quedan desfasados.  
Si omites aplicar (`MigrateAsync` o `database update`), el archivo existe en git pero la tabla no.

---

## 5. Cómo se actualiza el esquema después

Ejemplo: agregar `AssignedTo` a las tareas.

1. Añades `public string? AssignedTo { get; set; }` en `TaskItem`.
2. Si hace falta, configuras la propiedad en `OnModelCreating`.
3. Generas otra migración: `dotnet ef migrations add AddAssignedTo`.
4. Reinicias la API (o corres `database update`). EF genera un `ALTER TABLE` y lo aplica.

Reglas prácticas:

- **No edites** una migración que ya se aplicó en una base que te importa.
- Si te equivocaste y **aún no** la aplicaste: `dotnet ef migrations remove`, corrige el modelo y genera de nuevo.
- Si ya está aplicada: crea una migración nueva que corrija el error.

`EnsureCreated()` no se usa en este proyecto. Crea el esquema de un golpe **sin** historial de migraciones y luego choca con `MigrateAsync`. El camino correcto es siempre migraciones.

---

## 6. Cómo lo usa el servicio

`TaskService` recibe `ApplicationDbContext` por constructor. ASP.NET lo resuelve porque está registrado con `AddDbContext`.

| Operación | Código | SQL aproximado |
| --- | --- | --- |
| Listar | `_db.Tasks.AsNoTracking().ToListAsync()` | `SELECT` sin tracking (solo lectura) |
| Buscar | `_db.Tasks.FirstOrDefaultAsync(x => x.Id == id)` | `SELECT ... WHERE "Id" = @id` (tracked) |
| Crear fila | `_db.Tasks.Add(task)` + `SaveChangesAsync()` | `INSERT` |
| Actualizar | Cambias el objeto tracked + `SaveChangesAsync()` | `UPDATE` |
| Borrar | `ExecuteDeleteAsync()` | `DELETE` directo |

`SaveChangesAsync()` es el commit: hasta ahí, los cambios están solo en memoria.

`AsNoTracking()` se usa en GET para no vigilar esos objetos. En UPDATE/COMPLETE no se usa: EF necesita tracking para saber qué columnas cambiar.

`ToUtc` existe porque Npgsql guarda `timestamp with time zone` y rechaza un `DateTime` sin kind UTC.

---

## 7. Flujo local de desarrollo

1. `docker compose up -d postgres`
2. `cd TaskManagementApi` y `dotnet watch run`
3. Al arrancar, `MigrateAsync` alinea el esquema.
4. Swagger: `http://localhost:5000/swagger`

No levantes el servicio `api` de Compose al mismo tiempo que `dotnet watch run`: los dos quieren el puerto `5000`.
