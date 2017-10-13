namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class FakeQueueCreator : ICreateQueues
    {
        public FakeQueueCreator(Action action = null)
        {
            this.action = action;
        }

        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            action?.Invoke();
            return Task.FromResult(0);
        }

        Action action;
    }
}