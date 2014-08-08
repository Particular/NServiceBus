namespace NServiceBus
{
    using System;
    using Transports;

    /// <summary>
    /// Extension methods to configure transport.
    /// </summary>
    public static class UseTransportExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
// ReSharper disable UnusedParameter.Global
        [ObsoleteEx(Replacement = "Configure.With(c => c.UseTransport<T>(customizations", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure UseTransport<T>(this Configure config, Action<TransportConfiguration> customizations = null) where T : TransportDefinition
// ReSharper restore UnusedParameter.Global
        {
            throw new InvalidOperationException();
        }


        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
// ReSharper disable UnusedParameter.Global
        [ObsoleteEx(Replacement = "Configure.With(c => c.UseTransport(transportDefinitionType, customizations)", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure UseTransport(this Configure config, Type transportDefinitionType, Action<TransportConfiguration> customizations = null)
// ReSharper restore UnusedParameter.Global
        {
            throw new InvalidOperationException();
        }
    }
}