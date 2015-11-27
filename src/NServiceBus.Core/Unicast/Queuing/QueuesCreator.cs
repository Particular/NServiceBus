namespace NServiceBus.Unicast.Queuing
{
    using System.Threading.Tasks;
    using Installation;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using Transports;

    class QueuesCreator : INeedToInstallSomething
    {
        readonly IBuilder builder;
        readonly ReadOnlySettings settings;

        public QueuesCreator(IBuilder builder, ReadOnlySettings settings)
        {
            this.builder = builder;
            this.settings = settings;
        }

        public Task Install(string identity)
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

            return queueCreator.CreateQueueIfNecessary(queueBindings, identity);
        }
    }
}
