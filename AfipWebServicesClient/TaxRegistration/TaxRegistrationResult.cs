using System;

namespace AfipWebServicesClient.TaxRegistration;

public enum VatCondition { RI, MT, EX }

public sealed record TaxRegistrationResult(
    string Cuit, VatCondition Condition, DateTime QueriedAtUtc, string OriginalResponse);

public sealed class TaxRegistrationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public static class TaxId
{
    public static string Normalize(string value)
    {
        value = (value ?? "").Trim().Replace("-", "");
        if (value.Length != 11 || !System.Linq.Enumerable.All(value, c => c is >= '0' and <= '9'))
            throw new TaxRegistrationException("invalid_cuit", "Ingresá un CUIT válido de once dígitos.");
        int[] weights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
        var sum = 0;
        for (var i = 0; i < 10; i++) sum += (value[i] - '0') * weights[i];
        var digit = 11 - sum % 11;
        if (digit == 11) digit = 0;
        if (digit == 10 || digit != value[10] - '0')
            throw new TaxRegistrationException("invalid_cuit", "El dígito verificador del CUIT no es válido.");
        return value;
    }
}
