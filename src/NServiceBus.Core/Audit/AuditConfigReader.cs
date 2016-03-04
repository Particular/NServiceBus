namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using Config;
    using Logging;
    using Settings;

    class AuditConfigReader
    {
        public static bool GetConfiguredAuditQueue(ReadOnlySettings settings, out Result result)
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
                Logger.Warn("Endpoint auditing is configured using the registry on this machine, ensure that either the Set-NServiceBusLocalMachineSettings cmdlet has been run on the target deployment machine or specify the QueueName attribute in the AuditConfig section in the app.config file. To quickly add the AuditConfig section to the app.config, in Package Manager Console type: add-NServiceBusAuditConfig.");
            }
            return queue;
        }

        static ILog Logger = LogManager.GetLogger<AuditConfigReader>();

        public class Result
        {
            public string Address;
            public TimeSpan? TimeToBeReceived;
        }
    }
}