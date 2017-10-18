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
            onQueueCreation = settings.GetOrDefault<Action>("FakeTransport.QueueCreatorAction");
        }

        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            settings.Get<FakeTransport.StartUpSequence>().Add($"{nameof(ICreateQueues)}.{nameof(CreateQueueIfNecessary)}");

            onQueueCreation?.Invoke();
            return Task.FromResult(0);
        }

        ReadOnlySettings settings;
        Action onQueueCreation;
    }
}