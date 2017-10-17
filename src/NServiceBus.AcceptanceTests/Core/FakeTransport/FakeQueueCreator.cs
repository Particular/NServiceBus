namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Threading.Tasks;
    using Settings;
    using Transport;

    class FakeQueueCreator : ICreateQueues
    {
        public FakeQueueCreator(ReadOnlySettings settings)
        {
            this.settings = settings;
            onQueueCreation = settings.GetOrDefault<Action<QueueBindings>>("FakeTransport.onQueueCreation");
        }

        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            settings.Get<FakeTransport.StartUpSequence>().Add($"{nameof(ICreateQueues)}.{nameof(CreateQueueIfNecessary)}");

            onQueueCreation?.Invoke(queueBindings);
            return Task.FromResult(0);
        }

        ReadOnlySettings settings;
        Action<QueueBindings> onQueueCreation;
    }
}