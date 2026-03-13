using System.Xml.Linq;
using RipsValidatorApi.Data;
using RipsValidatorApi.Models;
using Microsoft.EntityFrameworkCore;

namespace RipsValidatorApi.Validators;

/// <summary>
/// Valida una Factura Electrónica DIAN (UBL 2.1) para el sector salud.
/// El XML puede ser el AttachedDocument completo o el Invoice directamente.
/// </summary>
public class FacturaXmlValidator
{
    // Namespaces UBL/DIAN
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Sts = "dian:gov:co:facturaelectronica:Structures-2-1";
    private static readonly XNamespace InvNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";

    private readonly ValidadorDbContext _db;

    public FacturaXmlValidator(ValidadorDbContext db)
    {
        _db = db;
    }

    public async Task<List<Hallazgo>> ValidarAsync(XDocument doc)
    {
        var hallazgos = new List<Hallazgo>();

        // Extraer el Invoice (puede venir como AttachedDocument o directo)
        var invoice = ExtraerInvoice(doc);
        if (invoice == null)
        {
            hallazgos.Add(Error("RVX001", "documento", "No se encontró el elemento Invoice en el XML. Verifique que sea una Factura Electrónica DIAN válida."));
            return hallazgos;
        }

        // Extraer el ApplicationResponse (validación DIAN)
        var appResponse = ExtraerApplicationResponse(doc);

        ValidarEstructura(invoice, hallazgos);
        ValidarFechas(invoice, hallazgos);
        await ValidarSalud(invoice, hallazgos);
        ValidarFinanciero(invoice, hallazgos);
        ValidarLineas(invoice, hallazgos);

        if (appResponse != null)
            ValidarEstadoDian(appResponse, hallazgos);
        else
            hallazgos.Add(Advertencia("RVX050", "applicationResponse", "El XML no contiene respuesta de validación DIAN (ApplicationResponse). Verifique que la factura haya sido validada."));

        return hallazgos;
    }

    // ─── Extracción ───────────────────────────────────────────────────────────

    private static XElement? ExtraerInvoice(XDocument doc)
    {
        // Caso 1: el root ya es el Invoice
        if (doc.Root?.Name.LocalName == "Invoice")
            return doc.Root;

        // Caso 2: AttachedDocument con Invoice en CDATA dentro de cbc:Description
        var descriptions = doc.Descendants(Cbc + "Description");
        foreach (var desc in descriptions)
        {
            var cdata = desc.Value;
            if (!cdata.Contains("<Invoice") && !cdata.Contains("Invoice-2")) continue;
            try
            {
                var innerDoc = XDocument.Parse(cdata);
                if (innerDoc.Root?.Name.LocalName == "Invoice")
                    return innerDoc.Root;
            }
            catch { /* no es XML válido */ }
        }
        return null;
    }

    private static XElement? ExtraerApplicationResponse(XDocument doc)
    {
        if (doc.Root?.Name.LocalName == "ApplicationResponse")
            return doc.Root;

        var descriptions = doc.Descendants(Cbc + "Description");
        foreach (var desc in descriptions)
        {
            var cdata = desc.Value;
            if (!cdata.Contains("ApplicationResponse")) continue;
            try
            {
                var innerDoc = XDocument.Parse(cdata);
                if (innerDoc.Root?.Name.LocalName == "ApplicationResponse")
                    return innerDoc.Root;
            }
            catch { }
        }
        return null;
    }

    // ─── Validación de estructura ─────────────────────────────────────────────

    private static void ValidarEstructura(XElement inv, List<Hallazgo> h)
    {
        // CUFE obligatorio
        var cufe = inv.Element(Cbc + "UUID")?.Value;
        if (string.IsNullOrWhiteSpace(cufe))
            h.Add(Error("RVX010", "Invoice/UUID", "CUFE ausente"));
        else if (cufe.Length < 64)
            h.Add(Error("RVX011", "Invoice/UUID", $"CUFE con longitud inusual ({cufe.Length} chars). Se esperan 96 hex chars SHA384"));

        // Número de factura
        var id = inv.Element(Cbc + "ID")?.Value;
        if (string.IsNullOrWhiteSpace(id))
            h.Add(Error("RVX012", "Invoice/ID", "Número de factura ausente"));

        // CustomizationID debe contener "CUFE"
        var custId = inv.Element(Cbc + "CustomizationID")?.Value;
        if (string.IsNullOrWhiteSpace(custId) || !custId.Contains("CUFE"))
            h.Add(Advertencia("RVX013", "Invoice/CustomizationID", $"CustomizationID '{custId}' no contiene 'CUFE'. Verifique el perfil DIAN"));

        // Proveedor: NIT obligatorio
        var supplier = inv.Descendants(Cac + "AccountingSupplierParty").FirstOrDefault();
        if (supplier == null)
            h.Add(Error("RVX014", "AccountingSupplierParty", "Información del prestador ausente"));
        else
        {
            var nit = supplier.Descendants(Cbc + "CompanyID").FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(nit))
                h.Add(Error("RVX015", "AccountingSupplierParty/CompanyID", "NIT del prestador ausente"));
        }

