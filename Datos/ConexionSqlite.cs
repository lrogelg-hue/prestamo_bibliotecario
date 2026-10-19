using Microsoft.Data.Sqlite;

namespace SistemaBiblioteca.Datos;

/// <summary>
/// Administra la conexion a la base de datos SQLite y la creacion del esquema.
/// </summary>
public class ConexionSqlite
{
    private readonly string _cadenaConexion;

    public ConexionSqlite(string rutaBaseDatos)
    {
        _cadenaConexion = new SqliteConnectionStringBuilder
        {
            DataSource = rutaBaseDatos,
            Mode = SqliteOpenMode.ReadWriteCreate,
            // Activa el chequeo de las claves foraneas declaradas en el esquema.
            ForeignKeys = true,
            // Sin pooling: cada operacion abre y cierra su conexion. Asi no
            // quedan manejos abiertos sobre el archivo (por ejemplo, al borrar
            // la base de datos temporal de la autoprueba).
            Pooling = false
        }.ToString();
    }

    /// <summary>
    /// Abre una conexion nueva. Quien llama debe liberarla (using).
    /// </summary>
    public SqliteConnection Abrir()
    {
        var conexion = new SqliteConnection(_cadenaConexion);
        conexion.Open();
        return conexion;
    }

    /// <summary>
    /// Crea tablas e indices si no existen. Es seguro llamarlo en cada arranque.
    /// </summary>
    public void Inicializar()
    {
        using var conexion = Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = Esquema;
        comando.ExecuteNonQuery();
    }

    /// <summary>
    /// Indica que la base de datos aun no tiene libros ni usuarios registrados.
    /// </summary>
    public bool EstaVacia()
    {
        using var conexion = Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = """
            SELECT (SELECT COUNT(*) FROM Libros) + (SELECT COUNT(*) FROM Usuarios);
            """;
        return Convert.ToInt32(comando.ExecuteScalar()) == 0;
    }

    private const string Esquema = """
        CREATE TABLE IF NOT EXISTS Libros (
            Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
            Titulo             TEXT    NOT NULL,
            Autor              TEXT    NOT NULL,
            Isbn               TEXT    NOT NULL,
            Anio               INTEGER NOT NULL,
            CantidadTotal      INTEGER NOT NULL CHECK (CantidadTotal > 0),
            CantidadDisponible INTEGER NOT NULL CHECK (CantidadDisponible >= 0)
        );

        CREATE TABLE IF NOT EXISTS Usuarios (
            Id     INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Carnet TEXT NOT NULL COLLATE NOCASE,
            Tipo   TEXT NOT NULL DEFAULT 'Estudiante'
        );

        -- El carnet no se puede repetir (la comparacion ignora mayusculas).
        CREATE UNIQUE INDEX IF NOT EXISTS ux_usuarios_carnet
            ON Usuarios (Carnet);

        -- Si se elimina un libro, desaparece su historial de prestamos (CASCADE).
        CREATE TABLE IF NOT EXISTS Prestamos (
            Id              INTEGER PRIMARY KEY AUTOINCREMENT,
            LibroId         INTEGER NOT NULL REFERENCES Libros (Id) ON DELETE CASCADE,
            UsuarioId       INTEGER NOT NULL REFERENCES Usuarios (Id),
            FechaPrestamo   TEXT    NOT NULL,
            FechaLimite     TEXT    NOT NULL,
            FechaDevolucion TEXT    NULL
        );

        CREATE INDEX IF NOT EXISTS ix_prestamos_libro ON Prestamos (LibroId);
        CREATE INDEX IF NOT EXISTS ix_prestamos_usuario ON Prestamos (UsuarioId);
        """;
}
