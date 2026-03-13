namespace RipsValidatorApi.Models;

public class RipsLoteItem
{
    /// <summary>Identificador libre para correlacionar el resultado con la factura (ej: "fuente-numero")</summary>
    public string Id { get; set; } = string.Empty;
    public RipsDto? Rips { get; set; }
}

public class ValidacionLoteResult
{
    public string Id { get; set; } = string.Empty;
    public string Semaforo { get; set; } = "verde";
    public int TotalErrores { get; set; }
    public int TotalAdvertencias { get; set; }
    public List<Hallazgo> Hallazgos { get; set; } = new();
}

public class LoteResult
{
    public int TotalFacturas { get; set; }
    public int Verdes { get; set; }
    public int Amarillos { get; set; }
    public int Rojos { get; set; }
    public List<ValidacionLoteResult> Resultados { get; set; } = new();
}
