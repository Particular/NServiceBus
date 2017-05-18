namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using Config;
    using Logging;
    using Settings;

    /// <summary>
    /// Utility class to find the configured audit queue for an endpoint.
    /// </summary>
    public static class AuditConfigReader
    {
        /// <summary>
        /// Finds the configured audit queue for an endpoint.
        /// The audit queue can be configured by using 'EndpointConfiguration.AuditProcessedMessagesTo()'
        /// or by using the 'HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\ServiceBus\AuditQueue' registry key.
        /// </summary>
        /// <param name="settings">The configuration settings for the endpoint.</param>
        /// <param name="address">The configured audit queue address for the endpoint.</param>
        /// <returns>True if a configured audit address can be found, false otherwise.</returns>
        public static bool TryGetAuditQueueAddress(this ReadOnlySettings settings, out string address)
        {
            Guard.AgainstNull(nameof(settings), settings);

            var result = GetConfiguredAuditQueue(settings);
            if (result == null)
            {
                address = null;
                return false;
            }

            address = result.Address;
            return true;
        }

        internal static Result GetConfiguredAuditQueue(ReadOnlySettings settings)
        {
            if (settings.TryGet(out Result configResult))
            {
                return configResult;
            }

            var address = ReadAuditQueueNameFromRegistry();

            if (address == null)
            {
                return null;
            }

            return new Result
            {
                Address = address,
                TimeToBeReceived = null
            };
        }

        static string ReadAuditQueueNameFromRegistry()
        {
            var queue = RegistryReader.Read("AuditQueue");
            if (string.IsNullOrWhiteSpace(queue))
            {
                return null;
            }
            // If Audit feature is enabled and the value not specified via config and instead specified in the registry:
            // Log a warning when running in the debugger to remind user to make sure the
            // production machine will need to have the required registry setting.
            if (Debugger.IsAttached)
            {
                Logger.Warn("Endpoint auditing is configured using the registry on this machine, see Particular Documentation for details on how to address this with your version of NServiceBus.");
            }
            return queue;
        }

        static ILog Logger = LogManager.GetLogger(typeof(AuditConfigReader));

        internal class Result
        {
            public string Address;
            public TimeSpan? TimeToBeReceived;
        }
    }
}
