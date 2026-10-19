using System.Globalization;
using Microsoft.Data.Sqlite;
using SistemaBiblioteca.Modelos;

namespace SistemaBiblioteca.Datos;

/// <summary>
/// Acceso a datos de la tabla Prestamos. Las operaciones que tocan dos tablas
/// a la vez (prestar y devolver) se ejecutan dentro de una transaccion.
/// </summary>
public class PrestamoRepositorio
{
    private readonly ConexionSqlite _conexion;

    public PrestamoRepositorio(ConexionSqlite conexion) => _conexion = conexion;

    /// <summary>
    /// En una sola transaccion descuenta un ejemplar del libro y registra el prestamo.
    /// Retorna null si el libro ya no tenia ejemplares disponibles (no se aplica nada).
    /// </summary>
    public Prestamo? Prestar(int libroId, int usuarioId, DateTime fechaPrestamo, DateTime fechaLimite)
    {
        using var conexion = _conexion.Abrir();
        using var transaccion = conexion.BeginTransaction();

        // Descuento condicional: solo se aplica si quedan ejemplares.
        using (var descuento = conexion.CreateCommand())
        {
            descuento.Transaction = transaccion;
            descuento.CommandText = """
                UPDATE Libros
                   SET CantidadDisponible = CantidadDisponible - 1
                 WHERE Id = @libroId
                   AND CantidadDisponible > 0;
                """;
            descuento.Parameters.AddWithValue("@libroId", libroId);

            if (descuento.ExecuteNonQuery() == 0)
            {
                transaccion.Rollback();
                return null;
            }
        }

        var prestamo = new Prestamo
        {
            LibroId = libroId,
            UsuarioId = usuarioId,
            FechaPrestamo = fechaPrestamo,
            FechaLimite = fechaLimite
        };

        using (var insercion = conexion.CreateCommand())
        {
            insercion.Transaction = transaccion;
            insercion.CommandText = """
                INSERT INTO Prestamos (LibroId, UsuarioId, FechaPrestamo, FechaLimite, FechaDevolucion)
                VALUES (@libroId, @usuarioId, @fechaPrestamo, @fechaLimite, NULL);
                SELECT last_insert_rowid();
                """;
            insercion.Parameters.AddWithValue("@libroId", prestamo.LibroId);
            insercion.Parameters.AddWithValue("@usuarioId", prestamo.UsuarioId);
            insercion.Parameters.AddWithValue("@fechaPrestamo", FormatearFecha(prestamo.FechaPrestamo));
            insercion.Parameters.AddWithValue("@fechaLimite", FormatearFecha(prestamo.FechaLimite));

            prestamo.Id = Convert.ToInt32(insercion.ExecuteScalar());
        }

        transaccion.Commit();
        return prestamo;
    }

    /// <summary>
    /// En una sola transaccion marca la devolucion y devuelve el ejemplar al inventario.
    /// Retorna null si el prestamo no existia o ya estaba devuelto.
    /// </summary>
    public Prestamo? Devolver(int prestamoId, DateTime fechaDevolucion)
    {
        using var conexion = _conexion.Abrir();
        using var transaccion = conexion.BeginTransaction();

        using (var marcado = conexion.CreateCommand())
        {
            marcado.Transaction = transaccion;
            marcado.CommandText = """
                UPDATE Prestamos
                   SET FechaDevolucion = @fecha
                 WHERE Id = @prestamoId
                   AND FechaDevolucion IS NULL;
                """;
            marcado.Parameters.AddWithValue("@fecha", FormatearFecha(fechaDevolucion));
            marcado.Parameters.AddWithValue("@prestamoId", prestamoId);

            if (marcado.ExecuteNonQuery() == 0)
            {
                transaccion.Rollback();
                return null;
            }
        }

        // Se devuelve el ejemplar al inventario sin superar el total registrado.
        using (var inventario = conexion.CreateCommand())
        {
            inventario.Transaction = transaccion;
            inventario.CommandText = """
                UPDATE Libros
                   SET CantidadDisponible = MIN(CantidadDisponible + 1, CantidadTotal)
                 WHERE Id = (SELECT LibroId FROM Prestamos WHERE Id = @prestamoId);
                """;
            inventario.Parameters.AddWithValue("@prestamoId", prestamoId);
            inventario.ExecuteNonQuery();
        }

        var prestamo = BuscarPorId(conexion, prestamoId, transaccion);
        transaccion.Commit();
        return prestamo;
    }

