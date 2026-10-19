# Sistema de Préstamo Bibliotecario

Aplicación de consola en **C# / .NET 10** para gestionar el préstamo de libros de una biblioteca,
con persistencia real en **SQLite** (los datos sobreviven al cerrar el programa).

## Funcionalidades

- **Gestión de libros**: registrar, listar, buscar (por título, autor o ISBN) y eliminar.
- **Gestión de usuarios**: registrar, listar y buscar (estudiantes, docentes, administrativos).
- **Préstamos y devoluciones**: prestar ejemplares con fecha límite, registrar devoluciones y
  detectar préstamos vencidos o con días de atraso.
- **Reportes**: inventario, préstamos históricos y activos, préstamos vencidos, libros agotados y
  usuarios con préstamos activos.
- **Persistencia SQLite**: la información se guarda en `biblioteca.db`; en la primera ejecución
  se crean las tablas y se cargan datos de ejemplo (3 libros y 3 usuarios), después se conserva
  todo lo registrado.

## Estructura del proyecto

```
SistemaBiblioteca/
├── SistemaBiblioteca.csproj        Paquete Microsoft.Data.Sqlite
├── Program.cs                      Menú principal y flujo de consola
├── Modelos/
│   ├── Libro.cs                    Datos de un libro
│   ├── Usuario.cs                  Datos de un usuario
│   └── Prestamo.cs                 Datos de un préstamo (incluye vencimiento)
├── Datos/
│   ├── ConexionSqlite.cs           Conexión, esquema (CREATE TABLE) y detección de BD vacía
│   ├── LibroRepositorio.cs         Consultas SQL de la tabla Libros
│   ├── UsuarioRepositorio.cs       Consultas SQL de la tabla Usuarios
│   └── PrestamoRepositorio.cs      Consultas SQL de Prestamos (prestar/devolver en transacción)
└── Servicios/
    └── Biblioteca.cs               Lógica de negocio sobre la capa Datos
```

## Cómo ejecutarlo

Desde la carpeta `SistemaBiblioteca`:

```powershell
dotnet run
```

### Abrirlo en Visual Studio Code

1. Abre VS Code y selecciona `Archivo > Abrir carpeta...`.
2. Elige la carpeta `C:\Users\eg270\Documents\Progra2\SistemaBiblioteca`.
3. Presiona `F5` (o usa la extensión **C# Dev Kit** / **C#**) para compilar y depurar.

### Compilar sin ejecutar

```powershell
dotnet build
```

## Base de datos

- El motor es **SQLite** mediante el paquete `Microsoft.Data.Sqlite` (SQL parametrizado, sin ORM).
- El archivo `biblioteca.db` se crea en el directorio de trabajo junto al ejecutable.
- Los datos de ejemplo solo se insertan **si la base de datos está vacía**; si ya existe
  información, se arranca con lo registrado en sesiones anteriores.
- Para "reiniciar" el sistema, borra `biblioteca.db` y vuelve a ejecutar.
- Reglas aplicadas por la base de datos: carnet de usuario único (sin distinguir mayúsculas),
  cantidades no negativas, claves foráneas entre préstamos y sus tablas, e índices de consulta.
- Prestar y devolver se ejecutan en **transacciones** (descuento/incremento de ejemplares y
  registro del préstamo son atómicos).

## Autoprueba de la lógica

Incluye una verificación automática de la lógica de negocio (23 casos) que no requiere interacción.
Usa una base de datos temporal (`biblioteca_test.db`) que se crea limpia y se borra al terminar,
así nunca toca tus datos reales:

```powershell
dotnet run -- --test
```

## Reglas de negocio principales

- No se puede prestar un libro sin ejemplares disponibles.
- Cada préstamo dura **7 días** por defecto (configurable al momento de prestar).
- Un préstamo se marca como **VENCIDO** si pasa la fecha límite sin devolución.
- No se puede eliminar un libro que tenga préstamos activos.
- No se permiten dos usuarios con el mismo carnet.

## Posibles mejoras

- Cálculo de multas por atraso según el tipo de usuario.
- Exportar reportes a CSV o PDF.
- Interfaz gráfica con Windows Forms o aplicación web con ASP.NET.
