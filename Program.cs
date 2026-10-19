using SistemaBiblioteca.Modelos;
using SistemaBiblioteca.Servicios;

// Los datos se persisten en SQLite. En modo normal se usa biblioteca.db
// (se conserva entre sesiones); la autoprueba usa una BD temporal desechable.
bool modoPrueba = args.Contains("--test");
string rutaBaseDatos = modoPrueba ? "biblioteca_test.db" : "biblioteca.db";

if (modoPrueba)
{
    // La autoprueba siempre arranca con una base de datos limpia
    // y la borra al terminar para no dejar residuos.
    BorrarArchivoEnSilencio(rutaBaseDatos);

    var bibliotecaPrueba = new Biblioteca(rutaBaseDatos);
    CargarDatosDeEjemplo(bibliotecaPrueba);
    EjecutarAutoprueba(bibliotecaPrueba);

    BorrarArchivoEnSilencio(rutaBaseDatos);
    return;
}

var biblioteca = new Biblioteca(rutaBaseDatos);

// Los datos de ejemplo solo se cargan la primera vez (cuando la BD esta vacia);
// despues se conserva todo lo registrado en sesiones anteriores.
if (biblioteca.EstaVacia())
{
    CargarDatosDeEjemplo(biblioteca);
}

bool salir = false;
while (!salir)
{
    MostrarMenuPrincipal();
    var opcion = Console.ReadLine();

    Console.WriteLine();

    // Si la entrada termina (por ejemplo, salida redirigida), salimos para no entrar en bucle.
    if (opcion is null)
    {
        Console.WriteLine("No hay mas entrada. Saliendo del sistema.");
        break;
    }

    try
    {
        switch (opcion)
        {
            case "1":
                MenuLibros(biblioteca);
                break;
            case "2":
                MenuUsuarios(biblioteca);
                break;
            case "3":
                MenuPrestamos(biblioteca);
                break;
            case "4":
                MostrarReportes(biblioteca);
                break;
            case "0":
                salir = true;
                Console.WriteLine("Saliendo del sistema. Hasta pronto!");
                break;
            default:
                Console.WriteLine("Opcion no valida. Intenta de nuevo.");
                break;
        }
    }
    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    if (!salir)
    {
        Pausar();
    }
}

// ===================================================================
// Menu principal
// ===================================================================
static void MostrarMenuPrincipal()
{
    LimpiarPantalla();
    Console.WriteLine("=================================================");
    Console.WriteLine("     SISTEMA DE PRESTAMO BIBLIOTECARIO");
    Console.WriteLine("=================================================");
    Console.WriteLine("1. Gestionar libros");
    Console.WriteLine("2. Gestionar usuarios");
    Console.WriteLine("3. Gestionar prestamos y devoluciones");
    Console.WriteLine("4. Reportes");
    Console.WriteLine("0. Salir");
    Console.WriteLine("-------------------------------------------------");
    Console.Write("Selecciona una opcion: ");
}

// ===================================================================
// Menu de libros
// ===================================================================
static void MenuLibros(Biblioteca biblioteca)
{
    bool volver = false;
    while (!volver)
    {
        LimpiarPantalla();
        Console.WriteLine("--- GESTION DE LIBROS ---");
        Console.WriteLine("1. Registrar libro");
        Console.WriteLine("2. Listar libros");
        Console.WriteLine("3. Buscar libro");
        Console.WriteLine("4. Eliminar libro");
        Console.WriteLine("0. Volver");
        Console.Write("Opcion: ");

        switch (Console.ReadLine())
        {
            case "1":
                RegistrarLibro(biblioteca);
                Pausar();
                break;
            case "2":
                ListarLibros(biblioteca);
                Pausar();
                break;
            case "3":
                BuscarLibro(biblioteca);
                Pausar();
                break;
            case "4":
                EliminarLibro(biblioteca);
                Pausar();
                break;
            case "0":
                volver = true;
                break;
            default:
                Console.WriteLine("Opcion no valida.");
                Pausar();
                break;
        }
    }
}

