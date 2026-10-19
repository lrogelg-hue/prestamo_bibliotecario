using Microsoft.Data.Sqlite;
using SistemaBiblioteca.Modelos;

namespace SistemaBiblioteca.Datos;

/// <summary>
/// Acceso a datos de la tabla Usuarios (SQL parametrizado).
/// </summary>
public class UsuarioRepositorio
{
    private readonly ConexionSqlite _conexion;

    public UsuarioRepositorio(ConexionSqlite conexion) => _conexion = conexion;

    /// <summary>
    /// Inserta el usuario. Si el carnet ya existe, SQLite lanza una excepcion
    /// de restriccion (UNIQUE) que el llamador puede traducir a mensaje de negocio.
    /// </summary>
    public Usuario Insertar(Usuario usuario)
    {
        using var conexion = _conexion.Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO Usuarios (Nombre, Carnet, Tipo)
            VALUES (@nombre, @carnet, @tipo);
            SELECT last_insert_rowid();
            """;
        comando.Parameters.AddWithValue("@nombre", usuario.Nombre);
        comando.Parameters.AddWithValue("@carnet", usuario.Carnet);
        comando.Parameters.AddWithValue("@tipo", usuario.Tipo);

        usuario.Id = Convert.ToInt32(comando.ExecuteScalar());
        return usuario;
    }

    public IReadOnlyList<Usuario> ObtenerTodos() =>
        Consultar("SELECT * FROM Usuarios ORDER BY Id;", null);

    public Usuario? BuscarPorId(int id)
    {
        using var conexion = _conexion.Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT * FROM Usuarios WHERE Id = @id;";
        comando.Parameters.AddWithValue("@id", id);

        using var lector = comando.ExecuteReader();
        return lector.Read() ? Leer(lector) : null;
    }

    /// <summary>
    /// Busca por carnet exacto sin distinguir mayusculas (COLLATE NOCASE).
    /// </summary>
    public Usuario? BuscarPorCarnet(string carnet)
    {
        using var conexion = _conexion.Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT * FROM Usuarios WHERE Carnet = @carnet;";
        comando.Parameters.AddWithValue("@carnet", carnet);

        using var lector = comando.ExecuteReader();
        return lector.Read() ? Leer(lector) : null;
    }

    /// <summary>
    /// Busca por nombre o carnet. El LIKE de SQLite ignora mayusculas.
    /// </summary>
    public IReadOnlyList<Usuario> BuscarPorTexto(string texto)
    {
        var patron = $"%{texto.Trim()}%";
        return Consultar("""
            SELECT * FROM Usuarios
             WHERE Nombre LIKE @patron OR Carnet LIKE @patron
             ORDER BY Id;
            """, comando => comando.Parameters.AddWithValue("@patron", patron));
    }

    private IReadOnlyList<Usuario> Consultar(string sql, Action<SqliteCommand>? preparar)
    {
        using var conexion = _conexion.Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        preparar?.Invoke(comando);

        using var lector = comando.ExecuteReader();
        var lista = new List<Usuario>();
        while (lector.Read())
        {
            lista.Add(Leer(lector));
        }

        return lista;
    }

    private static Usuario Leer(SqliteDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        Nombre = lector.GetString(1),
        Carnet = lector.GetString(2),
        Tipo = lector.GetString(3)
    };
}
