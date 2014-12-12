using System.Configuration;
using Common.Logging;
using NServiceBus.Config;
using NServiceBus.Faults;
using NServiceBus.Faults.Forwarder;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure
    /// </summary>
    public static class ConfigureFaultsForwarder
    {
        /// <summary>
        /// Forward messages that have repeatedly failed to another endpoint.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure MessageForwardingInCaseOfFault(this Configure config)
        {
            ErrorQueue = config.GetConfiguredErrorQueue();
       
            config.Configurer.ConfigureComponent<FaultManager>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(fm => fm.ErrorQueue, ErrorQueue);

            return config;
        }

        /// <summary>
        /// The queue to which to forward errors.
        /// </summary>
        public static Address ErrorQueue { get; set; }

    }

    class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            if (!Configure.SendOnlyMode && !Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                Configure.Instance.MessageForwardingInCaseOfFault();
        }
    }
}
