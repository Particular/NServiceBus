namespace NServiceBus.Unicast.Queuing
{
    using System.Threading.Tasks;
    using Installation;
    using Logging;
    using NServiceBus.ObjectBuilder;
    using Transports;

    class QueuesCreator : INeedToInstallSomething
    {
        IBuilder builder;

        public QueuesCreator(IBuilder builder)
        {
            this.builder = builder;
        }

        public Task InstallAsync(string identity, Configure config)
        {
            if (config.Settings.Get<bool>("Endpoint.SendOnly"))
            {
                return TaskEx.Completed;
            }
            if (!config.CreateQueues())
            {
                return TaskEx.Completed;
            }
            var queueCreator = builder.Build<ICreateQueues>();
            var queueBindings = config.Settings.Get<QueueBindings>();

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
