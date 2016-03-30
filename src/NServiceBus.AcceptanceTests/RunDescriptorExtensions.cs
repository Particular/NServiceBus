namespace NServiceBus.AcceptanceTests
{
    using System;
    using AcceptanceTesting.Support;
    using ScenarioDescriptors;

    public static class RunDescriptorExtensions
    {
        public static Type GetTransportType(this RunDescriptor runDescriptor)
        {
            Type transportType;
            if (!runDescriptor.Settings.TryGet("Transport", out transportType))
            {
                return Transports.Default.Settings.Get<Type>("Transport");
            }

            return transportType;
        }
    }
}