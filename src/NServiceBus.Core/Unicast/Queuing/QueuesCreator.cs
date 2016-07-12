namespace NServiceBus
{
    using System.Threading.Tasks;
    using Installation;
    using ObjectBuilder;
    using Settings;
    using Transport;

    class QueuesCreator : INeedToInstallSomething
    {
        public QueuesCreator(IBuilder builder, ReadOnlySettings settings)
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

        readonly IBuilder builder;
        readonly ReadOnlySettings settings;
    }
}