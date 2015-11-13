namespace NServiceBus.Unicast.Queuing
{
    using System.Linq;
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

        public async Task InstallAsync(string identity)
        {
            if (settings.Get<bool>("Endpoint.SendOnly"))
            {
                return;
            }
            if (!settings.CreateQueues())
            {
                return;
            }
            var queueCreator = builder.Build<ICreateQueues>();
            var queueBindings = settings.Get<QueueBindings>();

            var receiveCreationTasks = queueBindings.ReceivingAddresses.Select(receiveLogicalAddress => CreateQueue(queueCreator, identity, receiveLogicalAddress)).ToList();
            await Task.WhenAll(receiveCreationTasks).ConfigureAwait(false);

            var sendingCreationTasks = queueBindings.SendingAddresses.Select(sendingAddress => CreateQueue(queueCreator, identity, sendingAddress)).ToList();
            await Task.WhenAll(sendingCreationTasks).ConfigureAwait(false);
        }

        static async Task CreateQueue(ICreateQueues queueCreator, string identity, string transportAddress)
        {
            await queueCreator.CreateQueueIfNecessary(transportAddress, identity).ConfigureAwait(false);
            Logger.DebugFormat("Verified that the queue: [{0}] existed", transportAddress);
        }

        static ILog Logger = LogManager.GetLogger<QueuesCreator>();

    }
}
