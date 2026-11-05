using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml;

namespace AfipWebServicesClient.TaxRegistration;

/// <summary>Reads the current getPersona_v2 XML, including impuesto/estadoImpuesto (WSCI 4.1).</summary>
public static class TaxRegistrationParser
{
    public static TaxRegistrationResult Parse(string xml, string requestedCuit, DateTime queriedAtUtc)
    {
        var cuit = TaxId.Normalize(requestedCuit);
        XDocument root;
        try { root = XDocument.Parse(xml); }
        catch (XmlException)
        {
            throw new TaxRegistrationException("invalid_response", "ARCA devolvió una respuesta que no se pudo interpretar.");
        }
        if (root.Descendants().Any(e => e.Name.LocalName == "Fault"))
            throw new TaxRegistrationException("padron_fault", "ARCA rechazó la consulta del padrón.");
        var person = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "personaReturn")
            ?? throw new TaxRegistrationException("no_data", "ARCA no devolvió datos de inscripción.");
        var general = person.Elements().FirstOrDefault(e => e.Name.LocalName == "datosGenerales");
        string? Value(XElement? node, string name) => node?.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value;
        if (general is null)
            throw new TaxRegistrationException("no_data", "CUIT inexistente o sin constancia disponible. Requiere revisión manual.");
        if (Value(general, "idPersona") != cuit)
            throw new TaxRegistrationException("identity_mismatch", "ARCA devolvió un CUIT diferente al solicitado.");
        if (Value(general, "estadoClave") != "ACTIVO" || Value(general, "tipoClave") != "CUIT")
            throw new TaxRegistrationException("inactive_cuit", "La clave no es un CUIT activo. Requiere revisión manual.");
        // Any partial business error prevents automatic classification; never turn no data into CF.
        if (person.Elements().Where(e => e.Name.LocalName.StartsWith("error", StringComparison.Ordinal))
            .Any(e => e.Descendants().Any(d => !string.IsNullOrWhiteSpace(d.Value))))
            throw new TaxRegistrationException("incomplete_registration", "La constancia contiene errores. Requiere revisión manual.");
        var active = person.Descendants().Where(e => e.Name.LocalName == "impuesto" && Value(e, "estadoImpuesto") == "AC")
            .Select(e => Value(e, "idImpuesto")).ToHashSet();
        var conditions = new[] { (Id: "30", Condition: VatCondition.RI), (Id: "20", Condition: VatCondition.MT), (Id: "32", Condition: VatCondition.EX) }
            .Where(x => active.Contains(x.Id)).Select(x => x.Condition).ToArray();
        if (conditions.Length != 1)
            throw new TaxRegistrationException("review_required", "No se pudo determinar una única condición de IVA vigente. No se presume consumidor final.");
        return new(cuit, conditions[0], queriedAtUtc, person.ToString(SaveOptions.DisableFormatting));
    }
}
