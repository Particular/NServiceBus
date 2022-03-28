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
        /// <inheritdoc />
        public SendOptions()
        {
            Context.GetOrCreate<UnicastSendRouter.State>();
            Context.GetOrCreate<RoutingToDispatchConnector.State>(); // this needs to be done for all message options!
            Context.GetOrCreate<ApplyReplyToAddressBehavior.State>();
        }

        internal DelayedDeliveryConstraint DelayedDeliveryConstraint { get; set; }
    }
}