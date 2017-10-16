namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class FakeQueueCreator : ICreateQueues
    {
        public FakeQueueCreator(Action onQueueCreation = null)
        {
            this.onQueueCreation = onQueueCreation;
        }

        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            onQueueCreation?.Invoke();
            return Task.FromResult(0);
        }

        Action onQueueCreation;
    }
}