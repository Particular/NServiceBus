namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System.Threading.Tasks;
    using Transport;

    class FakeQueueCreator : ICreateQueues
    {
        public FakeQueueCreator(FakeTransport settings)
        {
            this.settings = settings;
        }

        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            settings.StartUpSequence.Add($"{nameof(ICreateQueues)}.{nameof(CreateQueueIfNecessary)}");

            settings.OnQueueCreation?.Invoke(queueBindings);
            return Task.FromResult(0);
        }

        FakeTransport settings;
    }
}