static void RegistrarLibro(Biblioteca biblioteca)
{
    Console.WriteLine();
    var titulo = LeerTexto("Titulo: ");
    var autor = LeerTexto("Autor: ");
    var isbn = LeerTexto("ISBN: ");
    var anio = LeerEntero("Anio de publicacion: ");
    var cantidad = LeerEntero("Cantidad de ejemplares: ");

    var libro = biblioteca.AgregarLibro(titulo, autor, isbn, anio, cantidad);
    Console.WriteLine($"Libro registrado con exito: {libro}");
}

static void ListarLibros(Biblioteca biblioteca)
{
    Console.WriteLine();
    var libros = biblioteca.ObtenerLibros();
    if (libros.Count == 0)
    {
        Console.WriteLine("No hay libros registrados.");
        return;
    }

    Console.WriteLine($"Total de libros: {libros.Count}");
    foreach (var libro in libros)
    {
        Console.WriteLine("  " + libro);
    }
}

static void BuscarLibro(Biblioteca biblioteca)
{
    Console.WriteLine();
    var texto = LeerTexto("Ingresa titulo, autor o ISBN a buscar: ");
    var resultados = biblioteca.BuscarLibros(texto).ToList();

    if (resultados.Count == 0)
    {
        Console.WriteLine("No se encontraron coincidencias.");
        return;
    }

    Console.WriteLine($"Se encontraron {resultados.Count} libro(s):");
    foreach (var libro in resultados)
    {
        Console.WriteLine("  " + libro);
    }
}

static void EliminarLibro(Biblioteca biblioteca)
{
    Console.WriteLine();
    var id = LeerEntero("Id del libro a eliminar: ");
    if (biblioteca.EliminarLibro(id))
    {
        Console.WriteLine("Libro eliminado.");
    }
    else
    {
        Console.WriteLine("No existe un libro con ese Id.");
    }
}

// ===================================================================
// Menu de usuarios
// ===================================================================
static void MenuUsuarios(Biblioteca biblioteca)
{
    bool volver = false;
    while (!volver)
    {
        LimpiarPantalla();
        Console.WriteLine("--- GESTION DE USUARIOS ---");
        Console.WriteLine("1. Registrar usuario");
        Console.WriteLine("2. Listar usuarios");
        Console.WriteLine("3. Buscar usuario");
        Console.WriteLine("0. Volver");
        Console.Write("Opcion: ");

        switch (Console.ReadLine())
        {
            case "1":
                RegistrarUsuario(biblioteca);
                Pausar();
                break;
            case "2":
                ListarUsuarios(biblioteca);
                Pausar();
                break;
            case "3":
                BuscarUsuario(biblioteca);
                Pausar();
                break;
            case "0":
                volver = true;
                break;
            default:
                Console.WriteLine("Opcion no valida.");
                Pausar();
                break;
        }
    }
}

static void RegistrarUsuario(Biblioteca biblioteca)
{
    Console.WriteLine();
    var nombre = LeerTexto("Nombre completo: ");
    var carnet = LeerTexto("Carnet: ");
    Console.WriteLine("Tipo de usuario:");
    Console.WriteLine("  1. Estudiante");
    Console.WriteLine("  2. Docente");
    Console.WriteLine("  3. Administrativo");
    var tipoOpt = Console.ReadLine();
    var tipo = tipoOpt switch
    {
        "2" => "Docente",
        "3" => "Administrativo",
        _ => "Estudiante"
    };

    var usuario = biblioteca.AgregarUsuario(nombre, carnet, tipo);
    Console.WriteLine($"Usuario registrado con exito: {usuario}");
}

static void ListarUsuarios(Biblioteca biblioteca)
{
    Console.WriteLine();
    var usuarios = biblioteca.ObtenerUsuarios();
    if (usuarios.Count == 0)
    {
        Console.WriteLine("No hay usuarios registrados.");
        return;
    }

    Console.WriteLine($"Total de usuarios: {usuarios.Count}");
    foreach (var usuario in usuarios)
    {
        Console.WriteLine("  " + usuario);
    }
}

static void BuscarUsuario(Biblioteca biblioteca)
{
    Console.WriteLine();
    var texto = LeerTexto("Ingresa nombre o carnet a buscar: ");
    var resultados = biblioteca.BuscarUsuarios(texto).ToList();

    if (resultados.Count == 0)
    {
        Console.WriteLine("No se encontraron coincidencias.");
        return;
    }

    Console.WriteLine($"Se encontraron {resultados.Count} usuario(s):");
    foreach (var usuario in resultados)
    {
        Console.WriteLine("  " + usuario);
    }
}

