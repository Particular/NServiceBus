namespace NServiceBus
{
    using System.Threading.Tasks;
    using Transport;

    class DevelopmentTransportQueueCreator : ICreateQueues
    {
        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            //no-op
            return TaskEx.CompletedTask;
        }
    }
}