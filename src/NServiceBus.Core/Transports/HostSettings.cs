namespace NServiceBus.Transport
{
    using System;

    /// <summary>
    /// Contains information about the hosting environment that is using the transport.
    /// </summary>
    public class HostSettings
    {
        /// <summary>
        /// Creates a new instance of <see cref="HostSettings"/>.
        /// </summary>
        public HostSettings(string name, string hostDisplayName, StartupDiagnosticEntries startupDiagnostic, Action<string, Exception> criticalErrorAction, bool setupInfrastructure)
        {
            Name = name;
            HostDisplayName = hostDisplayName;
            StartupDiagnostic = startupDiagnostic;
            CriticalErrorAction = criticalErrorAction;
            SetupInfrastructure = setupInfrastructure;
        }

        /// <summary>
        /// 
        /// A name that describes the host (e.g. the endpoint name).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// TODO: rethink the whole name properties. Which ones do we really need?
        /// The name for the host as it should be shown on UIs.
        /// </summary>
        // TODO: rethink the whole name properties. Which ones do we really need?
        public string HostDisplayName { get; }

        /// <summary>
        /// 
        /// A <see cref="StartupDiagnosticEntries"/> instance that can store diagnostic information about this transport.
        /// </summary>
        public StartupDiagnosticEntries StartupDiagnostic { get; }

        /// <summary>
        /// 
        /// A callback to invoke when exception occur that can't be handled by the transport.
        /// </summary>
        public Action<string, Exception> CriticalErrorAction { get; }

        /// <summary>
        /// 
        /// A flag that indicates whether the transport should automatically setup necessary infrastructure.
        /// </summary>
        public bool SetupInfrastructure { get; }
    }
}