namespace NServiceBus
{
    /// <summary>
    /// Gives users fine grained control over routing via extension methods.
    /// </summary>
    public static class RoutingOptionExtensions
    {
        /// <summary>
        /// Allows a specific physical address to be used to route this message.
        /// </summary>
        /// <param name="option">Option being extended.</param>
        /// <param name="destination">The destination address.</param>
        public static void SetDestination(this SendOptions option, string destination)
        {
            Guard.AgainstNullAndEmpty("destination", destination);

            option.Context.GetOrCreate<DetermineRouteForSendBehavior.State>()
                .ExplicitDestination = destination;
        }

        /// <summary>
        /// Allows the target endpoint instance for this reply to set. If not used the reply will be sent to the `ReplyToAddress` of the incoming message.
        /// </summary>
        /// <param name="option">Option being extended.</param>
        /// <param name="destination">The new target address.</param>
        public static void OverrideReplyToAddressOfIncomingMessage(this ReplyOptions option, string destination)
        {
            Guard.AgainstNullAndEmpty("destination", destination);

            option.Context.GetOrCreate<DetermineRouteForReplyBehavior.State>()
                .ExplicitDestination = destination;
        }

        /// <summary>
        /// Routes this message to the local endpoint instance.
        /// </summary>
        /// <param name="option">Context being extended.</param>
        public static void RouteToLocalEndpointInstance(this SendOptions option)
        {
            option.Context.GetOrCreate<DetermineRouteForSendBehavior.State>()
                .RouteToLocalInstance = true;
        }
    }
}