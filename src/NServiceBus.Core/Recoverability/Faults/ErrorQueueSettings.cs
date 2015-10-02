namespace NServiceBus.Faults
{
    using System;
    using NServiceBus.Config;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using NServiceBus.Utils;

    static class ErrorQueueSettings
    {
        public static string GetConfiguredErrorQueue(ReadOnlySettings settings)
        {
            string errorQueue;

            if (settings.TryGet("errorQueue", out errorQueue))
            {
                Logger.Debug("Error queue retrieved from code configuration via 'BusConfiguration.ErrorQueue().");
                return errorQueue;
            }

            var section = settings.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
            if (section != null)
            {
                if (!string.IsNullOrWhiteSpace(section.ErrorQueue))
                {
                    Logger.Debug("Error queue retrieved from <MessageForwardingInCaseOfFaultConfig> element in config file.");
                    return section.ErrorQueue;
                }

                throw new Exception(
                    @"'MessageForwardingInCaseOfFaultConfig' configuration section is found but 'ErrorQueue' value is empty. 
Please take on of the following actions: 
- set the error queue at configuration time using 'BusConfiguration.SendFailedMessagesTo()'
- Add a valid value to to your app config. For example: 
 <MessageForwardingInCaseOfFaultConfig ErrorQueue=""error""/>");
            }

            var registryErrorQueue = RegistryReader.Read("ErrorQueue");
            if (registryErrorQueue != null)
            {
                if (!string.IsNullOrWhiteSpace(registryErrorQueue))
                {
                    Logger.Debug("Error queue retrieved from registry settings.");
                    return registryErrorQueue;
                }
                throw new Exception(
                    @"'ErrorQueue' read from registry but the value is empty. 
Please take on of the following actions: 
- set the error queue at configuration time using 'BusConfiguration.SendFailedMessagesTo()'
- add a 'MessageForwardingInCaseOfFaultConfig' section to your app.config
- give 'HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\ServiceBus\ErrorQueue' a valid value for your error queue");
            }

            throw new Exception(
@"Faults forwarding requires an error queue to be specified. 
Please take on of the following actions: 
- set the error queue at configuration time using 'BusConfiguration.SendFailedMessagesTo()'
- add a 'MessageForwardingInCaseOfFaultConfig' section to your app.config
- configure a global error queue in the registry using the powershell command: Set-NServiceBusLocalMachineSettings -ErrorQueue {address of error queue}");
        }

        static ILog Logger = LogManager.GetLogger(typeof(ErrorQueueSettings));
    }
}