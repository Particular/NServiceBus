namespace NServiceBus.DelayedDelivery
{
    using NServiceBus.DeliveryConstraints;

    /// <summary>
    /// Base for the 2 flavours of delayed delivery
    /// </summary>
    public abstract class DelayedDeliveryConstraint : DeliveryConstraint
    {
    }
}