// ===================================================================
// Menu de prestamos
// ===================================================================
static void MenuPrestamos(Biblioteca biblioteca)
{
    bool volver = false;
    while (!volver)
    {
        LimpiarPantalla();
        Console.WriteLine("--- PRESTAMOS Y DEVOLUCIONES ---");
        Console.WriteLine("1. Registrar prestamo");
        Console.WriteLine("2. Registrar devolucion");
        Console.WriteLine("3. Listar prestamos activos");
        Console.WriteLine("0. Volver");
        Console.Write("Opcion: ");

        switch (Console.ReadLine())
        {
            case "1":
                RegistrarPrestamo(biblioteca);
                Pausar();
                break;
            case "2":
                RegistrarDevolucion(biblioteca);
                Pausar();
                break;
            case "3":
                ListarPrestamosActivos(biblioteca);
                Pausar();
                break;
            case "0":
                volver = true;
                break;
            default:
                Console.WriteLine("Opcion no valida.");
                Pausar();
                break;
        }
    }
}

static void RegistrarPrestamo(Biblioteca biblioteca)
{
    Console.WriteLine();
    if (biblioteca.ObtenerLibros().Count == 0 || biblioteca.ObtenerUsuarios().Count == 0)
    {
        Console.WriteLine("Debes registrar al menos un libro y un usuario primero.");
        return;
    }

    ListarLibros(biblioteca);
    var libroId = LeerEntero("Id del libro a prestar: ");

    Console.WriteLine();
    ListarUsuarios(biblioteca);
    var usuarioId = LeerEntero("Id del usuario: ");

    var dias = LeerEntero($"Dias de prestamo (Enter para {Biblioteca.DiasPrestamoPorDefecto}): ", permitirVacio: true);
    if (dias <= 0)
    {
        dias = Biblioteca.DiasPrestamoPorDefecto;
    }

    var prestamo = biblioteca.PrestarLibro(libroId, usuarioId, dias);
    Console.WriteLine($"Prestamo registrado: {prestamo}");
}

static void RegistrarDevolucion(Biblioteca biblioteca)
{
    Console.WriteLine();
    var activos = biblioteca.ObtenerPrestamosActivos().ToList();
    if (activos.Count == 0)
    {
        Console.WriteLine("No hay prestamos activos.");
        return;
    }

    foreach (var prestamo in activos)
    {
        Console.WriteLine("  " + prestamo);
    }

    var prestamoId = LeerEntero("Id del prestamo a devolver: ");
    var devuelto = biblioteca.DevolverLibro(prestamoId);

    Console.WriteLine($"Devolucion registrada. {devuelto}");
    if (devuelto.DiasAtraso() > 0)
    {
        Console.WriteLine($"Atencion: la devolucion tiene {devuelto.DiasAtraso()} dia(s) de atraso.");
    }
}

static void ListarPrestamosActivos(Biblioteca biblioteca)
{
    Console.WriteLine();
    var activos = biblioteca.ObtenerPrestamosActivos().ToList();
    if (activos.Count == 0)
    {
        Console.WriteLine("No hay prestamos activos.");
        return;
    }

    Console.WriteLine($"Prestamos activos: {activos.Count}");
    foreach (var prestamo in activos)
    {
        Console.WriteLine("  " + prestamo);
    }
}

