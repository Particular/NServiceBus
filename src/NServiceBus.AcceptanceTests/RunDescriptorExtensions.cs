namespace NServiceBus.AcceptanceTests
{
    using System;
    using NServiceBus.AcceptanceTesting.Support;

    public static class RunDescriptorExtensions
    {
        public static Type GetTransportType(this RunDescriptor runDescriptor)
        {
            var settings = runDescriptor.Settings;
            if (!settings.ContainsKey("Transport"))
            {
                settings = ScenarioDescriptors.Transports.Default.Settings;
            }

            return Type.GetType(settings["Transport"]);
        }
    }
}