# MiPokemonApp

Una aplicación web .NET MVC que muestra información de Pokémon usando la PokéAPI, con filtros, paginación, exportación a Excel y funcionalidad de correo electrónico.

## Empezando

### Prerrequisitos

- .NET SDK 8 o 9
- Git

### Instalación

1. Clonar el repositorio (desde tu máquina local o GitHub):

   ```bash
   git clone https://github.com/tuUsuario/MiPokemonApp.git
   cd MiPokemonApp
   ```

2. Restaurar paquetes y compilar:

   ```bash
   dotnet restore
   dotnet build
   ```

3. Ejecutar la aplicación:

   ```bash
   dotnet run
   ```

   Verás en consola algo como:

   ```
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: http://localhost:7183
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: https://localhost:7184
   ```

4. Abrir el navegador en:

   ```
   http://localhost:7183/Pokemon/Index
   ```

   (Ajústalo al puerto que indique tu salida de `dotnet run`).

## Estructura del Proyecto

```
MiPokemonApp
├─ Controllers
│    └─ PokemonController.cs
├─ Models
│    ├─ PokeApi
│    │    ├─ PokemonListResponse.cs
│    │    ├─ PokemonBasicInfo.cs
│    │    └─ PokemonSpeciesResponse.cs
│    │
│    ├─ ViewModels
│    │    ├─ PokemonGridItemViewModel.cs
│    │    ├─ PokemonFilterViewModel.cs
│    │    └─ PokemonDetailViewModel.cs
│    │
│    ├─ Excel
│    │    └─ PokemonExcelRow.cs
│    │
│    └─ Email
│         └─ EmailSettings.cs
│
├─ Services
│    ├─ Interfaces
│    │    ├─ IPokeApiService.cs
│    │    ├─ IExcelService.cs
│    │    └─ IEmailService.cs
│    │
│    └─ Implementations
│         ├─ SpeciesCache.cs
│         ├─ PokeApiService.cs
│         ├─ EmailService.cs
│         └─ ExcelService.cs
│
├─ Views
│    ├─ Pokemon
│    │    ├─ Index.cshtml
│    │    └─ _PokemonDetailPartial.cshtml
│    │
│    └─ Shared
│         └─ _Layout.cshtml
│
├─ wwwroot
│    ├─ css
│    └─ js
│
├─ appsettings.json
├─ Program.cs
├─ MiPokemonApp.csproj
└─ README.md
```

## Configuración

### appsettings.json

Ejemplo mínimo (solo para EmailSettings; si no usarás SMTP real, puedes dejar valores vacíos o de ejemplo):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "EmailSettings": {
    "SmtpHost": "smtp.tu-servidor.com",
    "SmtpPort": 587,
    "FromAddress": "no-reply@tudominio.com",
    "FromName": "Pokémon App",
    "UserName": "usuario_smtp",
    "Password": "tu_password",
    "EnableSsl": true
  }
}
```

**Nota:** Para no exponer credenciales en texto plano, considera usar `appsettings.Development.json` (en `.gitignore`) o variables de entorno.

### Paquetes NuGet

Verifica que tu `.csproj` incluya:

```xml
<ItemGroup>
  <PackageReference Include="ClosedXML" Version="0.104.0" />
  <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />
</ItemGroup>
```

Para instalar desde consola:

```bash
dotnet add package ClosedXML
dotnet add package Microsoft.Extensions.Caching.Memory
```

## Funcionalidades

### 1. Listado de Pokémon
- Llama a `IPokeApiService.GetPokemonListAsync(offset, limit)` con paginación
- Muestra imagen, nombre, especie (obtenida vía `GetSpeciesNameAsync`) y botones para detalle y correo

### 2. Filtros
- Por nombre (texto)
- Por especie (dropdown con lista de `GetAllSpeciesNamesAsync()`)

### 3. Paginación Manual
- Páginas de 20 ítems: `offset = (page - 1) * PageSize`
- Visualiza enlaces numerados para navegar

### 4. Modal de Detalle
- Al hacer clic en "Detalle", se utiliza `fetch` para cargar `_PokemonDetailPartial.cshtml` y mostrar en modal

### 5. Exportación a Excel
- Se serializa en JSON las filas visibles (`PokemonExcelRow`) y envía POST a `ExportToExcel`
- `ExcelService` genera un archivo .xlsx en memoria y lo descarga

### 6. Envío de Correos (simulado)
- `EmailService` imprime en consola destinatario, asunto y cuerpo; no envía correos reales

### 7. Caching de Especies
- `SpeciesCache` almacena en memoria el mapeo `{ ID → Nombre }` por 12 horas para `/pokemon-species/{id}`

## Notas Adicionales

### Entorno de Desarrollo
Se probó con .NET SDK 8 y 9. Para usar .NET 9, modifica en `MiPokemonApp.csproj`:

```xml
<TargetFramework>net9.0</TargetFramework>
```

### Manejo de SMTP
Si usas un servidor real, NO guardes credenciales en `appsettings.json` del repo.

**Opción 1:** `appsettings.Development.json` (en `.gitignore`)

**Opción 2:** Variables de entorno:

```powershell
$Env:SMTP__Host = "smtp.mi-servidor.com"
$Env:SMTP__Port = "587"
$Env:SMTP__FromAddress = "no-reply@midominio.com"
$Env:SMTP__FromName = "MiPokemonApp"
$Env:SMTP__UserName = "usuario_smtp"
$Env:SMTP__Password = "mi_password_secreto"
$Env:SMTP__EnableSsl = "true"
```

### Optimización de Carga de Especies
La llamada a `GetAllSpeciesNamesAsync()` itera todas las páginas de `/pokemon-species`; puede tardar varios segundos. Para mejorar:

1. Cachear la lista completa en memoria al primer acceso
2. Precargar especies en un `IHostedService`
3. Limitar a las primeras N especies si solo te interesan

### Mejoras Futuras
- Validar entrada en filtros (sanitizar `nameFilter`, `speciesFilter`)
- Agregar paginación "Prev/Next"
- Mejorar UI con Bootstrap o Tailwind
- Implementar pruebas unitarias para servicios y controlador

### Rutas Personalizadas
Por defecto, se usa `{controller=Pokemon}/{action=Index}/{id?}`. Para cambiar, edita en `Program.cs`.

Ejemplo, para que la raíz sea `/`:

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

Renombra `PokemonController` a `HomeController` y ajusta las vistas a `Views/Home`.

## Contacto

- **Autor / Mantenedor**: Angel Daniel Doria Moncada (Junior Front End – React, .NET MVC)
- **Repositorio GitHub**: [https://github.com/tuUsuario/MiPokemonApp](https://github.com/tuUsuario/MiPokemonApp)

Para dudas o sugerencias, abre un Issue o Pull Request en el repositorio.

---

¡Gracias por revisar **MiPokemonApp**!
Este proyecto puede usarse como referencia en tu portafolio Fullstack JR.