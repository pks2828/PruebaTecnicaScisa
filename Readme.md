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

- **Index [GET]**  
  Muestra la página principal con listado paginado de Pokémon, admite filtros (`nameFilter`, `typeFilter`) y paginación.

- **ExportToExcel [POST]**  
  Recibe JSON con filas de Pokémon y devuelve un archivo Excel (`.xlsx`) con esos datos.

- **SendBulkEmail [POST]**  
  Procesa una lista de correos (separados por comas), asunto y cuerpo, y envía correos masivos.

- **Detail [GET]**  
  Obtiene información detallada de un Pokémon por su `id` y retorna una vista parcial con esos datos.
 
## Estructura del Proyecto 
 
``` 
MiPokemonApp 
├─ Controllers 
│    └─ PokemonController.cs
├─ Helpers 
│    └─ PaginatedList.cs 
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
│    │    ├─ IPokemonService.cs    
│    │    └─ IEmailService.cs 
│    │ 
│    └─ Implementations 
│         ├─ PokeApiService.cs 
│         ├─ PokemonService.cs
│         ├─ EmailService.cs 
│         └─ ExcelService.cs 
│ 
├─ Views 
│    ├─ Pokemon 
│    │    ├─ _BulkEmailModalPartial.cshtml 
│    │    ├─ _DetailModalPartial.cshtml
│    │    └─ _PaginationPartial.cshtml
│    │    └─ _PokemonDetailPartial.cshtml
│    │    └─ Index.cshtml
│    └─ Shared 
│    |     └─ _ViewImports.cshtml
│    └─  
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
├─ .gitignore 
└─ README.md 
```

## Requisitos implementados

- [x] **Aplicación web en .NET Core 8 y FrontEnd (MVC).**  
  Se creó un proyecto ASP.NET Core 8 con patrón MVC para separar vistas, controladores y modelos, cumpliendo la estructura requerida.

- [x] **Consumir la API https://pokeapi.co/.**  
  Toda la lógica de obtención de datos (lista, detalle, tipos, especies) se realiza mediante el servicio `PokeApiService`, que llama asíncronamente a PokeAPI.

- [x] **Subir código a GitHub.**  
  El repositorio se inicializó y se subió al remoto, manteniendo historial limpio y ramas organizadas.

- [x] **Mostrar listado de Pokémon en un grid con imagen, nombre, botón de exportar a Excel y botón de envío de correo (individual o masivo).**  
  En la vista **Index**, se renderiza una tabla con tres columnas:  
  1. Imagen (extraída desde el repositorio publico de PokeApi en GitHub).  
  2. Nombre (propiedad `Name` del modelo).  
  3. Acciones:  
     - **Exportar a Excel**: al hacer clic, se recogen solo las filas visibles y se envían al método `ExportToExcel`.  
     - **Enviar correo (individual o masivo)**: abre un modal con un solo campo donde se ingresa uno o varios correos separados por comas. Si solo contiene un email, se envía de forma individual; si incluye comas, se envían correos masivos a la lista usando `SendBulkEmailAsync`.  

- [x] **Filtros por Nombre (texto) y Especie (dropdown cargado desde el API).**  
  - **Nombre**: cuadro de texto que filtra en el servidor usando LINQ sobre la lista obtenida.  
  - **Especie**: dropdown poblado con `GetAllTypesAsync()`. Si se selecciona una especie, se carga la lista de Pokémon de ese tipo con `GetPokemonsByTypeFullAsync`.

- [x] **Paginación manual.**  
  Se implementó una paginación por servidor con `PaginatedList<T>`. Cada página muestra 20 ítems (constante `PAGE_SIZE`). El usuario navega entre páginas mediante enlaces generados en la vista, y el controlador recibe el parámetro `page` por query string.

- [x] **Llamadas asíncronas y manejo correcto de errores.**  
  Todos los métodos de servicio (`GetPokemonListAsync`, `GetPokemonDetailAsync`, etc.) usan `async/await`. Se usan bloques `try-catch` específicos (por ejemplo, `HttpRequestException`, `JsonException`) para capturar fallos y retornar mensajes de error amigables. En caso de excepción general, `HandleError` en el controlador muestra una vista con mensaje genérico.

- [x] **Código limpio, estructurado y comentado.**  
  - Se respetaron normas de nombrado (constantes, métodos privados con función única, clases y propiedades con XML documentation).  
  - Se usaron excepciones personalizadas (`PokeApiException`, `ExcelException`, `EmailException`) para diferenciar errores.  
  - Los controladores y servicios tienen regiones bien delimitadas y comentarios claros (XML y comentarios inline solo para lógica compleja).

### Puntos extra (implementados)

- [x] **Mostrar detalles del Pokémon al hacer clic (modal o nueva vista).**  
  Al presionar el botón “Detalle”, se invoca la función `showDetail(id)` del JS que hace fetch a `/Pokemon/Detail?id={id}`. El servidor responde con una vista parcial (`_PokemonDetailPartial`) y se muestra en un modal, sin recargar la página.

- [x] **Usar caching local para evitar múltiples llamadas a la misma especie.**  
  Se utiliza `IMemoryCache` en `Index` para guardar el `PokemonFilterViewModel` completo con sus datos paginados y filtrados. Cada combinación de filtros y página genera una clave única, y si existe en caché, se devuelve sin volver a llamar a PokeAPI.

- [x] **Especie (segunda llamada) para obtener info desde `/pokemon-species/{id}`.**  
  Aunque la propiedad “especie” para cada Pokémon se obtuvo en la llamada a `/pokemon/{id}`, se añadió lógica en `PokeApiService` para, si se necesitara más detalle de especies, utilizar `/pokemon-species/{id}`. De momento, la lista de filtros por especie se basa en `/type`, y el detalle de cada Pokémon incluye su especie si es necesario (se puede activar fácilmente llamando al endpoint de especies).
