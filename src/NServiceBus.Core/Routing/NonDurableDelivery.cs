namespace NServiceBus
{
    using DeliveryConstraints;

    /// <summary>
    /// Instructs the transport that it's allowed to transport the message with out the need to store it durable.
    /// </summary>
    public class NonDurableDelivery : DeliveryConstraint
    {
    }
}