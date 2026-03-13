using Microsoft.AspNetCore.Mvc;
using RipsValidatorApi.Models;
using RipsValidatorApi.Services;

namespace RipsValidatorApi.Controllers;

[ApiController]
[Route("api/factura")]
public class FacturaController : ControllerBase
{
    private readonly IValidadorFacturaService _validadorService;

    public FacturaController(IValidadorFacturaService validadorService)
    {
        _validadorService = validadorService;
    }

    /// <summary>
    /// Valida una Factura Electrónica DIAN (UBL 2.1) para el sector salud.
    /// Acepta el XML completo (AttachedDocument) o el Invoice directamente.
    /// </summary>
    /// <returns>Resultado de validación con semáforo y lista de hallazgos</returns>
    [HttpPost("validar")]
    [Consumes("application/xml", "text/xml")]
    [ProducesResponseType(typeof(ValidacionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Validar()
    {
        using var reader = new StreamReader(Request.Body);
        var xmlContent = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(xmlContent))
            return BadRequest(new { mensaje = "El cuerpo de la solicitud no puede estar vacío." });

        var resultado = await _validadorService.ValidarAsync(xmlContent);
        return Ok(resultado);
    }
}
