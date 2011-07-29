using System;

namespace NServiceBus.Licensing.Extensions
{
    public static class StringExtensions
    {
        public static T CastWithDefault<T>(this string value, T defaultValue)
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}