// ===================================================================
// Reportes
// ===================================================================
static void MostrarReportes(Biblioteca biblioteca)
{
    LimpiarPantalla();
    Console.WriteLine("=================================================");
    Console.WriteLine("                  REPORTES");
    Console.WriteLine("=================================================");

    Console.WriteLine("\n1) Inventario de libros");
    foreach (var libro in biblioteca.ObtenerLibros())
    {
        Console.WriteLine("   " + libro);
    }

    Console.WriteLine($"\n2) Total de prestamos historicos: {biblioteca.ObtenerPrestamos().Count}");
    Console.WriteLine($"   Prestamos activos: {biblioteca.ObtenerPrestamosActivos().Count()}");

    Console.WriteLine("\n3) Prestamos vencidos (atrasados)");
    var vencidos = biblioteca.ObtenerPrestamosVencidos().ToList();
    if (vencidos.Count == 0)
    {
        Console.WriteLine("   Ninguno. Todo en orden.");
    }
    else
    {
        foreach (var prestamo in vencidos)
        {
            Console.WriteLine("   " + prestamo);
        }
    }

    Console.WriteLine("\n4) Libros sin ejemplares disponibles");
    var agotados = biblioteca.ObtenerLibros().Where(l => !l.Disponible).ToList();
    if (agotados.Count == 0)
    {
        Console.WriteLine("   Todos los libros tienen ejemplares disponibles.");
    }
    else
    {
        foreach (var libro in agotados)
        {
            Console.WriteLine("   " + libro);
        }
    }

    Console.WriteLine("\n5) Usuarios con prestamos activos");
    var porUsuario = biblioteca.ObtenerPrestamosActivos()
        .GroupBy(p => p.UsuarioId)
        .ToList();

    if (porUsuario.Count == 0)
    {
        Console.WriteLine("   Ningun usuario tiene prestamos activos.");
    }
    else
    {
        foreach (var grupo in porUsuario)
        {
            var usuario = biblioteca.BuscarUsuarioPorId(grupo.Key);
            Console.WriteLine($"   {usuario?.Nombre ?? $"Usuario #{grupo.Key}"}: {grupo.Count()} prestamo(s)");
        }
    }

    Pausar();
}

// ===================================================================
// Autoprueba de la logica (se ejecuta con: dotnet run -- --test)
// ===================================================================
static void EjecutarAutoprueba(Biblioteca biblioteca)
{
    int pruebas = 0, fallos = 0;

    void Afirmar(string descripcion, bool condicion)
    {
        pruebas++;
        if (!condicion)
        {
            fallos++;
        }
        Console.WriteLine($"  [{(condicion ? "OK " : "FALLA")}] {descripcion}");
    }

    Console.WriteLine("Ejecutando autoprueba de la logica...");

    // Libros cargados de ejemplo.
    Afirmar("Se cargaron 3 libros de ejemplo", biblioteca.ObtenerLibros().Count == 3);
    Afirmar("El libro #3 tiene 1 ejemplar disponible", biblioteca.BuscarLibroPorId(3)!.CantidadDisponible == 1);
    Afirmar("Buscar 'orwell' encuentra 1 libro", biblioteca.BuscarLibros("orwell").Count() == 1);
    Afirmar("Buscar por ISBN parcial funciona", biblioteca.BuscarLibros("978-0451").Any());

    // Usuarios.
    Afirmar("Se cargaron 3 usuarios", biblioteca.ObtenerUsuarios().Count == 3);
    Afirmar("Buscar usuario por carnet funciona", biblioteca.BuscarUsuarioPorCarnet("2021001")?.Nombre == "Ana Lopez");
    Afirmar("No se permite carnet duplicado", Lanza<ArgumentException>(() => biblioteca.AgregarUsuario("Otra", "2021001", "Estudiante")));

    // Prestamo valido.
    var prestamo = biblioteca.PrestarLibro(libroId: 1, usuarioId: 1);
    Afirmar("El prestamo se crea activo", prestamo.Activo);
    Afirmar("El libro #1 baja a 2 disponibles", biblioteca.BuscarLibroPorId(1)!.CantidadDisponible == 2);
    Afirmar("El plazo del prestamo es 7 dias", (prestamo.FechaLimite - prestamo.FechaPrestamo).Days == 7);
    Afirmar("El prestamo no esta vencido hoy", !prestamo.EstaVencido());

    // Un libro agotado no se puede prestar.
    biblioteca.PrestarLibro(libroId: 3, usuarioId: 2);
    Afirmar("El libro #3 queda agotado", !biblioteca.BuscarLibroPorId(3)!.Disponible);
    Afirmar("No se presta un libro sin ejemplares", Lanza<InvalidOperationException>(() => biblioteca.PrestarLibro(3, 3)));
    Afirmar("No se presta un libro inexistente", Lanza<ArgumentException>(() => biblioteca.PrestarLibro(999, 1)));
    Afirmar("No se presta a un usuario inexistente", Lanza<ArgumentException>(() => biblioteca.PrestarLibro(2, 999)));

    // Deteccion de vencimiento.
    var vencido = biblioteca.PrestarLibro(2, 1);
    Afirmar("Un prestamo vencido se detecta a futuro",
        vencido.EstaVencido(DateTime.Today.AddDays(30)));
    Afirmar("El atraso se calcula en dias",
        vencido.DiasAtraso(DateTime.Today.AddDays(10)) == 3);

    // Devolucion.
    var devuelto = biblioteca.DevolverLibro(prestamo.Id);
    Afirmar("Tras devolver, el prestamo queda cerrado", !devuelto.Activo);
    Afirmar("La fecha de devolucion queda registrada", devuelto.FechaDevolucion == DateTime.Today);
    Afirmar("El libro #1 vuelve a 3 disponibles", biblioteca.BuscarLibroPorId(1)!.CantidadDisponible == 3);
    Afirmar("No se devuelve dos veces", Lanza<InvalidOperationException>(() => biblioteca.DevolverLibro(prestamo.Id)));

    // Eliminacion.
    Afirmar("No se elimina un libro con prestamos activos", Lanza<InvalidOperationException>(() => biblioteca.EliminarLibro(3)));
    Afirmar("Se elimina un libro sin prestamos activos", biblioteca.EliminarLibro(1));

    Console.WriteLine($"\nResultado: {pruebas - fallos}/{pruebas} pruebas correctas.");
    Console.WriteLine(fallos == 0 ? "TODO CORRECTO." : $"Hay {fallos} prueba(s) con fallas.");
}

static bool Lanza<TExcepcion>(Action accion) where TExcepcion : Exception
{
    try
    {
        accion();
        return false;
    }
    catch (TExcepcion)
    {
        return true;
    }
    catch
    {
        return false;
    }
}

/// <summary>
/// Borra un archivo si existe; si esta bloqueado, se omite sin romper la ejecucion
/// (la limpieza se reintenta en la siguiente corrida de la autoprueba).
/// </summary>
static void BorrarArchivoEnSilencio(string ruta)
{
    try
    {
        if (File.Exists(ruta))
        {
            File.Delete(ruta);
        }
    }
    catch (IOException)
    {
        // Nada que hacer: el archivo se eliminara en la proxima corrida.
    }
}

// ===================================================================
// Utilidades de entrada/salida
// ===================================================================
static string LeerTexto(string mensaje)
{
    string? valor;
    do
    {
        Console.Write(mensaje);
        valor = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(valor))
        {
            Console.WriteLine("Este campo no puede estar vacio.");
        }
    } while (string.IsNullOrWhiteSpace(valor));

    return valor.Trim();
}

