namespace NServiceBus.AcceptanceTesting.Customization
{
    using System;
    using System.Collections.Generic;
    using Support;

    public static class BusConfigExtensions
    {
        public static void TypesToIncludeInScan(this BusConfiguration config, IEnumerable<Type> typesToScan)
        {
            config.TypesToScanInternal(typesToScan);
        }
    }

    public class Conventions
    {
        static Conventions()
        {
            EndpointNamingConvention = t => t.Name;
        }

        public static Func<RunDescriptor> DefaultRunDescriptor = () => new RunDescriptor {Key = "Default"};

        public static Func<Type, string> EndpointNamingConvention { get; set; }
    }
}