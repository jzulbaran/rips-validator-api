using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RipsValidatorApi.Data.Entities;

[Table("SISPRO_FinalidadConsulta")]
public class SisproFinalidadConsulta
{
    [Key]
    public int Codigo { get; set; }

    [MaxLength(300)]
    public string Nombre { get; set; } = string.Empty;

    public bool Habilitado { get; set; }

    // "SI", "NO", "NA"
    [MaxLength(3)]
    public string AplicaConsultas { get; set; } = "NA";

    [MaxLength(3)]
    public string AplicaProcedimientos { get; set; } = "NA";

    [MaxLength(3)]
    public string AplicaUrgencias { get; set; } = "NA";

    [MaxLength(3)]
    public string AplicaHospitalizacion { get; set; } = "NA";

    // Sexo aplicable: "M", "F", "Z" (ambos)
    [MaxLength(3)]
    public string? SexoAplica { get; set; }

    public int EdadMinima { get; set; }
    public int EdadMaxima { get; set; }
}