static int LeerEntero(string mensaje, bool permitirVacio = false)
{
    while (true)
    {
        Console.Write(mensaje);
        var entrada = Console.ReadLine();

        if (permitirVacio && string.IsNullOrWhiteSpace(entrada))
        {
            return 0;
        }

        if (int.TryParse(entrada, out var numero))
        {
            return numero;
        }

        Console.WriteLine("Ingresa un numero valido.");
    }
}

static void Pausar()
{
    Console.WriteLine("\nPresiona Enter para continuar...");
    Console.ReadLine();
}

static void LimpiarPantalla()
{
    try
    {
        Console.Clear();
    }
    catch (IOException)
    {
        // No hay consola real (por ejemplo, salida redirigida): continuamos sin limpiar.
    }
}

// ===================================================================
// Datos de ejemplo para probar el sistema rapidamente
// ===================================================================
static void CargarDatosDeEjemplo(Biblioteca biblioteca)
{
    biblioteca.AgregarLibro("Cien anios de soledad", "Gabriel Garcia Marquez", "978-0307474728", 1967, 3);
    biblioteca.AgregarLibro("El Principito", "Antoine de Saint-Exupery", "978-0156012195", 1943, 2);
    biblioteca.AgregarLibro("1984", "George Orwell", "978-0451524935", 1949, 1);

    biblioteca.AgregarUsuario("Ana Lopez", "2021001", "Estudiante");
    biblioteca.AgregarUsuario("Carlos Perez", "2021002", "Estudiante");
    biblioteca.AgregarUsuario("Dra. Maria Ruiz", "DOC-100", "Docente");
}
