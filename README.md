# API de gestión de usuarios, roles y permisos

API REST construida con .NET 10 y Oracle. Implementa autenticación con JWT, administración de usuarios y un modelo de autorización basado en roles y permisos.

## Arquitectura

La solución aplica Clean Architecture para separar las reglas de negocio, los casos de uso, la infraestructura y el transporte HTTP. Las dependencias apuntan hacia el núcleo: `Domain` no depende de los demás módulos; `Application` depende de `Domain`; `Infrastructure` implementa las abstracciones de `Application`; `Api` es el punto de entrada y conecta los módulos.

```text
Module.Api
  ├── Module.Application
  │     └── Module.Domain
  └── Module.Infrastructure
        ├── Module.Application
        └── Module.Domain
```

| Proyecto | Responsabilidad |
|---|---|
| `Module.Domain` | Entidades (`User`, `Role`, `Permission`, `RefreshToken`), entidades base y constantes para roles y permisos. No contiene referencias a paquetes NuGet. |
| `Module.Application` | Casos de uso y servicios de autenticación, perfiles, usuarios, roles y catálogo de permisos; contratos/DTOs; validadores; mapeos; paginación; e interfaces que abstraen persistencia, tokens, contraseñas y caché de permisos. |
| `Module.Infrastructure` | Persistencia con Entity Framework Core y Oracle, configuración del modelo, implementaciones de hashing/tokens/permisos y siembra inicial de datos. |
| `Module.Api` | Aplicación ASP.NET Core: controladores, autenticación JWT, autorización por permisos, filtro de validación, manejo de errores, OpenAPI y configuración de inicio. |

Los controladores delegan en los servicios de `Application`. Estos usan interfaces del módulo, no dependen directamente de Oracle. `Infrastructure` proporciona las implementaciones y `Api` registra ambos módulos al iniciar la aplicación.

### Crear una biblioteca y enlazar proyectos

Desde la raíz del repositorio, crea una biblioteca .NET con `classlib` y agrégala a la solución existente:

```powershell
dotnet new classlib -n Module.Shared -o .\Module.Shared
dotnet sln .\Module.slnx add .\Module.Shared\Module.Shared.csproj
```

Para que un proyecto existente use esa biblioteca, agrega una referencia desde el proyecto consumidor:

```powershell
dotnet add .\Module.Application\Module.Application.csproj reference .\Module.Shared\Module.Shared.csproj
```

Esto registra un `ProjectReference` en `Module.Application.csproj`. Usa la misma forma para enlazarla desde otro proyecto, cambiando el primer argumento por el `.csproj` consumidor. Mantén las dependencias dirigidas hacia el núcleo: por ejemplo, `Module.Domain` no debe depender de `Module.Api` ni de `Module.Infrastructure`. Después de agregar proyectos o referencias, restaura y compila:

```powershell
dotnet restore .\Module.slnx
dotnet build .\Module.slnx
```

#### Instalar paquetes NuGet en un proyecto

Los paquetes NuGet son bibliotecas externas; se agregan al `.csproj` del proyecto que realmente los utiliza. Por ejemplo, para agregar FluentValidation a `Module.Application`:

```powershell
dotnet add .\Module.Application\Module.Application.csproj package FluentValidation
```

Para solicitar una versión específica, agrega `--version`:

```powershell
dotnet add .\Module.Application\Module.Application.csproj package FluentValidation --version 11.11.0
```

Si el paquete solo se necesita en otra biblioteca, cambia la ruta del `.csproj` por la de ese proyecto. No agregues un paquete a todos los proyectos automáticamente; colócalo donde se usa y evita dependencias de infraestructura en `Module.Domain`. Después de instalarlo, restaura y compila la solución:

```powershell
dotnet restore .\Module.slnx
dotnet build .\Module.slnx
```

## Reglas de negocio

### Autenticación y cuentas

