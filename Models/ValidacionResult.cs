namespace RipsValidatorApi.Models;

public class ValidacionResult
{
    public string Semaforo { get; set; } = "verde";  // "rojo" | "amarillo" | "verde"
    public int TotalErrores { get; set; }
    public int TotalAdvertencias { get; set; }
    public List<Hallazgo> Hallazgos { get; set; } = new();
}
