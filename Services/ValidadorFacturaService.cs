using System.Xml.Linq;
using RipsValidatorApi.Models;
using RipsValidatorApi.Validators;

namespace RipsValidatorApi.Services;

public class ValidadorFacturaService : IValidadorFacturaService
{
    private readonly FacturaXmlValidator _validator;

    public ValidadorFacturaService(FacturaXmlValidator validator)
    {
        _validator = validator;
    }

    public async Task<ValidacionResult> ValidarAsync(string xmlContent)
    {
        var resultado = new ValidacionResult();

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xmlContent);
        }
        catch (Exception ex)
        {
            resultado.Hallazgos.Add(new Hallazgo
            {
                Tipo = "ERROR",
                Codigo = "RVX000",
                Campo = "documento",
                Descripcion = $"El contenido no es XML válido: {ex.Message}"
            });
            resultado.TotalErrores = 1;
            resultado.Semaforo = "rojo";
            return resultado;
        }

        var hallazgos = await _validator.ValidarAsync(doc);
        resultado.Hallazgos.AddRange(hallazgos);
        resultado.TotalErrores = resultado.Hallazgos.Count(h => h.Tipo == "ERROR");
        resultado.TotalAdvertencias = resultado.Hallazgos.Count(h => h.Tipo == "ADVERTENCIA");
        resultado.Semaforo = resultado.TotalErrores > 0 ? "rojo"
            : resultado.TotalAdvertencias > 0 ? "amarillo"
            : "verde";

        return resultado;
    }
}
