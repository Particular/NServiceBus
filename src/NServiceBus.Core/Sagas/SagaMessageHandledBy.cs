namespace NServiceBus.Sagas
{
    enum SagaMessageHandledBy
    {
        StartedByMessage,
        StartedByCommand,
        StartedByEvent,
        ProcessTimeout,
        HandleTimeout,
        ProcessEvent,
        ProcessCommand,
        HandleMessage
    }
}