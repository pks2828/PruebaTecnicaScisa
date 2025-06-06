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

2. Configurar variables de entorno:

   ```bash
   # Renombrar el archivo de configuración de ejemplo
   mv appsettings.example.json appsettings.json
   ```
   
   Editar `appsettings.json` y configurar las variables SMTP:
   ```json
   {
     "EmailSettings": {
       "SmtpServer": "tu-servidor-smtp.com",
       "SmtpPort": 587,
       "SmtpUsername": "tu-email@ejemplo.com",
       "SmtpPassword": "tu-contraseña",
       "EnableSsl": true
     }
   }
   ```
 
3. Restaurar paquetes y compilar: 
 
   ```bash 
   dotnet restore 
   dotnet build 
   ``` 
 
4. Ejecutar la aplicación: 
 
   ```bash 
   dotnet run 
   ``` 
 
5. Abrir el navegador en: 
 
   ``` 
   http://localhost:5001/ 
   ``` 
 
   (Ajústalo al puerto que indique tu salida de `dotnet run`). 

## Controladores

### PokemonController

Controlador principal que maneja todas las operaciones relacionadas con Pokémon:

- **Index [GET]** - Página principal con listado paginado. Acepta parámetros `nameFilter`, `speciesFilter` y `page` para filtrado y navegación.

- **ExportToExcel [POST]** - Exporta datos a Excel. Recibe JSON con lista de Pokémon y retorna archivo `.xlsx`.

- **SendBulkEmail [POST]** - Envía correos masivos. Acepta `emailList` (separado por comas), `subject` y `body` del formulario.

- **Detail [GET]** - Obtiene detalles de un Pokémon específico. Requiere parámetro `id` y retorna vista parcial.
 
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
│    │    ├─ index.css 
│    │    └─ pokemonDetail.css 
│    │    └─ site.css 
│    │ 
│    └─ js 
│         └─ index.js 
│         └─ site.js 
│ 
├─ appsettings.json 
├─ appsettings.example.json
├─ Program.cs 
├─ MiPokemonApp.csproj 
└─ README.md 
```