using System;

namespace NServiceBus.Transport
{
    /// <summary>
    /// </summary>
    public class HostSettings
    {
        /// <summary>
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