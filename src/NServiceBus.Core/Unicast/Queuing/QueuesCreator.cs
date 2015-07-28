
namespace NServiceBus.Unicast.Queuing
{
    using System.Linq;
    using Installation;
    using Logging;
    using Transports;

    class QueuesCreator : INeedToInstallSomething
    {
        public ICreateQueues QueueCreator { get; set; }

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
            var boundQueueAddresses = queueBindings.ReceivingAddresses.Concat(queueBindings.SendingAddresses);

            foreach (var address in boundQueueAddresses)
            {
                QueueCreator.CreateQueueIfNecessary(address, identity);
                Logger.DebugFormat("Verified that the queue: [{0}] existed", address);
            }
        }

        static ILog Logger = LogManager.GetLogger<QueuesCreator>();
    }
}
