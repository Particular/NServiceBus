namespace NServiceBus.Sagas
{
    enum SagaMessageHandledBy
    {
        StartedByMessage,
        StartedByConsumedMessage,
        StartedByConsumedEvent,
        ConsumeTimeout,
        HandleTimeout,
        ConsumeEvent,
        ConsumeMessage,
        HandleMessage
    }
}