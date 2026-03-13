using RipsValidatorApi.Models;

namespace RipsValidatorApi.Services;

public interface IValidadorFacturaService
{
    Task<ValidacionResult> ValidarAsync(string xmlContent);
}