- El registro público crea cuentas con el rol activo `ADMIN`; no permite seleccionar otro rol.
- El email y el nombre de usuario se normalizan quitando espacios externos y convirtiéndolos a minúsculas. Ambos deben ser únicos entre registros activos.
- El nombre de usuario tiene entre 3 y 50 caracteres y admite letras, números, `_`, `.` y `-`. El email debe ser válido y tener como máximo 255 caracteres.
- Las contraseñas deben tener entre 8 y 72 caracteres e incluir al menos una mayúscula, una minúscula y un número. Se almacenan como hash BCrypt, nunca como texto sin procesar.
- Las cuentas bloqueadas y las cuentas cuyo rol está inactivo no pueden autenticarse.
- El token de acceso JWT dura 15 minutos por defecto. El refresh token dura 7 días por defecto, se guarda en la base de datos como hash SHA-256 y se rota al utilizarlo.
- Los refresh tokens pertenecen a una familia. Cerrar sesión revoca la familia; reutilizar un token usado o revocado también revoca los tokens activos de esa familia.
- Cambiar la contraseña revoca todas las sesiones de la cuenta. Para el super admin, el cambio se realiza mediante la variable `SUPERADMIN__PASSWORD`.
- Los endpoints de autenticación aplican un límite de 10 solicitudes por minuto.

### Roles y permisos

- Cada permiso se identifica por una clave, por ejemplo `users.read` o `roles.manage`. Los permisos conocidos se mantienen en el catálogo del código y se sincronizan con la base de datos al iniciar.
- `SUPER_ADMIN` es un rol reservado, activo y protegido. Recibe todos los permisos conocidos, incluidos los agregados al catálogo posteriormente. No se puede modificar ni eliminar y no se puede asignar desde la API.
- `ADMIN` es el rol predeterminado para el registro. Se crea sin permisos; el super admin puede asignarle permisos, pero no puede renombrarse ni eliminarse.
- Los demás roles pueden crearse, activarse/desactivarse y recibir un conjunto completo de permisos mediante `PUT /api/roles/{id}/permissions`.
- No se puede eliminar un rol que tenga usuarios asignados. Tampoco se puede crear otro rol con el nombre reservado `SUPER_ADMIN`.
- La autorización de los endpoints protegidos consulta los permisos asociados al rol del usuario. Los permisos se almacenan en caché durante un máximo de dos minutos; los cambios de roles, permisos o usuarios invalidan esa caché.

### Administración y persistencia

- Un usuario no puede bloquearse, eliminarse ni cambiarse su propio rol.
- La cuenta super admin no puede bloquearse, eliminarse ni cambiar de rol, nombre de usuario o email. Puede actualizar su nombre y apellido.
- El borrado de usuarios y roles es lógico: se registra `DeletedAt` y las consultas normales excluyen los registros eliminados.
- Bloquear o eliminar un usuario revoca sus refresh tokens activos.
- Las respuestas de la API serializan las fechas en UTC con formato ISO 8601 y sufijo `Z`.
- Los errores conocidos y de validación se devuelven como `ProblemDetails`/`ValidationProblemDetails`. Los errores inesperados se registran y se responden como error interno.

## Paquetes y tecnologías

Las versiones indicadas son las declaradas en los archivos `.csproj` (`*` representa una versión comodín que resuelve NuGet al restaurar). La columna **Biblioteca** identifica el módulo/proyecto en cuyo `.csproj` está declarado cada paquete.

| Paquete NuGet | Versión | Biblioteca (proyecto) | Uso |
|---|---:|---|---|
| `DotNetEnv` | `3.*` | `Module.Api` | Lee `.env` sin reemplazar variables existentes y busca el archivo en directorios superiores. |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.*` | `Module.Api` | Valida JWT bearer en las solicitudes HTTP. |
| `Microsoft.AspNetCore.OpenApi` | `10.*` | `Module.Api` | Expone el documento OpenAPI en desarrollo. |
| `Microsoft.EntityFrameworkCore.Design` | `10.*` | `Module.Api` | Herramientas de diseño y migraciones de EF Core; dependencia privada de desarrollo (`PrivateAssets=all`). |
| `FluentValidation` | `11.*` | `Module.Application` | Define reglas de validación para los contratos de entrada. |
| `FluentValidation.DependencyInjectionExtensions` | `11.*` | `Module.Application` | Registra validadores en la inyección de dependencias. |
| `Microsoft.EntityFrameworkCore` | `10.*` | `Module.Application` | Tipos y operaciones de consulta usados por los casos de uso. |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.*` | `Module.Application` | Abstracciones para registrar servicios de aplicación. |
| `Oracle.EntityFrameworkCore` | `10.*` | `Module.Infrastructure` | Proveedor de EF Core para Oracle; configurado con compatibilidad SQL de Oracle 19. |
| `EFCore.NamingConventions` | `10.*` | `Module.Infrastructure` | Configura nombres de tablas y columnas en mayúsculas con guiones bajos. |
| `BCrypt.Net-Next` | `4.0.3` | `Module.Infrastructure` | Genera y verifica hashes de contraseñas. |
| `System.IdentityModel.Tokens.Jwt` | `8.*` | `Module.Infrastructure` | Crea y serializa tokens JWT. |

