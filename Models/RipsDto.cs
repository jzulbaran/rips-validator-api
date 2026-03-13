using System.Text.Json.Serialization;

namespace RipsValidatorApi.Models;

// ---- Raíz del JSON RIPS (Res. 2275/2023) ----
public class RipsDto
{
    public string? NumDocumentoIdObligado { get; set; }
    public string? NumFactura { get; set; }
    public string? TipoNota { get; set; }
    public string? NumNota { get; set; }
    public List<UsuarioRips>? Usuarios { get; set; }
}

public class UsuarioRips
{
    public string? TipoDocumentoIdentificacion { get; set; }
    public string? NumDocumentoIdentificacion { get; set; }
    public string? FechaNacimiento { get; set; }
    public string? CodSexo { get; set; }
    public string? CodPaisResidencia { get; set; }
    public string? CodMunicipioResidencia { get; set; }
    public string? CodZonaTerritorialResidencia { get; set; }
    public string? Incapacidad { get; set; }
    public string? CodPaisOrigen { get; set; }
    public string? TipoUsuario { get; set; }
    public string? FechaInicioAtencion { get; set; }
    public string? NumAutorizacion { get; set; }
    public string? IdMIPRES { get; set; }
    public string? FechaFinAtencion { get; set; }
    public string? CoberturaPlan { get; set; }
    public string? NumPolizaAfiliado { get; set; }
    public string? CopagoCuotaModeradora { get; set; }
    public string? NumFEVIPACAP { get; set; }
    public List<Consulta>? Consultas { get; set; }
    public List<Procedimiento>? Procedimientos { get; set; }
    public List<HospitalizacionRips>? Hospitalizacion { get; set; }
    public List<Medicamento>? Medicamentos { get; set; }
    public List<OtroServicio>? OtrosServicios { get; set; }
}

public class Consulta
{
    public string? FechaInicioAtencion { get; set; }
    public string? NumAutorizacion { get; set; }
    public string? IdMIPRES { get; set; }
    public string? CodConsulta { get; set; }
    public string? ModalidadGrupoServicioTecSal { get; set; }
    public string? GrupoServicios { get; set; }
    public string? CodServicio { get; set; }
    public string? FinalidadTecnologiaSalud { get; set; }
    public string? CausaMotivoAtencion { get; set; }
    public string? CodDiagnosticoPrincipal { get; set; }
    public string? CodDiagnosticoRelacionado1 { get; set; }
    public string? CodDiagnosticoRelacionado2 { get; set; }
    public string? CodDiagnosticoRelacionado3 { get; set; }
    public string? TipoDiagnosticoPrincipal { get; set; }
    public string? TipoDocumentoIdentificacion { get; set; }
    public string? NumDocumentoIdentificacion { get; set; }
    public string? ViaIngresoUsuario { get; set; }
    public decimal? ValorPagoModerador { get; set; }
    public decimal? ValorNetoPagar { get; set; }
    public string? ConceptoRecaudo { get; set; }
    public string? PagoCompartido { get; set; }
}

public class Procedimiento
{
    public string? FechaInicioAtencion { get; set; }
    public string? IdMIPRES { get; set; }
    public string? NumAutorizacion { get; set; }
    public string? CodProcedimiento { get; set; }
    public string? ModalidadGrupoServicioTecSal { get; set; }
    public string? GrupoServicios { get; set; }
    public string? CodServicio { get; set; }
    public string? FinalidadTecnologiaSalud { get; set; }
    public string? TipoDocumentoIdentificacion { get; set; }
    public string? NumDocumentoIdentificacion { get; set; }
    public string? ViaIngresoUsuario { get; set; }
    public string? CodDiagnosticoPrincipal { get; set; }
    public string? CodDiagnosticoRelacionado1 { get; set; }
    public string? CodComplicacion { get; set; }
    public decimal? ValorPagoModerador { get; set; }
    public decimal? ValorNetoPagar { get; set; }
    public string? ConceptoRecaudo { get; set; }
}

public class HospitalizacionRips
{
    public string? FechaInicioAtencion { get; set; }
    public string? FechaFinAtencion { get; set; }
    public string? NumAutorizacion { get; set; }
    public string? CausaMotivoAtencion { get; set; }
    public string? CodDiagnosticoPrincipal { get; set; }
    public string? CodDiagnosticoCausaMuerte { get; set; }
    public string? CodDiagnosticoRelacionado1 { get; set; }
    public string? CodDiagnosticoRelacionado2 { get; set; }
    public string? CodDiagnosticoRelacionado3 { get; set; }
    public string? ModalidadGrupoServicioTecSal { get; set; }
    public string? GrupoServicios { get; set; }
    public string? CodServicio { get; set; }
    public string? TipoDocumentoIdentificacion { get; set; }
    public string? NumDocumentoIdentificacion { get; set; }
    public string? ViaIngresoUsuario { get; set; }
    public decimal? ValorPagoModerador { get; set; }
    public decimal? ValorNetoPagar { get; set; }
    public string? ConceptoRecaudo { get; set; }
}

public class Medicamento
{
    public string? FechaDispensacionMedicamento { get; set; }
    public string? NumAutorizacion { get; set; }
    public string? IdMIPRES { get; set; }
    public string? CodTecnologiaSalud { get; set; }
    public string? NomTecnologiaSalud { get; set; }
    public string? ConcentracionMedicamento { get; set; }
    public string? UnidadMedidaMedicamento { get; set; }
    public string? FormaFarmaceutica { get; set; }
    public string? NomGenericoMedicamento { get; set; }
    public string? TipoMedicamento { get; set; }
    public decimal? CantidadMedicamento { get; set; }
    public decimal? ValorUnitario { get; set; }
    public decimal? ValorTotal { get; set; }
    public decimal? ValorPagoModerador { get; set; }
    public string? ConceptoRecaudo { get; set; }
}

public class OtroServicio
{
    public string? FechaInicioAtencion { get; set; }
    public string? NumAutorizacion { get; set; }
    public string? IdMIPRES { get; set; }
    public string? CodTecnologiaSalud { get; set; }
    public string? NomTecnologiaSalud { get; set; }
    public string? ModalidadGrupoServicioTecSal { get; set; }
    public string? GrupoServicios { get; set; }
    public string? CodServicio { get; set; }
    public string? FinalidadTecnologiaSalud { get; set; }
    public string? TipoDocumentoIdentificacion { get; set; }
    public string? NumDocumentoIdentificacion { get; set; }
    public decimal? CantidadOS { get; set; }
    public decimal? ValorUnitOS { get; set; }
    public decimal? ValorTotalOS { get; set; }
    public decimal? ValorPagoModerador { get; set; }
    public string? ConceptoRecaudo { get; set; }
}
