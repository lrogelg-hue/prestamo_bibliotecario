using Microsoft.Data.Sqlite;
using SistemaBiblioteca.Modelos;

namespace SistemaBiblioteca.Datos;

/// <summary>
/// Acceso a datos de la tabla Libros (SQL parametrizado).
/// </summary>
public class LibroRepositorio
{
    private readonly ConexionSqlite _conexion;

    public LibroRepositorio(ConexionSqlite conexion) => _conexion = conexion;

    public Libro Insertar(Libro libro)
    {
        using var conexion = _conexion.Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO Libros (Titulo, Autor, Isbn, Anio, CantidadTotal, CantidadDisponible)
            VALUES (@titulo, @autor, @isbn, @anio, @total, @disponible);
            SELECT last_insert_rowid();
            """;
        comando.Parameters.AddWithValue("@titulo", libro.Titulo);
        comando.Parameters.AddWithValue("@autor", libro.Autor);
        comando.Parameters.AddWithValue("@isbn", libro.Isbn);
        comando.Parameters.AddWithValue("@anio", libro.Anio);
        comando.Parameters.AddWithValue("@total", libro.CantidadTotal);
        comando.Parameters.AddWithValue("@disponible", libro.CantidadDisponible);

        libro.Id = Convert.ToInt32(comando.ExecuteScalar());
        return libro;
    }

    public IReadOnlyList<Libro> ObtenerTodos() =>
        Consultar("SELECT * FROM Libros ORDER BY Id;", null);

    public Libro? BuscarPorId(int id)
    {
        using var conexion = _conexion.Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT * FROM Libros WHERE Id = @id;";
        comando.Parameters.AddWithValue("@id", id);

        using var lector = comando.ExecuteReader();
        return lector.Read() ? Leer(lector) : null;
    }

    /// <summary>
    /// Busca por titulo, autor o ISBN. El LIKE de SQLite ignora mayusculas.
    /// </summary>
    public IReadOnlyList<Libro> BuscarPorTexto(string texto)
    {
        var patron = $"%{texto.Trim()}%";
        return Consultar("""
            SELECT * FROM Libros
             WHERE Titulo LIKE @patron OR Autor LIKE @patron OR Isbn LIKE @patron
             ORDER BY Id;
            """, comando => comando.Parameters.AddWithValue("@patron", patron));
    }

    public bool Eliminar(int id)
    {
        using var conexion = _conexion.Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = "DELETE FROM Libros WHERE Id = @id;";
        comando.Parameters.AddWithValue("@id", id);
        return comando.ExecuteNonQuery() > 0;
    }

    private IReadOnlyList<Libro> Consultar(string sql, Action<SqliteCommand>? preparar)
    {
        using var conexion = _conexion.Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        preparar?.Invoke(comando);

        using var lector = comando.ExecuteReader();
        var lista = new List<Libro>();
        while (lector.Read())
        {
            lista.Add(Leer(lector));
        }

        return lista;
    }

    private static Libro Leer(SqliteDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        Titulo = lector.GetString(1),
        Autor = lector.GetString(2),
        Isbn = lector.GetString(3),
        Anio = lector.GetInt32(4),
        CantidadTotal = lector.GetInt32(5),
        CantidadDisponible = lector.GetInt32(6)
    };
}
