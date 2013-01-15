namespace NServiceBus.Unicast.Queuing
{
    using System.Linq;
    using Installation;
    using Installation.Environments;
    using Logging;
    using NServiceBus.Config;
    using INeedInitialization = NServiceBus.INeedInitialization;

    /// <summary>
    /// Iterating over all implementers of IWantQueueCreated and creating queue for each.
    /// </summary>
    public class QueuesCreator : INeedInitialization, INeedToInstallSomething<Windows>
    {
        public ICreateQueues QueueCreator { get; set; }
        /// <summary>
        /// Performs the installation providing permission for the given user.
        /// </summary>
        /// <param name="identity">The user for under which the queue will be created.</param>
        public void Install(string identity)
        {
            if (Endpoint.IsSendOnly)
                return;

            if(MsmqTransportConfig.DoNotCreateQueues)
                return;

            var wantQueueCreatedInstances = Configure.Instance.Builder.BuildAll<IWantQueueCreated>().ToList();

            foreach (var wantQueueCreatedInstance in wantQueueCreatedInstances.Where(wantQueueCreatedInstance => !wantQueueCreatedInstance.IsDisabled))
            {
                QueueCreator.CreateQueueIfNecessary(wantQueueCreatedInstance.Address, identity);
                Logger.InfoFormat("Created queue: [{0}], for identity: [{1}]", wantQueueCreatedInstance.Address, identity);
            }
        }

        /// <summary>
        /// Register all IWantQueueCreated implementers.
        /// </summary>
        public void Init()
        {
            Configure.Instance.ForAllTypes<IWantQueueCreated>(type => Configure.Instance.Configurer.ConfigureComponent(type, DependencyLifecycle.InstancePerCall));
        }

        private readonly static ILog Logger = LogManager.GetLogger(typeof(QueuesCreator));
    }
}
