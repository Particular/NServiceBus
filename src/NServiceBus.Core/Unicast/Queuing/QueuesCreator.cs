namespace NServiceBus.Unicast.Queuing
{
    using System;
    using System.Linq;
    using Installation;
    using Logging;
    using Transports;

    /// <summary>
    /// Iterating over all implementers of IWantQueueCreated and creating queue for each.
    /// </summary>
    class QueuesCreator : INeedInitialization, INeedToInstallSomething
    {
        public ICreateQueues QueueCreator { get; set; }

        /// <summary>
        /// Performs the installation providing permission for the given user.
        /// </summary>
        /// <param name="identity">The user for under which the queue will be created.</param>
        /// <param name="config"></param>
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

            foreach (var wantQueueCreatedInstance in wantQueueCreatedInstances.Where(wantQueueCreatedInstance => wantQueueCreatedInstance.ShouldCreateQueue(config)))
            {
                if (wantQueueCreatedInstance.Address == null)
                {
                    throw new InvalidOperationException(string.Format("IWantQueueCreated implementation {0} returned a null address",wantQueueCreatedInstance.GetType().FullName));
                }

                QueueCreator.CreateQueueIfNecessary(wantQueueCreatedInstance.Address, identity);
                Logger.DebugFormat("Verified that the queue: [{0}] existed", wantQueueCreatedInstance.Address);
            }
        }

        /// <summary>
        /// Register all IWantQueueCreated implementers.
        /// </summary>
        public void Init(Configure config)
        {
            config.ForAllTypes<IWantQueueCreated>(type => config.Configurer.ConfigureComponent(type, DependencyLifecycle.InstancePerCall));
        }

        static ILog Logger = LogManager.GetLogger<QueuesCreator>();
    }
}
