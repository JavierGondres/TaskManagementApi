# Task Management API

API REST de gestión de tareas construida con **ASP.NET Core 10**, **Entity Framework Core** y **PostgreSQL**. Cada usuario registra su cuenta, obtiene un JWT y solo puede ver y modificar **sus** tareas.

El objetivo del repo es demostrar un backend .NET realista: controllers, DI, EF Core (code-first), autenticación JWT, validación, errores consistentes, Docker y Swagger.

---

## Stack

| Pieza | Tecnología |
| --- | --- |
| Runtime | .NET 10 / C# |
| API | ASP.NET Core Web API (controllers) |
| ORM | Entity Framework Core (Npgsql) |
| Base de datos | PostgreSQL 16 |
| Auth | JWT Bearer + hash de contraseñas (`PasswordHasher`) |
| Docs | Swagger / OpenAPI |
| Contenedores | Docker Compose |

---

## Qué hace

- Registro y login con JWT
- CRUD de tareas + marcar como completada
- Filtros (`status`, `priority`) y paginación
- Aislamiento por usuario: otra cuenta no ve tus tareas
- Respuestas de error uniformes: `{ "message", "statusCode" }`
- Migraciones aplicadas al arrancar

---

## Arquitectura

```text
HTTP  →  ExceptionMiddleware
      →  JWT (Authentication / Authorization)
      →  Controller
      →  Service
      →  EF Core (DbContext)
      →  PostgreSQL
```

Un usuario tiene muchas tareas. El `UserId` sale del claim del token, no del body.

---

## Requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (solo si corres la API fuera de Docker)

---

## Cómo levantarlo

### Opción A — Todo con Docker (recomendada para probar el repo)

Desde la raíz del repositorio:

```powershell
docker compose up --build
```

Esto levanta PostgreSQL y la API. Cuando esté listo:

- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger

Para parar: `docker compose down`. Los datos de Postgres se conservan en el volumen `postgres_data`.

### Opción B — API en local + Postgres en Docker

Útil si estás desarrollando con `dotnet watch`.

```powershell
docker compose up -d postgres
cd TaskManagementApi
dotnet run --launch-profile http
```

La API escucha en http://localhost:5000 y se conecta a `localhost:5432`. Al arrancar aplica las migraciones pendientes.

Si el puerto 5000 está ocupado, cierra la instancia anterior o cambia el perfil en `Properties/launchSettings.json`.

---

## Probarlo en Swagger

Abre http://localhost:5000/swagger. Flujo recomendado:

1. **`POST /api/auth/register`**

```json
{
  "name": "Javier",
  "email": "javier@example.com",
  "password": "Password1"
}
```

La respuesta incluye `token` y `user` (nunca el hash de la contraseña).

2. Pulsa **Authorize**, pega **solo** el JWT (Swagger añade `Bearer`) y confirma.
3. **`GET /api/users/me`** — debe devolver tu perfil.
4. **`POST /api/tasks`** — crea una tarea:

```json
{
  "title": "Preparar demo de Swagger",
  "description": "Register, Authorize y CRUD",
  "priority": "High"
}
```

5. **`GET /api/tasks`** — lista paginada. Prueba también:

```text
/api/tasks?status=Pending&priority=High&page=1&pageSize=10
```

6. **`PATCH /api/tasks/{id}/complete`**, **`PUT`** y **`DELETE`** sobre el `id` que devolvió el create.

Sin token, las rutas de tareas responden `401`. Si registras un segundo usuario, no verá las tareas del primero (`404` al pedir un id ajeno).

---

## Endpoints

### Auth (públicos)

| Método | Ruta | Descripción |
| --- | --- | --- |
| `POST` | `/api/auth/register` | Crea usuario y devuelve JWT |
| `POST` | `/api/auth/login` | Verifica credenciales y devuelve JWT |

### Usuario (JWT)

| Método | Ruta | Descripción |
| --- | --- | --- |
| `GET` | `/api/users/me` | Perfil del token actual |

### Tareas (JWT)

| Método | Ruta | Descripción |
| --- | --- | --- |
| `GET` | `/api/tasks` | Lista filtrada y paginada |
| `GET` | `/api/tasks/{id}` | Detalle (solo si es tuya) |
| `POST` | `/api/tasks` | Crea (el dueño es el usuario del token) |
| `PUT` | `/api/tasks/{id}` | Reemplaza título, estado, prioridad, fecha |
| `PATCH` | `/api/tasks/{id}/complete` | Marca `Completed` |
| `DELETE` | `/api/tasks/{id}` | Elimina (`204`) |

Query de listado:

| Parámetro | Valores | Default |
| --- | --- | --- |
| `status` | `Pending`, `InProgress`, `Completed` | todos |
| `priority` | `Low`, `Medium`, `High` | todas |
| `page` | ≥ 1 | `1` |
| `pageSize` | 1–50 | `10` |

Ejemplo de listado:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 10,
  "totalCount": 0,
  "totalPages": 0
}
```

### Errores

Todas las fallas de negocio, validación y JWT usan el mismo contrato:

```json
{
  "message": "Task not found",
  "statusCode": 404
}
```

| Código | Ejemplo |
| --- | --- |
| `400` | Título vacío, password &lt; 8 caracteres |
| `401` | Sin token, token inválido, login incorrecto |
| `404` | Tarea inexistente o de otro usuario |
| `409` | Email ya registrado |
| `500` | Error inesperado |

---

## Estructura

```text
TaskManager/
├── docker-compose.yml          # API + PostgreSQL
├── docs/                       # Notas de EF y Postgres
└── TaskManagementApi/
    ├── Controllers/
    ├── Data/                   # DbContext y migraciones
    ├── DTOs/
    ├── Middleware/             # Errores globales
    ├── Models/
    ├── Services/
    ├── Dockerfile
    └── Program.cs
```

Detalle de la base y migraciones: [docs/postgresql-en-el-proyecto.md](docs/postgresql-en-el-proyecto.md) y [docs/comandos-ef.md](docs/comandos-ef.md).

---

## Configuración

Credenciales de desarrollo (no usar en producción):

| Variable | Valor local |
| --- | --- |
| Postgres | `postgres` / `postgres`, base `task_management` |
| JWT | `Jwt` en `appsettings.json` o `Jwt__*` en Compose |

En un deploy real la clave JWT y la cadena de conexión deben ir en variables de entorno, no en el repo. Compose ya muestra el patrón `ConnectionStrings__DefaultConnection` y `Jwt__Key`.

El tiempo de vida del token está en `Jwt:ExpirationMinutes`. Si Swagger empieza a devolver `401` tras un rato, vuelve a hacer login y pega el token nuevo en **Authorize**.
