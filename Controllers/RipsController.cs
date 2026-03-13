using Microsoft.AspNetCore.Mvc;
using RipsValidatorApi.Models;
using RipsValidatorApi.Services;

namespace RipsValidatorApi.Controllers;

[ApiController]
[Route("api/rips")]
public class RipsController : ControllerBase
{
    private readonly IValidadorRipsService _validadorService;
    private readonly IServiceScopeFactory _scopeFactory;

    public RipsController(IValidadorRipsService validadorService, IServiceScopeFactory scopeFactory)
    {
        _validadorService = validadorService;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Valida un JSON RIPS completo según Resolución 2275/2023
    /// </summary>
    [HttpPost("validar")]
    [ProducesResponseType(typeof(ValidacionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Validar([FromBody] RipsDto? rips)
    {
        if (rips == null)
            return BadRequest(new { mensaje = "El cuerpo de la solicitud no puede estar vacío." });

        var resultado = await _validadorService.ValidarAsync(rips);
        return Ok(resultado);
    }

    /// <summary>
    /// Valida un lote de RIPS en una sola llamada.
    /// Cada ítem lleva un 'id' libre (ej: "fuente-numeroFactura") para correlacionar resultados.
    /// Máximo 100 facturas por lote.
    /// </summary>
    [HttpPost("validar-lote")]
    [ProducesResponseType(typeof(LoteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidarLote([FromBody] List<RipsLoteItem>? items)
    {
        if (items == null || items.Count == 0)
            return BadRequest(new { mensaje = "El lote no puede estar vacío." });

        if (items.Count > 100)
            return BadRequest(new { mensaje = "El lote no puede superar 100 facturas por llamada." });

        // Validar en paralelo — cada tarea crea su propio scope para evitar
        // conflictos de concurrencia en DbContext (que es Scoped)
        var semaphore = new SemaphoreSlim(10, 10);
        var tareas = items.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                if (item.Rips == null)
                    return new ValidacionLoteResult
                    {
                        Id = item.Id,
                        Semaforo = "rojo",
                        TotalErrores = 1,
                        Hallazgos = new List<Hallazgo>
                        {
                            new() { Tipo = "ERROR", Codigo = "RVE000", Campo = "rips",
                                    Descripcion = $"El ítem '{item.Id}' no contiene datos RIPS." }
                        }
                    };

                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IValidadorRipsService>();
                var resultado = await service.ValidarAsync(item.Rips);

                return new ValidacionLoteResult
                {
                    Id = item.Id,
                    Semaforo = resultado.Semaforo,
                    TotalErrores = resultado.TotalErrores,
                    TotalAdvertencias = resultado.TotalAdvertencias,
                    Hallazgos = resultado.Hallazgos
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var resultados = await Task.WhenAll(tareas);

        var lote = new LoteResult
        {
            TotalFacturas = resultados.Length,
            Verdes        = resultados.Count(r => r.Semaforo == "verde"),
            Amarillos     = resultados.Count(r => r.Semaforo == "amarillo"),
            Rojos         = resultados.Count(r => r.Semaforo == "rojo"),
            Resultados    = resultados.ToList()
        };

        return Ok(lote);
    }
}
