namespace NServiceBus.Unicast.Queuing
{
    using System;
    using System.Linq;
    using Installation;
    using Installation.Environments;
    using Logging;
    using Settings;
    using Transports;
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
            if (SettingsHolder.Get<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            if (ConfigureQueueCreation.DontCreateQueues)
            {
                return;
            }

            var wantQueueCreatedInstances = Configure.Instance.Builder.BuildAll<IWantQueueCreated>().ToList();

            foreach (var wantQueueCreatedInstance in wantQueueCreatedInstances.Where(wantQueueCreatedInstance => !wantQueueCreatedInstance.IsDisabled))
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
        public void Init()
        {
            Configure.Instance.ForAllTypes<IWantQueueCreated>(type => Configure.Instance.Configurer.ConfigureComponent(type, DependencyLifecycle.InstancePerCall));
        }

        private readonly static ILog Logger = LogManager.GetLogger(typeof(QueuesCreator));
    }
}
