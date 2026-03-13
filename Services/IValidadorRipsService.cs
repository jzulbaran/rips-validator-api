using RipsValidatorApi.Models;

namespace RipsValidatorApi.Services;

public interface IValidadorRipsService
{
    Task<ValidacionResult> ValidarAsync(RipsDto rips);
}
