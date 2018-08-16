namespace NServiceBus
{
    using DelayedDelivery;
    using Extensibility;

    /// <summary>
    /// Allows the users to control how the send is performed.
    /// </summary>
    public class SendOptions : ExtendableOptions
    {
        internal DelayedDeliveryConstraint DelayedDeliveryConstraint { get; set; }
    }
}