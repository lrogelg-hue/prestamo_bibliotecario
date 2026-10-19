namespace SistemaBiblioteca.Modelos;

/// <summary>
/// Representa un libro dentro de la biblioteca.
/// </summary>
public class Libro
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public int Anio { get; set; }
    public int CantidadTotal { get; set; }
    public int CantidadDisponible { get; set; }

    public bool Disponible => CantidadDisponible > 0;

    public override string ToString()
    {
        return $"[{Id}] \"{Titulo}\" - {Autor} ({Anio}) | ISBN: {Isbn} | Disponibles: {CantidadDisponible}/{CantidadTotal}";
    }
}
