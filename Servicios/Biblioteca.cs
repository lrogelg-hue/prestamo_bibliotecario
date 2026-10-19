using Microsoft.Data.Sqlite;
using SistemaBiblioteca.Datos;
using SistemaBiblioteca.Modelos;

namespace SistemaBiblioteca.Servicios;

/// <summary>
/// Contiene toda la logica del sistema de prestamo bibliotecario:
/// gestion de libros, usuarios, prestamos y reportes.
/// Los datos se guardan en SQLite (ver capa Datos/); el archivo de la base
/// de datos persiste entre sesiones.
/// </summary>
public class Biblioteca
{
    // Dias que se presta un libro por defecto.
    public const int DiasPrestamoPorDefecto = 7;

    private readonly ConexionSqlite _conexion;
    private readonly LibroRepositorio _libros;
    private readonly UsuarioRepositorio _usuarios;
    private readonly PrestamoRepositorio _prestamos;

    public Biblioteca(string rutaBaseDatos)
    {
        _conexion = new ConexionSqlite(rutaBaseDatos);
        _conexion.Inicializar();

        _libros = new LibroRepositorio(_conexion);
        _usuarios = new UsuarioRepositorio(_conexion);
        _prestamos = new PrestamoRepositorio(_conexion);
    }

    /// <summary>
    /// Indica que la base de datos aun no tiene libros ni usuarios registrados.
    /// </summary>
    public bool EstaVacia() => _conexion.EstaVacia();

    // ---------------------------------------------------------------
    // Libros
    // ---------------------------------------------------------------

    public Libro AgregarLibro(string titulo, string autor, string isbn, int anio, int cantidad)
    {
        if (cantidad <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor que cero.");
        }

        var libro = new Libro
        {
            Titulo = titulo,
            Autor = autor,
            Isbn = isbn,
            Anio = anio,
            CantidadTotal = cantidad,
            CantidadDisponible = cantidad
        };

        return _libros.Insertar(libro);
    }

    public IReadOnlyList<Libro> ObtenerLibros() => _libros.ObtenerTodos();

    public Libro? BuscarLibroPorId(int id) => _libros.BuscarPorId(id);

    public IEnumerable<Libro> BuscarLibros(string texto) => _libros.BuscarPorTexto(texto);

    public bool EliminarLibro(int id)
    {
        if (BuscarLibroPorId(id) is null)
        {
            return false;
        }

        if (_prestamos.TienePrestamosActivos(id))
        {
            throw new InvalidOperationException("No se puede eliminar: el libro tiene prestamos activos.");
        }

        return _libros.Eliminar(id);
    }

    // ---------------------------------------------------------------
    // Usuarios
    // ---------------------------------------------------------------

    public Usuario AgregarUsuario(string nombre, string carnet, string tipo)
    {
        if (_usuarios.BuscarPorCarnet(carnet) is not null)
        {
            throw new ArgumentException($"Ya existe un usuario con el carnet {carnet}.");
        }

        var usuario = new Usuario
        {
            Nombre = nombre,
            Carnet = carnet,
            Tipo = tipo
        };

        try
        {
            return _usuarios.Insertar(usuario);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            // 19 = SQLITE_CONSTRAINT: el indice UNIQUE del carnet gano la carrera.
            throw new ArgumentException($"Ya existe un usuario con el carnet {carnet}.", ex);
        }
    }

    public IReadOnlyList<Usuario> ObtenerUsuarios() => _usuarios.ObtenerTodos();

    public Usuario? BuscarUsuarioPorId(int id) => _usuarios.BuscarPorId(id);

    public Usuario? BuscarUsuarioPorCarnet(string carnet) => _usuarios.BuscarPorCarnet(carnet);

    public IEnumerable<Usuario> BuscarUsuarios(string texto) => _usuarios.BuscarPorTexto(texto);

    // ---------------------------------------------------------------
    // Prestamos
    // ---------------------------------------------------------------

    public Prestamo PrestarLibro(int libroId, int usuarioId, int dias = DiasPrestamoPorDefecto)
    {
        var libro = BuscarLibroPorId(libroId)
            ?? throw new ArgumentException($"No existe el libro con Id {libroId}.");

        var usuario = BuscarUsuarioPorId(usuarioId)
            ?? throw new ArgumentException($"No existe el usuario con Id {usuarioId}.");

        if (!libro.Disponible)
        {
            throw new InvalidOperationException($"El libro \"{libro.Titulo}\" no tiene ejemplares disponibles.");
        }

        // La reserva del ejemplar y el registro del prestamo son atomicos.
        var prestamo = _prestamos.Prestar(
            libroId,
            usuario.Id,
            DateTime.Today,
            DateTime.Today.AddDays(dias));

        if (prestamo is null)
        {
            // Otro proceso gano la carrera por el ultimo ejemplar.
            throw new InvalidOperationException($"El libro \"{libro.Titulo}\" no tiene ejemplares disponibles.");
        }

        return prestamo;
    }

    public Prestamo DevolverLibro(int prestamoId)
    {
        var prestamo = _prestamos.BuscarPorId(prestamoId)
            ?? throw new ArgumentException($"No existe el prestamo con Id {prestamoId}.");

        if (!prestamo.Activo)
        {
            throw new InvalidOperationException("Este prestamo ya fue devuelto.");
        }

        // La marca de devolucion y la devolucion del ejemplar son atomicas.
        var actualizado = _prestamos.Devolver(prestamoId, DateTime.Today)
            ?? throw new InvalidOperationException("Este prestamo ya fue devuelto.");

        return actualizado;
    }

    public IReadOnlyList<Prestamo> ObtenerPrestamos() => _prestamos.ObtenerTodos();

    public IEnumerable<Prestamo> ObtenerPrestamosActivos() => _prestamos.ObtenerActivos();

    public IEnumerable<Prestamo> ObtenerPrestamosVencidos(DateTime? fecha = null) =>
        _prestamos.ObtenerVencidos(fecha ?? DateTime.Today);

    // ---------------------------------------------------------------
    // Reportes
    // ---------------------------------------------------------------

    /// <summary>
    /// Retorna true si un libro esta prestado por un usuario en particular.
    /// </summary>
    public bool UsuarioTienePrestado(int usuarioId, int libroId) =>
        _prestamos.UsuarioTienePrestamoActivo(usuarioId, libroId);
}
