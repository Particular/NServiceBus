namespace NServiceBus.Unicast.Queuing
{
    using System;
    using System.Linq;
    using Installation;
    using Logging;
    using Transports;

    class QueuesCreator : INeedInitialization, INeedToInstallSomething
    {
        public ICreateQueues QueueCreator { get; set; }

        public void Install(string identity, Configure config)
        {
            if (config.Settings.Get<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            if (ConfigureQueueCreation.DontCreateQueues)
            {
                return;
            }

            var wantQueueCreatedInstances = config.Builder.BuildAll<IWantQueueCreated>().ToList();

            foreach (var wantQueueCreatedInstance in wantQueueCreatedInstances.Where(wantQueueCreatedInstance => wantQueueCreatedInstance.ShouldCreateQueue()))
            {
                if (wantQueueCreatedInstance.Address == null)
                {
                    throw new InvalidOperationException(string.Format("IWantQueueCreated implementation {0} returned a null address",wantQueueCreatedInstance.GetType().FullName));
                }

                QueueCreator.CreateQueueIfNecessary(wantQueueCreatedInstance.Address, identity);
                Logger.DebugFormat("Verified that the queue: [{0}] existed", wantQueueCreatedInstance.Address);
            }
        }

        public void Init(Configure config)
        {
            config.ForAllTypes<IWantQueueCreated>(type => config.Configurer.ConfigureComponent(type, DependencyLifecycle.InstancePerCall));
        }

        static ILog Logger = LogManager.GetLogger<QueuesCreator>();
    }
}
