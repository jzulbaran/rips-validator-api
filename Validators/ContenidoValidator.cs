using Microsoft.EntityFrameworkCore;
using RipsValidatorApi.Data;
using RipsValidatorApi.Models;

namespace RipsValidatorApi.Validators;

public class ContenidoValidator
{
    private readonly ValidadorDbContext _db;

    public ContenidoValidator(ValidadorDbContext db)
    {
        _db = db;
    }

    public async Task<List<Hallazgo>> ValidarAsync(RipsDto rips)
    {
        var hallazgos = new List<Hallazgo>();

        // Cargar tablas de referencia en memoria para evitar N+1
        var codsCie10 = (await _db.CIE10.Where(x => x.Habilitado).Select(x => x.Codigo).ToListAsync()).ToHashSet();
        var codsTipoUsuario = (await _db.TipoUsuario.Where(x => x.Habilitado).Select(x => x.Codigo).ToListAsync()).ToHashSet();
        var codsCobertura = (await _db.CoberturaPlan.Where(x => x.Habilitado).Select(x => x.Codigo).ToListAsync()).ToHashSet();
        var codsConcepto = (await _db.ConceptoRecaudo.Where(x => x.Habilitado).Select(x => x.Codigo).ToListAsync()).ToHashSet();
        var viaIngreso = await _db.ViaIngreso.Where(x => x.Habilitado).ToDictionaryAsync(x => x.Codigo);

        if (rips.Usuarios == null) return hallazgos;

        for (int i = 0; i < rips.Usuarios.Count; i++)
        {
            var u = rips.Usuarios[i];
            string prefijo = $"usuarios[{i}]";

            // codSexo
            if (!string.IsNullOrWhiteSpace(u.CodSexo) && !new[] { "M", "F", "U" }.Contains(u.CodSexo.ToUpper()))
                hallazgos.Add(Error("RVC001", $"{prefijo}.codSexo", $"Sexo '{u.CodSexo}' no válido. Use M, F o U"));

            // tipoUsuario
            if (!string.IsNullOrWhiteSpace(u.TipoUsuario) && !codsTipoUsuario.Contains(u.TipoUsuario))
                hallazgos.Add(Error("RVC002", $"{prefijo}.tipoUsuario", $"Tipo de usuario '{u.TipoUsuario}' no existe en tabla SISPRO"));

            // coberturaPlan
            if (!string.IsNullOrWhiteSpace(u.CoberturaPlan) && !codsCobertura.Contains(u.CoberturaPlan))
                hallazgos.Add(Error("RVC003", $"{prefijo}.coberturaPlan", $"Cobertura/plan '{u.CoberturaPlan}' no existe en tabla SISPRO"));

            // fechaNacimiento no futura
            if (DateTime.TryParseExact(u.FechaNacimiento, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var fechaNac))
            {
                if (fechaNac > DateTime.Today)
                    hallazgos.Add(Error("RVC010", $"{prefijo}.fechaNacimiento", "La fecha de nacimiento no puede ser futura"));
            }

            // Consultas
            if (u.ConsultasEfectivas != null)
            {
                for (int j = 0; j < u.ConsultasEfectivas.Count; j++)
                {
                    var c = u.ConsultasEfectivas[j];
                    string p = $"{prefijo}.consultas[{j}]";

                    ValidarCie10(c.CodDiagnosticoPrincipal, $"{p}.codDiagnosticoPrincipal", codsCie10, hallazgos, "RVC020");
                    ValidarCie10(c.CodDiagnosticoRelacionado1, $"{p}.codDiagnosticoRelacionado1", codsCie10, hallazgos, "RVC021");
                    ValidarCie10(c.CodDiagnosticoRelacionado2, $"{p}.codDiagnosticoRelacionado2", codsCie10, hallazgos, "RVC022");
                    ValidarCie10(c.CodDiagnosticoRelacionado3, $"{p}.codDiagnosticoRelacionado3", codsCie10, hallazgos, "RVC023");

                    if (!string.IsNullOrWhiteSpace(c.ViaIngresoUsuario) && !viaIngreso.ContainsKey(c.ViaIngresoUsuario))
                        hallazgos.Add(Error("RVC024", $"{p}.viaIngresoUsuario", $"Vía de ingreso '{c.ViaIngresoUsuario}' no existe en tabla SISPRO"));

                    if (!string.IsNullOrWhiteSpace(c.ConceptoRecaudo) && !codsConcepto.Contains(c.ConceptoRecaudo))
                        hallazgos.Add(Error("RVC025", $"{p}.conceptoRecaudo", $"Concepto de recaudo '{c.ConceptoRecaudo}' no existe en tabla SISPRO"));

                    ValidarFechaNoFutura(c.FechaInicioAtencion, $"{p}.fechaInicioAtencion", "RVC026", hallazgos);
                }
            }

            // Procedimientos
            if (u.ProcedimientosEfectivos != null)
            {
                for (int j = 0; j < u.ProcedimientosEfectivos.Count; j++)
                {
                    var pr = u.ProcedimientosEfectivos[j];
                    string p = $"{prefijo}.procedimientos[{j}]";

                    ValidarCie10(pr.CodDiagnosticoPrincipal, $"{p}.codDiagnosticoPrincipal", codsCie10, hallazgos, "RVC030");
                    ValidarCie10(pr.CodDiagnosticoRelacionado1, $"{p}.codDiagnosticoRelacionado1", codsCie10, hallazgos, "RVC031");
                    ValidarCie10(pr.CodComplicacion, $"{p}.codComplicacion", codsCie10, hallazgos, "RVC032");

                    if (!string.IsNullOrWhiteSpace(pr.ViaIngresoUsuario) && !viaIngreso.ContainsKey(pr.ViaIngresoUsuario))
                        hallazgos.Add(Error("RVC033", $"{p}.viaIngresoUsuario", $"Vía de ingreso '{pr.ViaIngresoUsuario}' no existe en tabla SISPRO"));

                    if (!string.IsNullOrWhiteSpace(pr.ConceptoRecaudo) && !codsConcepto.Contains(pr.ConceptoRecaudo))
                        hallazgos.Add(Error("RVC034", $"{p}.conceptoRecaudo", $"Concepto de recaudo '{pr.ConceptoRecaudo}' no existe en tabla SISPRO"));

                    ValidarFechaNoFutura(pr.FechaInicioAtencion, $"{p}.fechaInicioAtencion", "RVC035", hallazgos);
                }
            }

            // Hospitalización
            if (u.HospitalizacionEfectiva != null)
            {
                for (int j = 0; j < u.HospitalizacionEfectiva.Count; j++)
                {
                    var h = u.HospitalizacionEfectiva[j];
                    string p = $"{prefijo}.hospitalizacion[{j}]";

                    ValidarCie10(h.CodDiagnosticoPrincipal, $"{p}.codDiagnosticoPrincipal", codsCie10, hallazgos, "RVC040");
                    ValidarCie10(h.CodDiagnosticoRelacionado1, $"{p}.codDiagnosticoRelacionado1", codsCie10, hallazgos, "RVC041");
                    ValidarCie10(h.CodDiagnosticoRelacionado2, $"{p}.codDiagnosticoRelacionado2", codsCie10, hallazgos, "RVC042");
                    ValidarCie10(h.CodDiagnosticoRelacionado3, $"{p}.codDiagnosticoRelacionado3", codsCie10, hallazgos, "RVC043");

                    if (!string.IsNullOrWhiteSpace(h.ViaIngresoUsuario) && !viaIngreso.ContainsKey(h.ViaIngresoUsuario))
                        hallazgos.Add(Error("RVC044", $"{p}.viaIngresoUsuario", $"Vía de ingreso '{h.ViaIngresoUsuario}' no existe en tabla SISPRO"));

                    if (!string.IsNullOrWhiteSpace(h.ConceptoRecaudo) && !codsConcepto.Contains(h.ConceptoRecaudo))
                        hallazgos.Add(Error("RVC045", $"{p}.conceptoRecaudo", $"Concepto de recaudo '{h.ConceptoRecaudo}' no existe en tabla SISPRO"));
                }
            }
        }

        return hallazgos;
    }

    private static void ValidarCie10(string? codigo, string campo, HashSet<string> codsCie10, List<Hallazgo> hallazgos, string codigoHallazgo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return;
        var codigoUpper = codigo.ToUpper();
        if (!codsCie10.Contains(codigoUpper))
            hallazgos.Add(Error(codigoHallazgo, campo, $"Diagnóstico CIE-10 '{codigo}' no existe o no está habilitado en tabla SISPRO"));
    }

    private static void ValidarFechaNoFutura(string? valor, string campo, string codigoHallazgo, List<Hallazgo> hallazgos)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;
        if (DateTime.TryParse(valor, out var fecha) && fecha.Date > DateTime.Today)
            hallazgos.Add(Error(codigoHallazgo, campo, $"La fecha '{valor}' no puede ser futura"));
    }

    private static Hallazgo Error(string codigo, string campo, string descripcion) =>
        new() { Tipo = "ERROR", Codigo = codigo, Campo = campo, Descripcion = descripcion };
}
