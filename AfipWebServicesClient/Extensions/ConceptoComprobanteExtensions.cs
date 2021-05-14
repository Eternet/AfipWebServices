using System;
using AfipWebServicesClient.Model;

namespace AfipWebServicesClient.Extensions
{
    // ReSharper disable IdentifierTypo
    public static class EnumsExtensions
    {
        public static int ToInt<TValue>(this TValue value) where TValue : Enum
            => (int)(object)value;
    }
}