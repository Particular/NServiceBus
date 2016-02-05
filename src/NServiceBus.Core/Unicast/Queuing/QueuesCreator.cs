namespace NServiceBus
{
    using System.Threading.Tasks;
    using Installation;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using Transports;

    class QueuesCreator : INeedToInstallSomething
    {
        readonly IChildBuilder builder;
        readonly ReadOnlySettings settings;

        public QueuesCreator(IChildBuilder builder, ReadOnlySettings settings)
        {
            this.builder = builder;
            this.settings = settings;
        }

        public Task Install(string identity)
        {
            if (settings.Get<bool>("Endpoint.SendOnly"))
            {
                return TaskEx.CompletedTask;
            }
            if (!settings.CreateQueues())
            {
                return TaskEx.CompletedTask;
            }
            var queueCreator = builder.Build<ICreateQueues>();
            var queueBindings = settings.Get<QueueBindings>();

            return queueCreator.CreateQueueIfNecessary(queueBindings, identity);
        }
    }
}
