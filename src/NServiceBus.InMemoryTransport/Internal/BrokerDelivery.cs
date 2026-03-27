namespace NServiceBus;

enum BrokerDeliveryStatus
{
    Pending,
    Dispatched,
    Acknowledged,
    MovedToError
}

sealed class BrokerDelivery
{
    public BrokerDelivery(BrokerEnvelope envelope, BrokerDeliveryStatus status)
    {
        Envelope = envelope;
        Status = status;
    }

    public BrokerEnvelope Envelope { get; }
    public BrokerDeliveryStatus Status { get; private set; }

    public void Acknowledge() => Status = BrokerDeliveryStatus.Acknowledged;
    public void MoveToError() => Status = BrokerDeliveryStatus.MovedToError;
    public void Dispatch() => Status = BrokerDeliveryStatus.Dispatched;
}
