namespace NServiceBus.IntegrationTests
{
    using System;

    public static class Conventions
    {
        static Conventions()
        {
            EndpointNamingConvention = (t) => t.Name;
        }

        public static Func<Type, string> EndpointNamingConvention { get; set; }

        public static string DefaultNameFor<T>()
        {
            return EndpointNamingConvention(typeof(T));
        }
    }
}