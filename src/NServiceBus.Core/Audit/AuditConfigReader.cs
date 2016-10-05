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
        /// The audit queue can be configured using 'EndpointConfiguration.AuditProcessedMessagesTo()',
        /// via the 'QueueName' attribute of the 'Audit' config section
        /// or by using the 'HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\ServiceBus\AuditQueue' registry key.
        /// </summary>
        /// <param name="settings">The configuration settings for the endpoint.</param>
        /// <param name="address">The configured audit queue address for the endpoint.</param>
        /// <returns>True if a configured audit address can be found, false otherwise.</returns>
        public static bool TryGetAuditQueueAddress(this ReadOnlySettings settings, out string address)
        {
            Guard.AgainstNull(nameof(settings), settings);

            Result result;
            if (!GetConfiguredAuditQueue(settings, out result))
            {
                address = null;
                return false;
            }

            address = result.Address;
            return true;
        }

        internal static bool GetConfiguredAuditQueue(ReadOnlySettings settings, out Result result)
        {
            if (settings.TryGet(out result))
            {
                return true;
            }

            var auditConfig = settings.GetConfigSection<AuditConfig>();
            string address;
            TimeSpan? timeToBeReceived = null;
            if (auditConfig == null)
            {
                address = ReadAuditQueueNameFromRegistry();
            }
            else
            {
                var ttbrOverride = auditConfig.OverrideTimeToBeReceived;

                if (ttbrOverride > TimeSpan.Zero)
                {
                    timeToBeReceived = ttbrOverride;
                }
                if (string.IsNullOrWhiteSpace(auditConfig.QueueName))
                {
                    address = ReadAuditQueueNameFromRegistry();
                }
                else
                {
                    address = auditConfig.QueueName;
                }
            }
            if (address == null)
            {
                result = null;
                return false;
            }
            result = new Result
            {
                Address = address,
                TimeToBeReceived = timeToBeReceived
            };
            return true;
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
