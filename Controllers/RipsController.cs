using Microsoft.AspNetCore.Mvc;
using RipsValidatorApi.Models;
using RipsValidatorApi.Services;

namespace RipsValidatorApi.Controllers;

[ApiController]
[Route("api/rips")]
public class RipsController : ControllerBase
{
    private readonly IValidadorRipsService _validadorService;

    public RipsController(IValidadorRipsService validadorService)
    {
        _validadorService = validadorService;
    }

    /// <summary>
    /// Valida un JSON RIPS completo según Resolución 2275/2023
    /// </summary>
    /// <param name="rips">JSON RIPS completo</param>
    /// <returns>Resultado de validación con semáforo (verde/amarillo/rojo) y lista de hallazgos</returns>
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
}
