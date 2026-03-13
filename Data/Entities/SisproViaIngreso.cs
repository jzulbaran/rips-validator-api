using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RipsValidatorApi.Data.Entities;

[Table("SISPRO_ViaIngreso")]
public class SisproViaIngreso
{
    [Key]
    [MaxLength(5)]
    public string Codigo { get; set; } = string.Empty;

    [MaxLength(200)]
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
}
