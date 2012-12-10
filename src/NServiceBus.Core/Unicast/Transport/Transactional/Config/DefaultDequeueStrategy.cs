namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    using System;
    using DequeueStrategies;
    using Queuing;

    class DefaultDequeueStrategy : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (Configure.Instance.Configurer.HasComponent<IDequeueMessages>())
                return;

            if (!Configure.Instance.Configurer.HasComponent<IReceiveMessages>())
                throw new InvalidOperationException("No message receiver has been specified. Either configure one or add your own DequeueStrategy");

            Configure.Instance.Configurer.ConfigureComponent<PollingDequeueStrategy>(DependencyLifecycle.InstancePerCall);
        }
    }
}