namespace SistemaBiblioteca.Modelos;

/// <summary>
/// Representa a una persona que puede solicitar prestamos (estudiante, docente, etc.).
/// </summary>
public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Carnet { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Estudiante";

    public override string ToString()
    {
        return $"[{Id}] {Nombre} | Carnet: {Carnet} | Tipo: {Tipo}";
    }
}
