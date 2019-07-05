namespace NServiceBus
{
    using System.Threading.Tasks;
    using Transport;

    class IdleReceiveComponent : IReceiveComponent
    {
        public Task CreateQueuesIfNecessary(QueueBindings queueBindings, string username)
        {
            return TaskEx.CompletedTask;
        }

        public Task Initialize()
        {
            return TaskEx.CompletedTask;
        }

        public void Start()
        {
        }

        public Task Stop()
        {
            return TaskEx.CompletedTask;
        }

        public Task PerformPreStartupChecks()
        {
            return TaskEx.CompletedTask;
        }
    }
}