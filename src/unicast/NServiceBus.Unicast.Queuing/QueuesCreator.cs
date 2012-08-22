namespace NServiceBus.Unicast.Queuing
{
    using System.Linq;
    using System.Security.Principal;
    using Config;
    using Installation;
    using Installation.Environments;
    using Logging;
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
        public void Install(WindowsIdentity identity)
        {
            if (Endpoint.IsSendOnly)
                return;

            var wantQueueCreatedInstances = Configure.Instance.Builder.BuildAll<IWantQueueCreated>();

            foreach (var wantQueueCreatedInstance in wantQueueCreatedInstances.Where(wantQueueCreatedInstance => !wantQueueCreatedInstance.IsDisabled))
            {
                QueueCreator.CreateQueueIfNecessary(wantQueueCreatedInstance.Address, identity.Name);
                Logger.InfoFormat("Created queue: [{0}], for identity: [{1}]", wantQueueCreatedInstance.Address, identity.Name);
            }
        }

        /// <summary>
        /// Register all IWantQueueCreated's implementers
        /// </summary>
        public void Init()
        {
            Configure.Instance.ForAllTypes<IWantQueueCreated>(type => Configure.Instance.Configurer.ConfigureComponent(type, DependencyLifecycle.InstancePerCall));
        }

        private readonly static ILog Logger = LogManager.GetLogger(typeof(QueuesCreator));
    }
}
