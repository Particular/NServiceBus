namespace NServiceBus.Features
{
    using NServiceBus.Faults;
    using NServiceBus.Faults.Forwarder;
    using NServiceBus.Faults.Forwarder.Config;

    class ForwarderFaultManager : Feature
    {
        internal ForwarderFaultManager()
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

            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(context.Settings);

            context.Container.ConfigureComponent<FaultManager>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(fm => fm.ErrorQueue, errorQueue);
            context.Container.ConfigureComponent<FaultsQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.Enabled, true)
                .ConfigureProperty(t => t.ErrorQueue, errorQueue);
        }


    }
}