    public Prestamo? BuscarPorId(int prestamoId)
    {
        using var conexion = _conexion.Abrir();
        return BuscarPorId(conexion, prestamoId, transaccion: null);
    }

    public IReadOnlyList<Prestamo> ObtenerTodos() =>
        Consultar("SELECT * FROM Prestamos ORDER BY Id;", null);

    public IReadOnlyList<Prestamo> ObtenerActivos() =>
        Consultar("""
            SELECT * FROM Prestamos
             WHERE FechaDevolucion IS NULL
             ORDER BY Id;
            """, null);

    /// <summary>
    /// Prestamos activos cuya fecha limite ya paso respecto a la fecha indicada.
    /// </summary>
    public IReadOnlyList<Prestamo> ObtenerVencidos(DateTime fecha) =>
        Consultar("""
            SELECT * FROM Prestamos
             WHERE FechaDevolucion IS NULL
               AND FechaLimite < @fecha
             ORDER BY Id;
            """, comando => comando.Parameters.AddWithValue("@fecha", FormatearFecha(fecha)));

    public bool TienePrestamosActivos(int libroId)
    {
        using var conexion = _conexion.Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = """
            SELECT COUNT(*) FROM Prestamos
             WHERE FechaDevolucion IS NULL AND LibroId = @libroId;
            """;
        comando.Parameters.AddWithValue("@libroId", libroId);
        return Convert.ToInt32(comando.ExecuteScalar()) > 0;
    }

    public bool UsuarioTienePrestamoActivo(int usuarioId, int libroId)
    {
        using var conexion = _conexion.Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = """
            SELECT COUNT(*) FROM Prestamos
             WHERE FechaDevolucion IS NULL
               AND UsuarioId = @usuarioId
               AND LibroId = @libroId;
            """;
        comando.Parameters.AddWithValue("@usuarioId", usuarioId);
        comando.Parameters.AddWithValue("@libroId", libroId);
        return Convert.ToInt32(comando.ExecuteScalar()) > 0;
    }

    private static Prestamo? BuscarPorId(SqliteConnection conexion, int prestamoId, SqliteTransaction? transaccion)
    {
        using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion;
        comando.CommandText = "SELECT * FROM Prestamos WHERE Id = @id;";
        comando.Parameters.AddWithValue("@id", prestamoId);

        using var lector = comando.ExecuteReader();
        return lector.Read() ? Leer(lector) : null;
    }

    private IReadOnlyList<Prestamo> Consultar(string sql, Action<SqliteCommand>? preparar)
    {
        using var conexion = _conexion.Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        preparar?.Invoke(comando);

        using var lector = comando.ExecuteReader();
        var lista = new List<Prestamo>();
        while (lector.Read())
        {
            lista.Add(Leer(lector));
        }

        return lista;
    }

    private static Prestamo Leer(SqliteDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        LibroId = lector.GetInt32(1),
        UsuarioId = lector.GetInt32(2),
        FechaPrestamo = LeerFecha(lector.GetString(3)),
        FechaLimite = LeerFecha(lector.GetString(4)),
        FechaDevolucion = lector.IsDBNull(5) ? null : LeerFecha(lector.GetString(5))
    };

    // SQLite no tiene tipo fecha: se guardan como texto ISO "aaaa-mm-dd",
    // formato que ademas permite comparar ordenadamente en SQL.
    private static string FormatearFecha(DateTime fecha) =>
        fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateTime LeerFecha(string texto) =>
        DateTime.ParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
