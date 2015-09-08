namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System.Threading.Tasks;
    using NServiceBus.Outbox;

    class FakeOutboxStorage : IOutboxStorage
    {
        public OutboxMessage ExistingMessage { get; set; }
        public OutboxMessage StoredMessage { get; set; }

        public bool WasDispatched { get; set; }

        public Task<OutboxMessage> Get(string messageId, OutboxStorageOptions options)
        {
            if (ExistingMessage != null && ExistingMessage.MessageId == messageId)
            {
                return Task.FromResult(ExistingMessage);
            }

            return Task.FromResult(default(OutboxMessage));
        }

        public Task Store(OutboxMessage message, OutboxStorageOptions options)
        {
            StoredMessage = message;
            return Task.FromResult(0);
        }

        public Task SetAsDispatched(string messageId, OutboxStorageOptions options)
        {
            WasDispatched = true;
            return Task.FromResult(0);
        }
    }
}
