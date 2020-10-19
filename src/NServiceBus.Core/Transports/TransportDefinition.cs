using System;
using System.Threading.Tasks;

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
        public abstract Task<TransportInfrastructure> Initialize(Settings settings);


        /// <summary>
        /// </summary>
        public abstract string ToTransportAddress(EndpointAddress address);
    }

    /// <summary>
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// </summary>
        public Settings(string name, string hostDisplayName, StartupDiagnosticEntries startupDiagnostic, Action<string, Exception> criticalErrorAction, bool setupInfrastructure)
        {
            Name = name;
            HostDisplayName = hostDisplayName;
            StartupDiagnostic = startupDiagnostic;
            CriticalErrorAction = criticalErrorAction;
            SetupInfrastructure = setupInfrastructure;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// TODO: rethink the whole name properties. Which ones do we really need?
        /// </summary>
        public string HostDisplayName { get; }

        /// <summary>
        /// 
        /// </summary>
        public StartupDiagnosticEntries StartupDiagnostic { get; }

        /// <summary>
        /// 
        /// </summary>
        public Action<string, Exception> CriticalErrorAction { get; }

        /// <summary>
        /// 
        /// </summary>
        public bool SetupInfrastructure { get;  }
    }
}