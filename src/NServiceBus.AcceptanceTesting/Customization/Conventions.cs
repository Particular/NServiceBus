namespace NServiceBus.AcceptanceTesting.Customization
{
    using System;
    using Support;

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