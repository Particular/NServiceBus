namespace NServiceBus
{
    using DelayedDelivery;
    using Extensibility;

    /// <summary>
    /// Allows the users to control how the send is performed.
    /// </summary>
    /// <remarks>
    /// The behavior of this class is exposed via extension methods.
    /// </remarks>
    public class SendOptions : ExtendableOptions
    {
        internal DelayedDeliveryConstraint DelayedDeliveryConstraint { get; set; }
    }
}
