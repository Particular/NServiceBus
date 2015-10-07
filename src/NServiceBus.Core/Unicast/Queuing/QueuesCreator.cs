namespace NServiceBus.Unicast.Queuing
{
    using System.Threading.Tasks;
    using Installation;
    using Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using Transports;

    class QueuesCreator : IInstall
    {
        readonly IBuilder builder;
        readonly ReadOnlySettings settings;

        public QueuesCreator(IBuilder builder, ReadOnlySettings settings)
        {
            this.builder = builder;
            this.settings = settings;
        }

        public Task InstallAsync(string identity)
        {
            if (settings.Get<bool>("Endpoint.SendOnly"))
            {
                return TaskEx.Completed;
            }
            if (!settings.CreateQueues())
            {
                return TaskEx.Completed;
            }
            var queueCreator = builder.Build<ICreateQueues>();
            var queueBindings = settings.Get<QueueBindings>();

            foreach (var receiveLogicalAddress in queueBindings.ReceivingAddresses)
            {
                CreateQueue(queueCreator, identity, receiveLogicalAddress);
            }

            foreach (var sendingAddress in queueBindings.SendingAddresses)
            {
                CreateQueue(queueCreator, identity, sendingAddress);
            }

            return TaskEx.Completed;
        }

        void CreateQueue(ICreateQueues queueCreator, string identity, string transportAddress)
        {
            queueCreator.CreateQueueIfNecessary(transportAddress, identity);
            Logger.DebugFormat("Verified that the queue: [{0}] existed", transportAddress);
        }

        static ILog Logger = LogManager.GetLogger<QueuesCreator>();

    }
}
