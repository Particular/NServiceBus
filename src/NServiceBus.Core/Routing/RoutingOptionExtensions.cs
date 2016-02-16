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
        /// <param name="options">Option being extended.</param>
        /// <param name="destination">The destination address.</param>
        public static void SetDestination(this SendOptions options, string destination)
        {
            Guard.AgainstNull(nameof(options), options);
            Guard.AgainstNullAndEmpty(nameof(destination), destination);

            var state = options.Context.GetOrCreate<UnicastSendRouterConnector.State>();
            state.Option = UnicastSendRouterConnector.RouteOption.ExplicitDestination;
            state.ExplicitDestination = destination;
        }

        /// <summary>
        /// Allows the target endpoint instance for this reply to set. If not used the reply will be sent to the `ReplyToAddress` of the incoming message.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <param name="destination">The new target address.</param>
        public static void SetDestination(this ReplyOptions options, string destination)
        {
            Guard.AgainstNull(nameof(options), options);
            Guard.AgainstNullAndEmpty(nameof(destination), destination);

            options.Context.GetOrCreate<UnicastReplyRouterConnector.State>()
                .ExplicitDestination = destination;
        }

        /// <summary>
        /// Returns the destination configured by <see cref="SetDestination(ReplyOptions, string)"/>.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <returns>The specified destination address or <c>null</c> when no destination was specified.</returns>
        public static string GetDestination(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            UnicastReplyRouterConnector.State state;
            options.Context.TryGet(out state);
            return state?.ExplicitDestination;
        }

        /// <summary>
        /// Routes this message to any instance of this endpoint.
        /// </summary>
        /// <param name="options">Context being extended.</param>
        public static void RouteToThisEndpoint(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.GetOrCreate<UnicastSendRouterConnector.State>()
                .Option = UnicastSendRouterConnector.RouteOption.RouteToAnyInstanceOfThisEndpoint;
        }

        /// <summary>
        /// Routes this message to this endpoint instance.
        /// </summary>
        /// <param name="options">Context being extended.</param>
        public static void RouteToThisInstance(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.GetOrCreate<UnicastSendRouterConnector.State>()
                .Option = UnicastSendRouterConnector.RouteOption.RouteToThisInstance;
        }

        /// <summary>
        /// Routes this message to a specific instance of a destination endpoint.
        /// </summary>
        /// <param name="options">Context being extended.</param>
        /// <param name="instanceId">ID of destination instance.</param>
        public static void RouteToSpecificInstance(this SendOptions options, string instanceId)
        {
            Guard.AgainstNull(nameof(options), options);
            Guard.AgainstNull(nameof(instanceId), instanceId);

            var state = options.Context.GetOrCreate<UnicastSendRouterConnector.State>();
            state.Option = UnicastSendRouterConnector.RouteOption.RouteToSpecificInstance;
            state.SpecificInstance = instanceId;
        }

        /// <summary>
        /// Instructs the receiver to route the reply for this message to this instance.
        /// </summary>
        /// <param name="options">Context being extended.</param>
        public static void RouteReplyToThisInstance(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>()
                .Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToThisInstance;
        }
        
        /// <summary>
        /// Instructs the receiver to route the reply for this message to any instance of this endpoint.
        /// </summary>
        /// <param name="options">Context being extended.</param>
        public static void RouteReplyToAnyInstance(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>()
                .Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToAnyInstanceOfThisEndpoint;
        }

        /// <summary>
        /// Instructs the receiver to route the reply for this message to this instance.
        /// </summary>
        /// <param name="options">Context being extended.</param>
        public static void RouteReplyToThisInstance(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>()
                .Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToThisInstance;
        }
        
        /// <summary>
        /// Instructs the receiver to route the reply for this message to any instance of this endpoint.
        /// </summary>
        /// <param name="options">Context being extended.</param>
        public static void RouteReplyToAnyInstance(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>()
                .Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToAnyInstanceOfThisEndpoint;
        }

        /// <summary>
        /// Instructs the receiver to route the reply to specified address.
        /// </summary>
        /// <param name="options">Context being extended.</param>
        /// <param name="address">Reply destination.</param>
        public static void RouteReplyTo(this ReplyOptions options, string address)
        {
            Guard.AgainstNull(nameof(options), options);
            Guard.AgainstNull(nameof(address), address);

            var state = options.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>();
            state.Option = ApplyReplyToAddressBehavior.RouteOption.ExplicitReplyDestination;
            state.ExplicitDestination = address;
        }

        /// <summary>
        /// Instructs the receiver to route the reply to specified address.
        /// </summary>
        /// <param name="options">Context being extended.</param>
        /// <param name="address">Reply destination.</param>
        public static void RouteReplyTo(this SendOptions options, string address)
        {
            Guard.AgainstNull(nameof(options), options);
            Guard.AgainstNull(nameof(address), address);

            var state = options.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>();
            state.Option = ApplyReplyToAddressBehavior.RouteOption.ExplicitReplyDestination;
            state.ExplicitDestination = address;
        }
    }
}