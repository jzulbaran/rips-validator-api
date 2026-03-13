namespace RipsValidatorApi.Models;

public class Hallazgo
{
    public string Tipo { get; set; } = string.Empty;        // "ERROR" | "ADVERTENCIA"
    public string Codigo { get; set; } = string.Empty;      // Ej: "RVC001"
    public string Campo { get; set; } = string.Empty;       // Campo RIPS afectado
    public string Descripcion { get; set; } = string.Empty; // Mensaje descriptivo
}
