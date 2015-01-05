namespace NServiceBus.AcceptanceTesting.Customization
{
    using System;
    using System.Collections.Generic;
    using Support;

    public class Conventions
    {
        static Conventions()
        {
            EndpointNamingConvention = t => t.Name;
        }

        public static Func<RunDescriptor> DefaultRunDescriptor = () => new RunDescriptor {Key = "Default"};

        public static Func<Type, string> EndpointNamingConvention { get; set; }

        public static Func<IDictionary<string, object>> DefaultDomainData { get; set; }
    }
}