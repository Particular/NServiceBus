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
            Guard.AgainstNullAndEmpty(nameof(destination), destination);

            option.Context.GetOrCreate<UnicastSendRouterConnector.State>()
                .RouteExplicit(destination);
        }

        /// <summary>
        /// Allows the target endpoint instance for this reply to set. If not used the reply will be sent to the `ReplyToAddress` of the incoming message.
        /// </summary>
        /// <param name="option">Option being extended.</param>
        /// <param name="destination">The new target address.</param>
        public static void OverrideReplyToAddressOfIncomingMessage(this ReplyOptions option, string destination)
        {
            Guard.AgainstNullAndEmpty(nameof(destination), destination);

            option.Context.GetOrCreate<UnicastReplyRouterConnector.State>()
                .ExplicitDestination = destination;
        }

        /// <summary>
        /// Routes this message to the local endpoint instance.
        /// </summary>
        /// <param name="option">Context being extended.</param>
        public static void RouteToLocalEndpointInstance(this SendOptions option)
        {
            option.Context.GetOrCreate<UnicastSendRouterConnector.State>()
                .RouteLocalInstance();
        }

        /// <summary>
        /// Routes this message to the local satellite instance.
        /// </summary>
        /// <param name="option">Context being extended.</param>
        /// <param name="satellite">The satellite name.</param>
        public static void RouteToSatellite(this SendOptions option, string satellite)
        {
            var state = option.Context.GetOrCreate<UnicastSendRouterConnector.State>();
            state.RouteToSatellite(satellite);
        }
    }
}