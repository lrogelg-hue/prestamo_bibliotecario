namespace SistemaBiblioteca.Modelos;

/// <summary>
/// Representa el prestamo de un libro a un usuario.
/// </summary>
public class Prestamo
{
    public int Id { get; set; }
    public int LibroId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaLimite { get; set; }
    public DateTime? FechaDevolucion { get; set; }

    public bool Activo => FechaDevolucion is null;

    /// <summary>
    /// Indica si el prestamo esta vencido en la fecha indicada (por defecto, hoy).
    /// </summary>
    public bool EstaVencido(DateTime? fecha = null)
    {
        var referencia = fecha ?? DateTime.Today;
        return Activo && referencia > FechaLimite;
    }

    /// <summary>
    /// Calcula los dias de atraso respecto a la fecha indicada (0 si no hay atraso).
    /// </summary>
    public int DiasAtraso(DateTime? fecha = null)
    {
        var referencia = fecha ?? DateTime.Today;
        if (!EstaVencido(referencia))
        {
            return 0;
        }

        return (referencia - FechaLimite).Days;
    }

    public override string ToString()
    {
        var estado = Activo
            ? (EstaVencido() ? $"VENCIDO ({DiasAtraso()} dias de atraso)" : "Activo")
            : $"Devuelto el {FechaDevolucion:dd/MM/yyyy}";

        return $"[{Id}] Libro #{LibroId} -> Usuario #{UsuarioId} | Prestado: {FechaPrestamo:dd/MM/yyyy} | Limite: {FechaLimite:dd/MM/yyyy} | {estado}";
    }
}
