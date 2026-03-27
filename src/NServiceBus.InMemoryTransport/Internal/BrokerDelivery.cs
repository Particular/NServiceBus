namespace NServiceBus;

enum BrokerDeliveryStatus
{
    Pending,
    Dispatched,
    Acknowledged,
    MovedToError
}

sealed class BrokerDelivery(BrokerEnvelope envelope, BrokerDeliveryStatus status)
{
    public BrokerEnvelope Envelope { get; } = envelope;
    public BrokerDeliveryStatus Status { get; private set; } = status;

    public void Acknowledge() => Status = BrokerDeliveryStatus.Acknowledged;
    public void MoveToError() => Status = BrokerDeliveryStatus.MovedToError;
    public void Dispatch() => Status = BrokerDeliveryStatus.Dispatched;
}
