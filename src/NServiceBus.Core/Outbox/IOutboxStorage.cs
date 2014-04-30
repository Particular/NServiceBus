namespace NServiceBus.Outbox
{
    public interface IOutboxStorage
    {
        OutboxMessage Get(string messageId);
        void Store(OutboxMessage outboxMessage);
        void SetAsDispatched(OutboxMessage outboxMessage);
    }
}