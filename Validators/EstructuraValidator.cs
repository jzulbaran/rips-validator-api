using RipsValidatorApi.Models;

namespace RipsValidatorApi.Validators;

public class EstructuraValidator
{
    public List<Hallazgo> Validar(RipsDto rips)
    {
        var hallazgos = new List<Hallazgo>();

        // Campos obligatorios raíz
        if (string.IsNullOrWhiteSpace(rips.NumDocumentoIdObligado))
            hallazgos.Add(Error("RVE001", "numDocumentoIdObligado", "Campo obligatorio ausente: numDocumentoIdObligado"));

        if (string.IsNullOrWhiteSpace(rips.NumFactura))
            hallazgos.Add(Error("RVE002", "numFactura", "Campo obligatorio ausente: numFactura"));

        if (rips.Usuarios == null || rips.Usuarios.Count == 0)
        {
            hallazgos.Add(Error("RVE003", "usuarios", "El RIPS debe contener al menos un usuario"));
            return hallazgos;
        }

        for (int i = 0; i < rips.Usuarios.Count; i++)
        {
            var u = rips.Usuarios[i];
            string prefijo = $"usuarios[{i}]";

            if (string.IsNullOrWhiteSpace(u.TipoDocumentoIdentificacion))
                hallazgos.Add(Error("RVE010", $"{prefijo}.tipoDocumentoIdentificacion", "Campo obligatorio ausente"));

            if (string.IsNullOrWhiteSpace(u.NumDocumentoIdentificacion))
                hallazgos.Add(Error("RVE011", $"{prefijo}.numDocumentoIdentificacion", "Campo obligatorio ausente"));

            if (string.IsNullOrWhiteSpace(u.FechaNacimiento))
                hallazgos.Add(Error("RVE012", $"{prefijo}.fechaNacimiento", "Campo obligatorio ausente"));
            else if (!EsFechaValida(u.FechaNacimiento, out _))
                hallazgos.Add(Error("RVE013", $"{prefijo}.fechaNacimiento", $"Formato de fecha inválido: '{u.FechaNacimiento}'. Use YYYY-MM-DD"));

            if (string.IsNullOrWhiteSpace(u.CodSexo))
                hallazgos.Add(Error("RVE014", $"{prefijo}.codSexo", "Campo obligatorio ausente"));

            if (string.IsNullOrWhiteSpace(u.TipoUsuario))
                hallazgos.Add(Error("RVE015", $"{prefijo}.tipoUsuario", "Campo obligatorio ausente"));

            // coberturaPlan es obligatorio solo si no viene anidado en servicios
            if (string.IsNullOrWhiteSpace(u.CoberturaPlan) && u.Servicios == null)
                hallazgos.Add(Error("RVE016", $"{prefijo}.coberturaPlan", "Campo obligatorio ausente"));

            // fechaInicioAtencion es obligatoria a nivel usuario solo si no viene en servicios
            if (!string.IsNullOrWhiteSpace(u.FechaInicioAtencion))
            {
                if (!EsFechaValida(u.FechaInicioAtencion, out _) && !EsFechaHoraValida(u.FechaInicioAtencion, out _))
                    hallazgos.Add(Error("RVE018", $"{prefijo}.fechaInicioAtencion", $"Formato de fecha inválido: '{u.FechaInicioAtencion}'"));
            }

            // Validar servicios
            ValidarConsultas(u.ConsultasEfectivas, $"{prefijo}.consultas", hallazgos);
            ValidarProcedimientos(u.ProcedimientosEfectivos, $"{prefijo}.procedimientos", hallazgos);
            ValidarHospitalizacion(u.HospitalizacionEfectiva, $"{prefijo}.hospitalizacion", hallazgos);
            ValidarMedicamentos(u.MedicamentosEfectivos, $"{prefijo}.medicamentos", hallazgos);
            ValidarOtrosServicios(u.OtrosServiciosEfectivos, $"{prefijo}.otrosServicios", hallazgos);
        }

        return hallazgos;
    }

    private void ValidarConsultas(List<Consulta>? consultas, string prefijo, List<Hallazgo> hallazgos)
    {
        if (consultas == null) return;
        for (int i = 0; i < consultas.Count; i++)
        {
            var c = consultas[i];
            string p = $"{prefijo}[{i}]";

            if (string.IsNullOrWhiteSpace(c.FechaInicioAtencion))
                hallazgos.Add(Error("RVE020", $"{p}.fechaInicioAtencion", "Campo obligatorio ausente en consulta"));

            if (string.IsNullOrWhiteSpace(c.CodDiagnosticoPrincipal))
                hallazgos.Add(Error("RVE021", $"{p}.codDiagnosticoPrincipal", "Campo obligatorio ausente en consulta"));

            if (c.ValorNetoPagar.HasValue && c.ValorNetoPagar < 0)
                hallazgos.Add(Error("RVE022", $"{p}.valorNetoPagar", "El valor neto a pagar no puede ser negativo"));

            if (c.ValorPagoModerador.HasValue && c.ValorPagoModerador < 0)
                hallazgos.Add(Error("RVE023", $"{p}.valorPagoModerador", "El valor del pago moderador no puede ser negativo"));
        }
    }