        // Cliente: NIT/ID obligatorio
        var customer = inv.Descendants(Cac + "AccountingCustomerParty").FirstOrDefault();
        if (customer == null)
            h.Add(Error("RVX016", "AccountingCustomerParty", "Información del pagador (EPS/asegurador) ausente"));
        else
        {
            var nitCliente = customer.Descendants(Cbc + "CompanyID").FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(nitCliente))
                h.Add(Error("RVX017", "AccountingCustomerParty/CompanyID", "NIT del pagador ausente"));
        }

        // Moneda
        var moneda = inv.Element(Cbc + "DocumentCurrencyCode")?.Value;
        if (string.IsNullOrWhiteSpace(moneda))
            h.Add(Error("RVX018", "DocumentCurrencyCode", "Moneda no especificada"));
        else if (moneda != "COP")
            h.Add(Advertencia("RVX019", "DocumentCurrencyCode", $"Moneda '{moneda}' — se esperaba COP para facturas en Colombia"));
    }

    // ─── Validación de fechas ─────────────────────────────────────────────────

    private static void ValidarFechas(XElement inv, List<Hallazgo> h)
    {
        var issueDate = ParseFecha(inv.Element(Cbc + "IssueDate")?.Value);
        var dueDate = ParseFecha(inv.Element(Cbc + "DueDate")?.Value);

        if (issueDate == null)
            h.Add(Error("RVX020", "IssueDate", "Fecha de emisión ausente o inválida"));
        else if (issueDate > DateTime.Today)
            h.Add(Error("RVX021", "IssueDate", $"Fecha de emisión '{inv.Element(Cbc + "IssueDate")?.Value}' es futura"));

        if (issueDate != null && dueDate != null && dueDate < issueDate)
            h.Add(Error("RVX022", "DueDate", $"Fecha de vencimiento ({dueDate:yyyy-MM-dd}) es anterior a la fecha de emisión ({issueDate:yyyy-MM-dd})"));

        // Período de la factura
        var periodo = inv.Descendants(Cac + "InvoicePeriod").FirstOrDefault();
        if (periodo != null)
        {
            var periodoInicio = ParseFecha(periodo.Element(Cbc + "StartDate")?.Value);
            var periodoFin = ParseFecha(periodo.Element(Cbc + "EndDate")?.Value);

            if (periodoInicio != null && periodoFin != null && periodoFin < periodoInicio)
                h.Add(Error("RVX023", "InvoicePeriod", $"El período de la factura tiene fecha fin ({periodoFin:yyyy-MM-dd}) anterior a la fecha inicio ({periodoInicio:yyyy-MM-dd})"));
        }
    }

    // ─── Validación campos de salud (Interoperabilidad SISPRO) ────────────────

    private async Task ValidarSalud(XElement inv, List<Hallazgo> h)
    {
        // Buscar el bloque de interoperabilidad del sector salud
        var interop = inv.Descendants(Sts + "Interoperabilidad").FirstOrDefault();
        if (interop == null)
        {
            h.Add(Error("RVX030", "Interoperabilidad", "Bloque de interoperabilidad del sector salud ausente (sts:Interoperabilidad). Esta factura no tiene los campos requeridos para el sector salud."));
            return;
        }

        var campos = interop.Descendants(Sts + "AdditionalInformation")
            .ToDictionary(
                x => x.Element(Sts + "Name")?.Value?.Trim() ?? "",
                x => x.Element(Sts + "Value")
            );

        // Campos obligatorios de salud
        string[] obligatorios = { "CODIGO_PRESTADOR", "MODALIDAD_PAGO", "COBERTURA_PLAN_BENEFICIOS", "NUMERO_CONTRATO" };
        foreach (var campo in obligatorios)
        {
            if (!campos.ContainsKey(campo) || string.IsNullOrWhiteSpace(campos[campo]?.Value))
                h.Add(Error("RVX031", $"Interoperabilidad/{campo}", $"Campo obligatorio de salud ausente: {campo}"));
        }

        // Validar MODALIDAD_PAGO contra tabla SISPRO
        if (campos.TryGetValue("MODALIDAD_PAGO", out var modalidadEl))
        {
            var schemeId = modalidadEl?.Attribute("schemeID")?.Value;
            if (!string.IsNullOrWhiteSpace(schemeId))
            {
                var existe = await _db.ModalidadPago.AnyAsync(x => x.Codigo == schemeId && x.Habilitado);
                if (!existe)
                    h.Add(Error("RVX032", "Interoperabilidad/MODALIDAD_PAGO",
                        $"Código de modalidad de pago '{schemeId}' no existe en tabla SISPRO"));
            }
        }

        // Validar COBERTURA_PLAN_BENEFICIOS contra tabla SISPRO
        if (campos.TryGetValue("COBERTURA_PLAN_BENEFICIOS", out var coberturaEl))
        {
            var schemeId = coberturaEl?.Attribute("schemeID")?.Value;
            if (!string.IsNullOrWhiteSpace(schemeId))
            {
                var existe = await _db.CoberturaPlan.AnyAsync(x => x.Codigo == schemeId && x.Habilitado);
                if (!existe)
                    h.Add(Error("RVX033", "Interoperabilidad/COBERTURA_PLAN_BENEFICIOS",
                        $"Código de cobertura/plan '{schemeId}' no existe en tabla SISPRO"));
            }
        }

        // COPAGO y CUOTA_MODERADORA deben ser numéricos >= 0
        foreach (var campoPago in new[] { "COPAGO", "CUOTA_MODERADORA" })
        {
            if (campos.TryGetValue(campoPago, out var el) && !string.IsNullOrWhiteSpace(el?.Value))
            {
                if (!decimal.TryParse(el.Value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var val) || val < 0)
                    h.Add(Error("RVX034", $"Interoperabilidad/{campoPago}",
                        $"El valor de {campoPago} ('{el.Value}') debe ser un número mayor o igual a cero"));
            }
        }
    }

    // ─── Validación financiera ────────────────────────────────────────────────

    private static void ValidarFinanciero(XElement inv, List<Hallazgo> h)
    {
        var totales = inv.Descendants(Cac + "LegalMonetaryTotal").FirstOrDefault();
        if (totales == null)
        {
            h.Add(Error("RVX040", "LegalMonetaryTotal", "Sección de totales monetarios ausente"));
            return;
        }

        var lineExtension = ParseDecimal(totales.Element(Cbc + "LineExtensionAmount")?.Value);
        var taxExclusive = ParseDecimal(totales.Element(Cbc + "TaxExclusiveAmount")?.Value);
        var taxInclusive = ParseDecimal(totales.Element(Cbc + "TaxInclusiveAmount")?.Value);
        var prepaid = ParseDecimal(totales.Element(Cbc + "PrepaidAmount")?.Value) ?? 0;
        var payable = ParseDecimal(totales.Element(Cbc + "PayableAmount")?.Value);

        if (lineExtension == null) h.Add(Error("RVX041", "LegalMonetaryTotal/LineExtensionAmount", "Monto total de líneas ausente"));
        if (taxInclusive == null) h.Add(Error("RVX042", "LegalMonetaryTotal/TaxInclusiveAmount", "Monto total con impuestos ausente"));
        if (payable == null) h.Add(Error("RVX043", "LegalMonetaryTotal/PayableAmount", "Monto a pagar ausente"));

        // TaxInclusiveAmount debe ser >= LineExtensionAmount (por impuestos)
        if (lineExtension.HasValue && taxInclusive.HasValue && taxInclusive < lineExtension)
            h.Add(Error("RVX044", "LegalMonetaryTotal",
                $"TaxInclusiveAmount ({taxInclusive:N2}) no puede ser menor que LineExtensionAmount ({lineExtension:N2})"));

        // PayableAmount = TaxInclusiveAmount - PrepaidAmount (tolerancia 1 COP por redondeo)
        if (payable.HasValue && taxInclusive.HasValue)
        {
            var esperado = taxInclusive.Value - prepaid;
            if (Math.Abs(payable.Value - esperado) > 1)
                h.Add(Error("RVX045", "LegalMonetaryTotal/PayableAmount",
                    $"PayableAmount ({payable:N2}) ≠ TaxInclusiveAmount ({taxInclusive:N2}) - PrepaidAmount ({prepaid:N2}) = {esperado:N2}"));
        }

        // Montos negativos
        foreach (var (campo, valor) in new[]
        {
            ("LineExtensionAmount", lineExtension),
            ("TaxInclusiveAmount", taxInclusive),
            ("PayableAmount", payable)
        })
        {
            if (valor.HasValue && valor < 0)
                h.Add(Error("RVX046", $"LegalMonetaryTotal/{campo}", $"{campo} no puede ser negativo ({valor:N2})"));
        }
    }

    // ─── Validación de líneas ─────────────────────────────────────────────────

    private static void ValidarLineas(XElement inv, List<Hallazgo> h)
    {
        var lineas = inv.Descendants(Cac + "InvoiceLine").ToList();

        if (lineas.Count == 0)
        {
            h.Add(Error("RVX060", "InvoiceLine", "La factura no tiene líneas de detalle"));
            return;
        }

        // LineCountNumeric debe coincidir con el número real de líneas
        if (int.TryParse(inv.Element(Cbc + "LineCountNumeric")?.Value, out var lineCount) && lineCount != lineas.Count)
            h.Add(Advertencia("RVX061", "LineCountNumeric",
                $"LineCountNumeric ({lineCount}) no coincide con el número real de líneas ({lineas.Count})"));

        decimal sumaLineas = 0;

        for (int i = 0; i < lineas.Count; i++)
        {
            var linea = lineas[i];
            string p = $"InvoiceLine[{i + 1}]";

            var lineaId = linea.Element(Cbc + "ID")?.Value;
            var cantidad = ParseDecimal(linea.Element(Cbc + "InvoicedQuantity")?.Value);
            var lineAmount = ParseDecimal(linea.Element(Cbc + "LineExtensionAmount")?.Value);
            var precioUnit = ParseDecimal(linea.Descendants(Cbc + "PriceAmount").FirstOrDefault()?.Value);
            var descripcion = linea.Descendants(Cbc + "Description").FirstOrDefault()?.Value;

            if (string.IsNullOrWhiteSpace(descripcion))
                h.Add(Advertencia("RVX062", $"{p}/Item/Description", $"Línea {lineaId}: descripción del servicio/producto ausente"));

            if (cantidad.HasValue && cantidad <= 0)
                h.Add(Error("RVX063", $"{p}/InvoicedQuantity", $"Línea {lineaId}: la cantidad debe ser mayor que cero (valor: {cantidad})"));

            if (lineAmount.HasValue && lineAmount < 0)
                h.Add(Error("RVX064", $"{p}/LineExtensionAmount", $"Línea {lineaId}: el importe de línea no puede ser negativo ({lineAmount:N2})"));

            // Verificar: cantidad × precio = importe (tolerancia 1 COP)
            if (cantidad.HasValue && precioUnit.HasValue && lineAmount.HasValue)
            {
                var calculado = cantidad.Value * precioUnit.Value;
                if (Math.Abs(calculado - lineAmount.Value) > 1)
                    h.Add(Advertencia("RVX065", $"{p}/LineExtensionAmount",
                        $"Línea {lineaId}: {cantidad} × {precioUnit:N2} = {calculado:N2} ≠ LineExtensionAmount ({lineAmount:N2})"));
            }

            if (lineAmount.HasValue)
                sumaLineas += lineAmount.Value;
        }

        // Suma de líneas vs total declarado
        var totales = inv.Descendants(Cac + "LegalMonetaryTotal").FirstOrDefault();
        var totalDeclarado = ParseDecimal(totales?.Element(Cbc + "LineExtensionAmount")?.Value);
        if (totalDeclarado.HasValue && Math.Abs(sumaLineas - totalDeclarado.Value) > 1)
            h.Add(Error("RVX066", "LegalMonetaryTotal/LineExtensionAmount",
                $"La suma de importes de líneas ({sumaLineas:N2}) no coincide con el total declarado ({totalDeclarado:N2})"));
    }

    // ─── Validación estado DIAN ───────────────────────────────────────────────

    private static void ValidarEstadoDian(XElement appResponse, List<Hallazgo> h)
    {
        var responseCode = appResponse.Descendants(Cbc + "ResponseCode").FirstOrDefault()?.Value;
        var description = appResponse.Descendants(Cbc + "Description").FirstOrDefault()?.Value;

        if (responseCode == null)
        {
            h.Add(Advertencia("RVX051", "ApplicationResponse/ResponseCode", "No se pudo leer el código de respuesta DIAN"));
            return;
        }

        if (responseCode != "02")
            h.Add(Error("RVX052", "ApplicationResponse/ResponseCode",
                $"La factura NO está validada por DIAN. Código: '{responseCode}' — {description}. Solo facturas con código 02 pueden emitirse."));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static DateTime? ParseFecha(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        return DateTime.TryParseExact(valor, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var f) ? f : null;
    }

    private static decimal? ParseDecimal(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        return decimal.TryParse(valor, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static Hallazgo Error(string codigo, string campo, string descripcion) =>
        new() { Tipo = "ERROR", Codigo = codigo, Campo = campo, Descripcion = descripcion };

    private static Hallazgo Advertencia(string codigo, string campo, string descripcion) =>
        new() { Tipo = "ADVERTENCIA", Codigo = codigo, Campo = campo, Descripcion = descripcion };
}