`Module.Domain` no declara paquetes NuGet. Además de los paquetes listados, `Module.Infrastructure` referencia el framework compartido `Microsoft.AspNetCore.App`; no es una dependencia NuGet del proyecto.

## Configuración

Al arrancar, la API carga el archivo `.env` si existe; las variables de entorno del proceso tienen prioridad. Para seguir la convención habitual de variables de entorno, se usan nombres en mayúsculas. En .NET, `__` representa `:` y separa las secciones de configuración: `CONNECTIONSTRINGS__DEFAULT` corresponde a `ConnectionStrings:Default`. Los nombres de configuración no distinguen mayúsculas de minúsculas.

| Variable | Requerida | Descripción |
|---|---|---|
| `CONNECTIONSTRINGS__DEFAULT` | Sí | Connection string de Oracle. |
| `JWT__ISSUER` | Sí | Emisor que se valida en el JWT. |
| `JWT__AUDIENCE` | Sí | Audiencia que se valida en el JWT. |
| `JWT__KEY` | Sí | Clave de firma JWT de al menos 32 caracteres. Debe mantenerse secreta. |
| `JWT__ACCESSTOKENMINUTES` | No | Duración del token de acceso; por defecto 15 minutos, rango permitido de 1 a 1440. |
| `JWT__REFRESHTOKENDAYS` | No | Duración del refresh token; por defecto 7 días, rango permitido de 1 a 90. |
| `SUPERADMIN__EMAIL` | Sí | Email de inicio de sesión de la cuenta super admin. |
| `SUPERADMIN__PASSWORD` | Sí | Contraseña inicial y fuente de sincronización de la contraseña del super admin. |
| `SUPERADMIN__USERNAME` | No | Nombre de usuario inicial; por defecto `superadmin`. |
| `SUPERADMIN__NAME` | No | Nombre inicial; por defecto `Super`. |
| `SUPERADMIN__SURNAME` | No | Apellido inicial; por defecto `Admin`. |
| `DATABASE__MIGRATEONSTARTUP` | No | Permite aplicar migraciones al iniciar fuera de Development si es `true`. |

La aplicación valida la configuración JWT y las credenciales del super admin al iniciar. Si faltan el email o la contraseña del super admin, el inicio falla indicando la configuración requerida. No guardes secretos reales en el repositorio; usa variables de entorno o un gestor de secretos como Azure Key Vault.

### Configurar secretos de usuario para desarrollo local

En lugar de guardar credenciales en `.env` o en archivos versionados, puedes usar User Secrets de .NET. Desde la raíz del repositorio, inicializa el almacén para `Module.Api` y establece los valores necesarios:

```powershell
dotnet user-secrets init --project .\Module.Api

dotnet user-secrets set "ConnectionStrings:Default" "User Id=USUARIO;Password=CONTRASENA;Data Source=localhost:1521/FREEPDB1" --project .\Module.Api
dotnet user-secrets set "Jwt:Issuer" "rbac-api" --project .\Module.Api
dotnet user-secrets set "Jwt:Audience" "rbac-clients" --project .\Module.Api
dotnet user-secrets set "Jwt:Key" "REEMPLAZA_POR_UNA_CLAVE_ALEATORIA_DE_AL_MENOS_32_CARACTERES" --project .\Module.Api

dotnet user-secrets set "SuperAdmin:Email" "admin@example.com" --project .\Module.Api
dotnet user-secrets set "SuperAdmin:Password" "REEMPLAZA_POR_UNA_CONTRASENA_SEGURA" --project .\Module.Api
dotnet user-secrets set "SuperAdmin:Username" "superadmin" --project .\Module.Api
dotnet user-secrets set "SuperAdmin:Name" "Super" --project .\Module.Api
dotnet user-secrets set "SuperAdmin:Surname" "Admin" --project .\Module.Api
```

Reemplaza los valores de ejemplo por los de tu entorno. User Secrets se almacena fuera del repositorio y se carga en el entorno `Development`; las variables de entorno del proceso tienen prioridad si una clave está definida en ambos lugares. No publiques credenciales ni claves reales en el README, `.env` u otros archivos versionados.

### Establecer variables de entorno

