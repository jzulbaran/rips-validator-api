using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RipsValidatorApi.Data.Entities;

[Table("SISPRO_CoberturaModalidadCruzada")]
public class SisproCoberturaModalidadCruzada
{
    [Key]
    public int Id { get; set; }

    [MaxLength(5)]
    public string CodigoCoberturaplan { get; set; } = string.Empty;

    [MaxLength(5)]
    public string CodigoModalidadPago { get; set; } = string.Empty;

    [MaxLength(5)]
    public string CodigoTipoUsuario { get; set; } = string.Empty;

    [MaxLength(5)]
    public string CodigoConceptoRecaudo { get; set; } = string.Empty;

    public bool Permitido { get; set; }
}
