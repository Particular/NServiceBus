
namespace NServiceBus.Unicast.Queuing
{
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

        public void Install(string identity, Configure config)
        {
            if (config.Settings.Get<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            if (!config.CreateQueues())
            {
                return;
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
        }

        void CreateQueue(string identity, string transportAddress)
        {
            queueCreator.CreateQueueIfNecessary(transportAddress, identity);
            Logger.DebugFormat("Verified that the queue: [{0}] existed", transportAddress);
        }

        static ILog Logger = LogManager.GetLogger<QueuesCreator>();
    }
}
