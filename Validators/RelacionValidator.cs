using Microsoft.EntityFrameworkCore;
using RipsValidatorApi.Data;
using RipsValidatorApi.Models;

namespace RipsValidatorApi.Validators;

public class RelacionValidator
{
    private readonly ValidadorDbContext _db;

    public RelacionValidator(ValidadorDbContext db)
    {
        _db = db;
    }

    public async Task<List<Hallazgo>> ValidarAsync(RipsDto rips)
    {
        var hallazgos = new List<Hallazgo>();

        var cie10Map = await _db.CIE10
            .Where(x => x.Habilitado)
            .ToDictionaryAsync(x => x.Codigo);

        var finalidades = await _db.FinalidadConsulta
            .Where(x => x.Habilitado)
            .ToDictionaryAsync(x => x.Codigo);

        var viaIngreso = await _db.ViaIngreso
            .Where(x => x.Habilitado)
            .ToDictionaryAsync(x => x.Codigo);

        var cruzadas = await _db.CoberturaModalidadCruzada.ToListAsync();

        if (rips.Usuarios == null) return hallazgos;

        for (int i = 0; i < rips.Usuarios.Count; i++)
        {
            var u = rips.Usuarios[i];
            string prefijo = $"usuarios[{i}]";

            // Calcular edad del paciente
            int edadPaciente = 0;
            bool tieneFechaNac = DateTime.TryParseExact(u.FechaNacimiento, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var fechaNac);
            if (tieneFechaNac)
                edadPaciente = CalcularEdad(fechaNac);

            string sexoPaciente = u.CodSexo?.ToUpper() ?? "U";

            // Validar combinación cobertura + modalidad + tipoUsuario + conceptoRecaudo (a nivel usuario)
            // Se toma el conceptoRecaudo del primer servicio encontrado si existe
            string? conceptoRecaudo = ObtenerPrimerConceptoRecaudo(u);

            // ---- Validaciones en Consultas ----
            if (u.ConsultasEfectivas != null)
            {
                for (int j = 0; j < u.ConsultasEfectivas.Count; j++)
                {
                    var c = u.ConsultasEfectivas[j];
                    string p = $"{prefijo}.consultas[{j}]";

                    // CIE10 sexo vs codSexo
                    if (!string.IsNullOrWhiteSpace(c.CodDiagnosticoPrincipal))
                    {
                        var codUpper = c.CodDiagnosticoPrincipal.ToUpper();
                        if (cie10Map.TryGetValue(codUpper, out var diag))
                        {
                            ValidarCie10Sexo(diag.AplicaASexo, sexoPaciente, $"{p}.codDiagnosticoPrincipal", c.CodDiagnosticoPrincipal, hallazgos, "RVR001");

                            if (tieneFechaNac)
                                ValidarCie10Edad(diag.EdadMinima, diag.EdadMaxima, edadPaciente, $"{p}.codDiagnosticoPrincipal", c.CodDiagnosticoPrincipal, hallazgos, "RVR002");
                        }
                    }

                    // viaIngreso permitido para consulta
                    if (!string.IsNullOrWhiteSpace(c.ViaIngresoUsuario) && viaIngreso.TryGetValue(c.ViaIngresoUsuario, out var via))
                    {
                        if (via.AplicaConsultas == "NO")
                            hallazgos.Add(Error("RVR010", $"{p}.viaIngresoUsuario",
                                $"La vía de ingreso '{c.ViaIngresoUsuario}' ({via.Nombre}) no aplica para consultas"));
                    }

                    // finalidadConsulta
                    if (!string.IsNullOrWhiteSpace(c.FinalidadTecnologiaSalud) &&
                        int.TryParse(c.FinalidadTecnologiaSalud, out int codFin) &&
                        finalidades.TryGetValue(codFin, out var fin))
                    {
                        if (fin.AplicaConsultas == "NO")
                            hallazgos.Add(Advertencia("RVR020", $"{p}.finalidadTecnologiaSalud",
                                $"La finalidad '{c.FinalidadTecnologiaSalud}' ({fin.Nombre}) no aplica para consultas"));

                        if (!string.IsNullOrWhiteSpace(fin.SexoAplica) && fin.SexoAplica != "Z")
                        {
                            string sexoFin = fin.SexoAplica == "M" ? "M" : "F";
                            if (sexoPaciente != "U" && sexoPaciente != sexoFin)
                                hallazgos.Add(Advertencia("RVR021", $"{p}.finalidadTecnologiaSalud",
                                    $"La finalidad '{c.FinalidadTecnologiaSalud}' aplica solo para sexo {sexoFin}, paciente es {sexoPaciente}"));
                        }

                        if (tieneFechaNac && (edadPaciente < fin.EdadMinima || edadPaciente > fin.EdadMaxima))
                            hallazgos.Add(Advertencia("RVR022", $"{p}.finalidadTecnologiaSalud",
                                $"La finalidad '{c.FinalidadTecnologiaSalud}' aplica para edades {fin.EdadMinima}-{fin.EdadMaxima}, paciente tiene {edadPaciente} años"));
                    }
                }
            }

            // ---- Validaciones en Procedimientos ----
            if (u.ProcedimientosEfectivos != null)
            {
                for (int j = 0; j < u.ProcedimientosEfectivos.Count; j++)
                {
                    var pr = u.ProcedimientosEfectivos[j];
                    string p = $"{prefijo}.procedimientos[{j}]";

                    if (!string.IsNullOrWhiteSpace(pr.CodDiagnosticoPrincipal))
                    {
                        var codUpper = pr.CodDiagnosticoPrincipal.ToUpper();
                        if (cie10Map.TryGetValue(codUpper, out var diag))
                        {
                            ValidarCie10Sexo(diag.AplicaASexo, sexoPaciente, $"{p}.codDiagnosticoPrincipal", pr.CodDiagnosticoPrincipal, hallazgos, "RVR003");
                            if (tieneFechaNac)
                                ValidarCie10Edad(diag.EdadMinima, diag.EdadMaxima, edadPaciente, $"{p}.codDiagnosticoPrincipal", pr.CodDiagnosticoPrincipal, hallazgos, "RVR004");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(pr.ViaIngresoUsuario) && viaIngreso.TryGetValue(pr.ViaIngresoUsuario, out var via))
                    {
                        if (via.AplicaProcedimientos == "NO")
                            hallazgos.Add(Error("RVR011", $"{p}.viaIngresoUsuario",
                                $"La vía de ingreso '{pr.ViaIngresoUsuario}' ({via.Nombre}) no aplica para procedimientos"));
                    }
                }
            }

            // ---- Validaciones en Hospitalización ----
            if (u.HospitalizacionEfectiva != null)
            {
                for (int j = 0; j < u.HospitalizacionEfectiva.Count; j++)
                {
                    var h = u.HospitalizacionEfectiva[j];
                    string p = $"{prefijo}.hospitalizacion[{j}]";

                    if (!string.IsNullOrWhiteSpace(h.CodDiagnosticoPrincipal))
                    {
                        var codUpper = h.CodDiagnosticoPrincipal.ToUpper();
                        if (cie10Map.TryGetValue(codUpper, out var diag))
                        {
                            ValidarCie10Sexo(diag.AplicaASexo, sexoPaciente, $"{p}.codDiagnosticoPrincipal", h.CodDiagnosticoPrincipal, hallazgos, "RVR005");
                            if (tieneFechaNac)
                                ValidarCie10Edad(diag.EdadMinima, diag.EdadMaxima, edadPaciente, $"{p}.codDiagnosticoPrincipal", h.CodDiagnosticoPrincipal, hallazgos, "RVR006");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(h.ViaIngresoUsuario) && viaIngreso.TryGetValue(h.ViaIngresoUsuario, out var via))
                    {
                        if (via.AplicaHospitalizacion == "NO")
                            hallazgos.Add(Error("RVR012", $"{p}.viaIngresoUsuario",
                                $"La vía de ingreso '{h.ViaIngresoUsuario}' ({via.Nombre}) no aplica para hospitalización"));
                    }
                }
            }
        }

        return hallazgos;
    }

    private static string? ObtenerPrimerConceptoRecaudo(UsuarioRips u)
    {
        if (u.ConsultasEfectivas?.Any(x => x.ConceptoRecaudo != null) == true)
            return u.ConsultasEfectivas.First(x => x.ConceptoRecaudo != null).ConceptoRecaudo;
        if (u.ProcedimientosEfectivos?.Any(x => x.ConceptoRecaudo != null) == true)
            return u.ProcedimientosEfectivos.First(x => x.ConceptoRecaudo != null).ConceptoRecaudo;
        if (u.HospitalizacionEfectiva?.Any(x => x.ConceptoRecaudo != null) == true)
            return u.HospitalizacionEfectiva.First(x => x.ConceptoRecaudo != null).ConceptoRecaudo;
        return null;
    }

    private static void ValidarCie10Sexo(int? aplicaASexo, string sexoPaciente, string campo, string codigoCie10,
        List<Hallazgo> hallazgos, string codigoHallazgo)
    {
        if (aplicaASexo == null || aplicaASexo == 3 || sexoPaciente == "U") return;
        // 1=Masculino, 2=Femenino
        bool soloMasculino = aplicaASexo == 1;
        bool soloFemenino = aplicaASexo == 2;

        if (soloMasculino && sexoPaciente == "F")
            hallazgos.Add(Advertencia(codigoHallazgo, campo,
                $"El diagnóstico '{codigoCie10}' aplica solo para sexo masculino, pero el paciente es femenino"));

        if (soloFemenino && sexoPaciente == "M")
            hallazgos.Add(Advertencia(codigoHallazgo, campo,
                $"El diagnóstico '{codigoCie10}' aplica solo para sexo femenino, pero el paciente es masculino"));
    }

    private static void ValidarCie10Edad(int edadMin, int edadMax, int edadPaciente, string campo, string codigoCie10,
        List<Hallazgo> hallazgos, string codigoHallazgo)
    {
        if (edadPaciente < edadMin || edadPaciente > edadMax)
            hallazgos.Add(Advertencia(codigoHallazgo, campo,
                $"El diagnóstico '{codigoCie10}' aplica para edades {edadMin}-{edadMax}, pero el paciente tiene {edadPaciente} años"));
    }

    private static int CalcularEdad(DateTime fechaNacimiento)
    {
        var hoy = DateTime.Today;
        int edad = hoy.Year - fechaNacimiento.Year;
        if (fechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
        return edad;
    }

    private static Hallazgo Error(string codigo, string campo, string descripcion) =>
        new() { Tipo = "ERROR", Codigo = codigo, Campo = campo, Descripcion = descripcion };

    private static Hallazgo Advertencia(string codigo, string campo, string descripcion) =>
        new() { Tipo = "ADVERTENCIA", Codigo = codigo, Campo = campo, Descripcion = descripcion };
}
