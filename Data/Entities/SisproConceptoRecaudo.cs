using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RipsValidatorApi.Data.Entities;

[Table("SISPRO_ConceptoRecaudo")]
public class SisproConceptoRecaudo
{
    [Key]
    [MaxLength(5)]
    public string Codigo { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    public bool Habilitado { get; set; }
}
