using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RipsValidatorApi.Data.Entities;

[Table("SISPRO_CoberturaPlan")]
public class SisproCoberturaPlan
{
    [Key]
    [MaxLength(5)]
    public string Codigo { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Nombre { get; set; } = string.Empty;

    public bool Habilitado { get; set; }
}