La convención del proyecto es usar nombres en mayúsculas y `__` únicamente para separar secciones de .NET (por ejemplo, `JWT__ISSUER`). No agregues guiones bajos para separar palabras dentro de una propiedad, porque cambiaría la clave que recibe el binder de configuración. El archivo `.env.example` es la plantilla canónica y contiene valores ficticios. Para desarrollo local, cópialo a `.env` en la raíz del repositorio y edita los valores:

```powershell
Copy-Item .env.example .env
notepad .env
```

Configura, como mínimo, `CONNECTIONSTRINGS__DEFAULT`, `JWT__ISSUER`, `JWT__AUDIENCE`, `JWT__KEY`, `SUPERADMIN__EMAIL` y `SUPERADMIN__PASSWORD`. El formato del archivo es `NOMBRE=valor`; para una connection string o contraseña que incluya espacios o `;`, encierra el valor entre comillas simples.

```dotenv
CONNECTIONSTRINGS__DEFAULT='User Id=USUARIO;Password=CONTRASENA;Data Source=localhost:1521/FREEPDB1'
JWT__ISSUER=Module.Api
JWT__AUDIENCE=Module.Api
JWT__KEY=REEMPLAZA_POR_UN_SECRETO_ALEATORIO_DE_AL_MENOS_32_CARACTERES
SUPERADMIN__EMAIL=admin@example.com
SUPERADMIN__PASSWORD='ReemplazaEsta123'
SUPERADMIN__USERNAME=superadmin
SUPERADMIN__NAME=Super
SUPERADMIN__SURNAME=Admin
```

También puedes establecerlas solo para la sesión actual de PowerShell. Los procesos iniciados desde esa ventana, incluido `dotnet run`, heredarán esos valores:

```powershell
$env:CONNECTIONSTRINGS__DEFAULT = 'User Id=USUARIO;Password=CONTRASENA;Data Source=localhost:1521/FREEPDB1'
$env:JWT__ISSUER = 'Module.Api'
$env:JWT__AUDIENCE = 'Module.Api'
$env:JWT__KEY = 'REEMPLAZA_POR_UN_SECRETO_ALEATORIO_DE_AL_MENOS_32_CARACTERES'
$env:SUPERADMIN__EMAIL = 'admin@example.com'
$env:SUPERADMIN__PASSWORD = 'ReemplazaEsta123'
dotnet run --project .\Module.Api
```

Para persistir una variable para tu usuario de Windows, puedes usar `[Environment]::SetEnvironmentVariable('JWT__ISSUER', 'Module.Api', 'User')`; repite el comando para cada variable y abre una nueva terminal para que los cambios estén disponibles. Para despliegues, configura estas mismas variables en el mecanismo del hosting o gestor de secretos. No subas `.env` ni persistas secretos en equipos compartidos. Las variables del proceso tienen prioridad sobre los valores del archivo `.env`.

## Inicializar la solución y ejecutar el backend

Requisitos: SDK de .NET 10, una instancia de Oracle accesible y la configuración local completada (por ejemplo, mediante User Secrets). Ejecuta los comandos desde la raíz del repositorio. La solución `Module.slnx` y sus proyectos ya están incluidos; no necesitas crear una solución nueva.

Restaura las dependencias y compila todos los proyectos:

```powershell
dotnet restore .\Module.slnx
dotnet build .\Module.slnx
```

### Crear y aplicar migraciones de Entity Framework Core

Instala la herramienta `dotnet-ef` si todavía no está instalada:

```powershell
dotnet tool install --global dotnet-ef
```

Si el proyecto todavía no tiene migraciones y vas a inicializar una base de datos nueva, crea la migración inicial y aplícala a Oracle:

```powershell
dotnet ef migrations add Initial --project .\Module.Infrastructure --startup-project .\Module.Api --output-dir Persistence\Migrations
dotnet ef database update --project .\Module.Infrastructure --startup-project .\Module.Api
```

La migración `Initial` se crea una sola vez. Cuando posteriormente cambies el modelo de datos, crea una migración con un nombre descriptivo y aplica las migraciones pendientes:

```powershell
dotnet ef migrations add NombreDelCambio --project .\Module.Infrastructure --startup-project .\Module.Api --output-dir Persistence\Migrations
dotnet ef database update --project .\Module.Infrastructure --startup-project .\Module.Api
```

