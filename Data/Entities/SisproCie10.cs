using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RipsValidatorApi.Data.Entities;

[Table("SISPRO_CIE10")]
public class SisproCie10
{
    [Key]
    [MaxLength(10)]
    public string Codigo { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Nombre { get; set; } = string.Empty;

    public bool Habilitado { get; set; }

    // 1=Masculino, 2=Femenino, 3=Ambos
    public int? AplicaASexo { get; set; }

    public int EdadMinima { get; set; }
    public int EdadMaxima { get; set; }
}
