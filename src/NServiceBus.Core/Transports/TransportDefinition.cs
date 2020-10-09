using System;

namespace NServiceBus.Transport
{
    /// <summary>
    /// Defines a transport.
    /// </summary>
    public abstract class TransportDefinition
    {
        /// <summary>
        /// Initializes all the factories and supported features for the transport. This method is called right before all features
        /// are activated and the settings will be locked down. This means you can use the SettingsHolder both for providing
        /// default capabilities as well as for initializing the transport's configuration based on those settings (the user cannot
        /// provide information anymore at this stage).
        /// </summary>
        public abstract TransportInfrastructure Initialize(TransportSettings settings);
    }

    /// <summary>
    /// TODO: rename to EndpointSettings, HostSettings, or something like that?
    /// </summary>
    public class TransportSettings
    {
        /// <summary>
        /// 
        /// </summary>
        public EndpointName EndpointName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public StartupDiagnosticEntries StartupDiagnostic { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Action<string, Exception> CriticalErrorAction { get; set; }

    }

    /// <summary>
    /// 
    /// </summary>
    public class EndpointName
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string HostDisplayName { get; set; }
    }
}