No vuelvas a crear `Initial` para actualizar el esquema. Antes de aplicar una migración a una base con datos existentes, revisa los cambios que generó y respalda los datos. En `Development`, el backend también aplica las migraciones automáticamente al arrancar y sincroniza permisos, el rol `SUPER_ADMIN`, la cuenta configurada y el rol `ADMIN`. En otros entornos, configura `DATABASE__MIGRATEONSTARTUP=true` para aplicarlas durante el inicio, o ejecuta `dotnet ef database update` explícitamente.

### Arrancar el backend

Con Oracle disponible y las variables configuradas, inicia la API desde la raíz:

```powershell
dotnet run --project .\Module.Api --urls "https://localhost:7001;http://localhost:5000"
```

El backend queda disponible en `https://localhost:7001` y `http://localhost:5000`. Para detenerlo, presiona `Ctrl+C` en la terminal.

## Cuenta super admin

Al iniciar, el seeder garantiza que exista el rol `SUPER_ADMIN` y que tenga todos los permisos del catálogo. Busca al usuario por el email configurado en `SUPERADMIN__EMAIL`. Si no existe, crea la cuenta; si existe, la reasigna al rol super admin, la desbloquea y sincroniza la contraseña con `SUPERADMIN__PASSWORD`. Si cambia la contraseña, revoca sus refresh tokens activos. Un conflicto de nombre de usuario al crear la cuenta impide el inicio y debe resolverse cambiando `SUPERADMIN__USERNAME` o liberando ese nombre.

## API

Los listados usan paginación indexada desde cero (`page`, predeterminado 0; `size`, predeterminado 30, máximo 100), búsqueda (`search`) y orden (`sort`, por ejemplo `sort=name,desc`) cuando aplique. Los resultados paginados tienen la forma `data` y `pagination`.

| Método | Ruta | Acceso |
|---|---|---|
| `POST` | `/api/auth/login` | Público; limitado por tasa |
| `POST` | `/api/auth/signup` | Público; limitado por tasa |
| `POST` | `/api/auth/refresh` | Público; limitado por tasa |
| `POST` | `/api/auth/logout` | Público; limitado por tasa |
| `GET`, `PUT` | `/api/auth/profile` | JWT válido |
| `PATCH` | `/api/auth/password` | JWT válido |
| `GET` | `/api/users`, `/api/users/{id}` | Permiso `users.read` |
| `POST`, `PUT`, `DELETE` | `/api/users`, `/api/users/{id}` | Permiso `users.manage` |
| `PATCH` | `/api/users/{id}/restore` | Permiso `users.manage` |
| `GET` | `/api/roles`, `/api/roles/{id}` | Permiso `roles.read` |
| `POST`, `PUT`, `DELETE` | `/api/roles`, `/api/roles/{id}` | Permiso `roles.manage` |
| `PATCH` | `/api/roles/{id}/restore` | Permiso `roles.manage` |
| `PUT` | `/api/roles/{id}/permissions` | Permiso `roles.manage` |
| `GET`, `PUT` | `/api/permissions`, `/api/permissions/{id}` | Permiso `permissions.read` para lectura; `roles.manage` para edición |

Los filtros disponibles incluyen `role` (ID de rol), `blocked` y `deleted` para usuarios, y `active` y `deleted` para roles. Para crear o actualizar un usuario, el rol se envía como `{ "role": { "id": 2 } }`; las listas de permisos en roles usan objetos `{ "id": ... }`. El inicio de sesión acepta un nombre de usuario o un email en el campo `username`. En desarrollo, OpenAPI está disponible en `/openapi/v1.json`. El archivo `Module.Api/Module.Api.http` contiene solicitudes de ejemplo para probar los endpoints.

## Migraciones y datos existentes

Las migraciones se generan en `Module.Infrastructure/Persistence/Migrations`. Si ya existe una base creada con un esquema incompatible con el modelo actual, respalda los datos antes de recrearla o aplica una migración de actualización. No ejecutes comandos destructivos como `DROP USER ... CASCADE` en una base con datos que deban conservarse.

## Producción

- Usa una clave JWT fuerte y única y credenciales del super admin fuera de los archivos versionados.
- Configura HTTPS, una instancia de Oracle administrada y políticas de copia de seguridad y acceso adecuadas.
- Revisa las migraciones antes de aplicarlas; fuera de Development el inicio no migra automáticamente salvo que `DATABASE__MIGRATEONSTARTUP=true`.
- La limitación actual de autenticación es por proceso y ventana fija, no sustituye una política de rate limiting distribuida cuando se ejecutan varias instancias.
