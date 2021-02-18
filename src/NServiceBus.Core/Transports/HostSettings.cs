namespace NServiceBus.Transport
{
    using System;
    using System.Threading;
    using Settings;

    /// <summary>
    /// Contains information about the hosting environment that is using the transport.
    /// </summary>
    public class HostSettings
    {
        /// <summary>
        /// Creates a new instance of <see cref="HostSettings"/>.
        /// </summary>
        public HostSettings(string name, string hostDisplayName, StartupDiagnosticEntries startupDiagnostic, Action<string, Exception, CancellationToken> criticalErrorAction, bool setupInfrastructure, ReadOnlySettings coreSettings = null)
        {
            Name = name;
            HostDisplayName = hostDisplayName;
            StartupDiagnostic = startupDiagnostic;
            CriticalErrorAction = criticalErrorAction;
            SetupInfrastructure = setupInfrastructure;
            CoreSettings = coreSettings;
        }

        /// <summary>
        /// Settings available only when running hosted in an NServiceBus endpoint; Otherwise, <c>null</c>.
        /// Transports can use these settings to validate the hosting endpoint settings.
        /// </summary>
        public ReadOnlySettings CoreSettings { get; }

        /// <summary>
        /// A name that describes the host (e.g. the endpoint name).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The name for the host as it should be shown on UIs.
        /// </summary>
        public string HostDisplayName { get; }

        /// <summary>
        /// A <see cref="StartupDiagnosticEntries"/> instance that can store diagnostic information about this transport.
        /// </summary>
        public StartupDiagnosticEntries StartupDiagnostic { get; }

        /// <summary>
        /// A callback to invoke when exception occur that can't be handled by the transport.
        /// </summary>
        public Action<string, Exception, CancellationToken> CriticalErrorAction { get; }

        /// <summary>
        /// A flag that indicates whether the transport should automatically setup necessary infrastructure.
        /// </summary>
        public bool SetupInfrastructure { get; }
    }
}
