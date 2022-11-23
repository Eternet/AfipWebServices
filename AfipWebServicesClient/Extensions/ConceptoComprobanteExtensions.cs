using System;

namespace AfipWebServicesClient.Extensions;

// ReSharper disable IdentifierTypo
public static class EnumsExtensions
{
    public static int ToInt<TValue>(this TValue value) where TValue : Enum
        => (int)(object)value;

    public static short ToShort<TValue>(this TValue value) where TValue : Enum
        => (short)(int)(object)value;
}