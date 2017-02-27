namespace NServiceBus.Transports.Msmq
{
    using System.Collections.Generic;
    using Routing;
    using Settings;

    /// <summary>
    /// Provides MSMQ transport specific routing extensions.
    /// </summary>
    public static class MsmqRoutingExtensions
    {
        /// <summary>
        /// Specify the machine name of a given logical endpoint.
        /// </summary>
        /// <param name="routingSettings">The settings to extend.</param>
        /// <param name="logicalEndpoint">The logical endpoint name.</param>
        /// <param name="machineName">The machine name of the logical endpoint.</param>
        public static void MapEndpointToMachine(this RoutingSettings<MsmqTransport> routingSettings, string logicalEndpoint, string machineName)
        {
            Guard.AgainstNull(nameof(routingSettings), routingSettings);
            Guard.AgainstNullAndEmpty(nameof(logicalEndpoint), logicalEndpoint);
            Guard.AgainstNullAndEmpty(nameof(machineName), machineName);

            List<EndpointInstance> mappings;
            if (!routingSettings.Settings.TryGet(machineMappingsSettingsKey, out mappings))
            {
                routingSettings.Settings.Set(machineMappingsSettingsKey, mappings = new List<EndpointInstance>());
            }

            mappings.Add(new EndpointInstance(logicalEndpoint).AtMachine(machineName));
        }

        internal static List<EndpointInstance> GetMachineMappings(this SettingsHolder settings)
        {
            return settings.GetOrDefault<List<EndpointInstance>>(machineMappingsSettingsKey) ?? new List<EndpointInstance>(0);
        }

        const string machineMappingsSettingsKey = "NServiceBus.Transport.MSMQ.MachineMappings";
    }
}