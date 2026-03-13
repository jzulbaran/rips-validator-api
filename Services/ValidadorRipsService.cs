using RipsValidatorApi.Models;
using RipsValidatorApi.Validators;

namespace RipsValidatorApi.Services;

public class ValidadorRipsService : IValidadorRipsService
{
    private readonly EstructuraValidator _estructuraValidator;
    private readonly ContenidoValidator _contenidoValidator;
    private readonly RelacionValidator _relacionValidator;

    public ValidadorRipsService(
        EstructuraValidator estructuraValidator,
        ContenidoValidator contenidoValidator,
        RelacionValidator relacionValidator)
    {
        _estructuraValidator = estructuraValidator;
        _contenidoValidator = contenidoValidator;
        _relacionValidator = relacionValidator;
    }

    public async Task<ValidacionResult> ValidarAsync(RipsDto rips)
    {
        var resultado = new ValidacionResult();

        // 1. Validaciones de estructura (síncronas)
        var hallazgosEstructura = _estructuraValidator.Validar(rips);
        resultado.Hallazgos.AddRange(hallazgosEstructura);

        // Si hay errores de estructura graves, aún ejecutamos contenido y relación
        // para dar un reporte completo en un solo llamado

        // 2. Validaciones de contenido contra tablas SISPRO
        var hallazgosContenido = await _contenidoValidator.ValidarAsync(rips);
        resultado.Hallazgos.AddRange(hallazgosContenido);

        // 3. Validaciones de relación cruzada
        var hallazgosRelacion = await _relacionValidator.ValidarAsync(rips);
        resultado.Hallazgos.AddRange(hallazgosRelacion);

        // Calcular totales y semáforo
        resultado.TotalErrores = resultado.Hallazgos.Count(h => h.Tipo == "ERROR");
        resultado.TotalAdvertencias = resultado.Hallazgos.Count(h => h.Tipo == "ADVERTENCIA");

        resultado.Semaforo = resultado.TotalErrores > 0 ? "rojo"
            : resultado.TotalAdvertencias > 0 ? "amarillo"
            : "verde";

        return resultado;
    }
}
