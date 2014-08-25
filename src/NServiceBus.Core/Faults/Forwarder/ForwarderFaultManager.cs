namespace NServiceBus.Faults.Forwarder
{
    using System.Configuration;
    using NServiceBus.Config;
    using NServiceBus.Faults.Forwarder.Config;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Utils;

    class ForwarderFaultManager : Feature
    {
        public ForwarderFaultManager()
        {
            EnableByDefault();
            Prerequisite(c => !c.Container.HasComponent<IManageMessageFailures>(), "An IManageMessageFailures implementation is already registered.");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            string errorQueue = null;

            var section = context.Settings.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
            if (section != null)
            {
                if (string.IsNullOrWhiteSpace(section.ErrorQueue))
                {
                    throw new ConfigurationErrorsException(
                        "'MessageForwardingInCaseOfFaultConfig' configuration section is found but 'ErrorQueue' value is missing." +
                        "\n The following is an example for adding such a value to your app config: " +
                        "\n <MessageForwardingInCaseOfFaultConfig ErrorQueue=\"error\"/> \n");
                }

                Logger.Debug("Error queue retrieved from <MessageForwardingInCaseOfFaultConfig> element in config file.");

                errorQueue = section.ErrorQueue;
            }
            else
            {
                var registryErrorQueue = RegistryReader.Read("ErrorQueue");
                if (!string.IsNullOrWhiteSpace(registryErrorQueue))
                {
                    Logger.Debug("Error queue retrieved from registry settings.");
                    errorQueue = registryErrorQueue;
                }
            }
            
            if (errorQueue == null)
            {
                throw new ConfigurationErrorsException("Faults forwarding requires an error queue to be specified. Please add a 'MessageForwardingInCaseOfFaultConfig' section to your app.config" +
                "\n or configure a global one using the powershell command: Set-NServiceBusLocalMachineSettings -ErrorQueue {address of error queue}");
            }

            context.Container.ConfigureComponent<FaultManager>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(fm => fm.ErrorQueue, errorQueue);
            context.Container.ConfigureComponent<FaultsQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.Enabled, true)
                .ConfigureProperty(t => t.ErrorQueue, errorQueue);
        }

        static ILog Logger = LogManager.GetLogger(typeof(ForwarderFaultManager));
    }
}