namespace NServiceBus.Sagas
{
    using System;

    [Flags]
    enum SagaMessageHandledBy
    {
        StartedByMessage = 1,
        StartedByConsumedMessage = 2,
        StartedByConsumedEvent = 4,
        ConsumeTimeout = 8,
        HandleTimeout = 16,
        ConsumeEvent = 32,
        ConsumeMessage = 64,
        HandleMessage = 128
    }
}