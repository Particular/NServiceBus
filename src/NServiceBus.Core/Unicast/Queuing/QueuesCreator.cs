namespace NServiceBus.Unicast.Queuing
{
    using System.Threading.Tasks;
    using Installation;
    using Logging;
    using Transports;

    class QueuesCreator : INeedToInstallSomething
    {
        readonly ICreateQueues queueCreator;

        public QueuesCreator(ICreateQueues queueCreator)
        {
            this.queueCreator = queueCreator;
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

            var queueBindings = config.Settings.Get<QueueBindings>();

            foreach (var receiveLogicalAddress in queueBindings.ReceivingAddresses)
            {
                CreateQueue(identity, receiveLogicalAddress);
            }

            foreach (var sendingAddress in queueBindings.SendingAddresses)
            {
                CreateQueue(identity, sendingAddress);
            }

            return TaskEx.Completed;
        }

        void CreateQueue(string identity, string transportAddress)
        {
            queueCreator.CreateQueueIfNecessary(transportAddress, identity);
            Logger.DebugFormat("Verified that the queue: [{0}] existed", transportAddress);
        }

        static ILog Logger = LogManager.GetLogger<QueuesCreator>();
    }
}