    private void ValidarProcedimientos(List<Procedimiento>? procedimientos, string prefijo, List<Hallazgo> hallazgos)
    {
        if (procedimientos == null) return;
        for (int i = 0; i < procedimientos.Count; i++)
        {
            var p2 = procedimientos[i];
            string p = $"{prefijo}[{i}]";

            if (string.IsNullOrWhiteSpace(p2.FechaInicioAtencion))
                hallazgos.Add(Error("RVE030", $"{p}.fechaInicioAtencion", "Campo obligatorio ausente en procedimiento"));

            if (string.IsNullOrWhiteSpace(p2.CodProcedimiento))
                hallazgos.Add(Error("RVE031", $"{p}.codProcedimiento", "Campo obligatorio ausente en procedimiento"));

            if (p2.ValorNetoPagar.HasValue && p2.ValorNetoPagar < 0)
                hallazgos.Add(Error("RVE032", $"{p}.valorNetoPagar", "El valor neto a pagar no puede ser negativo"));
        }
    }

    private void ValidarHospitalizacion(List<HospitalizacionRips>? hospitalizacion, string prefijo, List<Hallazgo> hallazgos)
    {
        if (hospitalizacion == null) return;
        for (int i = 0; i < hospitalizacion.Count; i++)
        {
            var h = hospitalizacion[i];
            string p = $"{prefijo}[{i}]";

            if (string.IsNullOrWhiteSpace(h.FechaInicioAtencion))
                hallazgos.Add(Error("RVE040", $"{p}.fechaInicioAtencion", "Campo obligatorio ausente en hospitalización"));

            if (string.IsNullOrWhiteSpace(h.CodDiagnosticoPrincipal))
                hallazgos.Add(Error("RVE041", $"{p}.codDiagnosticoPrincipal", "Campo obligatorio ausente en hospitalización"));

            if (!string.IsNullOrWhiteSpace(h.FechaInicioAtencion) && !string.IsNullOrWhiteSpace(h.FechaFinAtencion))
            {
                if (EsFechaValida(h.FechaInicioAtencion, out var inicio) && EsFechaValida(h.FechaFinAtencion, out var fin))
                {
                    if (fin < inicio)
                        hallazgos.Add(Error("RVE042", $"{p}.fechaFinAtencion", "La fecha fin de atención no puede ser anterior a la fecha inicio"));
                }
            }
        }
    }

    private void ValidarMedicamentos(List<Medicamento>? medicamentos, string prefijo, List<Hallazgo> hallazgos)
    {
        if (medicamentos == null) return;
        for (int i = 0; i < medicamentos.Count; i++)
        {
            var m = medicamentos[i];
            string p = $"{prefijo}[{i}]";

            if (string.IsNullOrWhiteSpace(m.CodTecnologiaSalud))
                hallazgos.Add(Error("RVE050", $"{p}.codTecnologiaSalud", "Campo obligatorio ausente en medicamento"));

            if (m.CantidadMedicamento.HasValue && m.CantidadMedicamento <= 0)
                hallazgos.Add(Error("RVE051", $"{p}.cantidadMedicamento", "La cantidad debe ser mayor que cero"));

            if (m.ValorUnitario.HasValue && m.ValorUnitario < 0)
                hallazgos.Add(Error("RVE052", $"{p}.valorUnitario", "El valor unitario no puede ser negativo"));

            if (m.ValorTotal.HasValue && m.ValorTotal < 0)
                hallazgos.Add(Error("RVE053", $"{p}.valorTotal", "El valor total no puede ser negativo"));
        }
    }

    private void ValidarOtrosServicios(List<OtroServicio>? otros, string prefijo, List<Hallazgo> hallazgos)
    {
        if (otros == null) return;
        for (int i = 0; i < otros.Count; i++)
        {
            var o = otros[i];
            string p = $"{prefijo}[{i}]";

            if (string.IsNullOrWhiteSpace(o.CodTecnologiaSalud))
                hallazgos.Add(Error("RVE060", $"{p}.codTecnologiaSalud", "Campo obligatorio ausente en otros servicios"));

            if (o.CantidadOS.HasValue && o.CantidadOS <= 0)
                hallazgos.Add(Error("RVE061", $"{p}.cantidadOS", "La cantidad debe ser mayor que cero"));

            if (o.ValorTotalOS.HasValue && o.ValorTotalOS < 0)
                hallazgos.Add(Error("RVE062", $"{p}.valorTotalOS", "El valor total no puede ser negativo"));
        }
    }

    private static bool EsFechaValida(string? valor, out DateTime fecha)
    {
        fecha = default;
        if (string.IsNullOrWhiteSpace(valor)) return false;
        return DateTime.TryParseExact(valor, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out fecha);
    }

    private static bool EsFechaHoraValida(string? valor, out DateTime fecha)
    {
        fecha = default;
        if (string.IsNullOrWhiteSpace(valor)) return false;
        return DateTime.TryParse(valor, out fecha);
    }

    private static Hallazgo Error(string codigo, string campo, string descripcion) =>
        new() { Tipo = "ERROR", Codigo = codigo, Campo = campo, Descripcion = descripcion };
}
