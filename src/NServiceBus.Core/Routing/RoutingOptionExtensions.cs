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

            var state = options.Context.GetOrCreate<UnicastSendRouter.State>();
            state.Option = UnicastSendRouter.RouteOption.ExplicitDestination;
            state.ExplicitDestination = destination;
        }

        /// <summary>
        /// Allows the target endpoint instance for this reply to set. If not used the reply will be sent to the `ReplyToAddress`
        /// of the incoming message.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <param name="destination">The new target address.</param>
        public static void SetDestination(this ReplyOptions options, string destination)
        {
            Guard.AgainstNull(nameof(options), options);
            Guard.AgainstNullAndEmpty(nameof(destination), destination);

            options.Context.GetOrCreate<ReplyConnector.State>()
                .ExplicitDestination = destination;
        }

        /// <summary>
        /// Returns the destination configured by <see cref="SetDestination(ReplyOptions, string)" />.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <returns>The specified destination address or <c>null</c> when no destination was specified.</returns>
        public static string GetDestination(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.TryGet(out ReplyConnector.State state);
            return state?.ExplicitDestination;
        }

        /// <summary>
        /// Returns the destination configured by <see cref="SetDestination(SendOptions, string)" />.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <returns>The specified destination address or <c>null</c> when no destination was specified.</returns>
        public static string GetDestination(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.TryGet(out UnicastSendRouter.State state);
            return state?.ExplicitDestination;
        }

        /// <summary>
        /// Routes this message to any instance of this endpoint.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static void RouteToThisEndpoint(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.GetOrCreate<UnicastSendRouter.State>()
                .Option = UnicastSendRouter.RouteOption.RouteToAnyInstanceOfThisEndpoint;
        }

        /// <summary>
        /// Returns whether the message should be routed to this endpoint.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <returns><c>true</c> when <see cref="RouteToThisEndpoint" /> has been called, <c>false</c> otherwise.</returns>
        public static bool IsRoutingToThisEndpoint(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            if (options.Context.TryGet(out UnicastSendRouter.State state))
            {
                return state.Option == UnicastSendRouter.RouteOption.RouteToAnyInstanceOfThisEndpoint;
            }

            return false;
        }

        /// <summary>
        /// Routes this message to this endpoint instance.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static void RouteToThisInstance(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.GetOrCreate<UnicastSendRouter.State>()
                .Option = UnicastSendRouter.RouteOption.RouteToThisInstance;
        }

        /// <summary>
        /// Returns whether the message should be routed to this endpoint instance.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <returns><c>true</c> when <see cref="IsRoutingToThisInstance" /> has been called, <c>false</c> otherwise.</returns>
        public static bool IsRoutingToThisInstance(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            if (options.Context.TryGet(out UnicastSendRouter.State state))
            {
                return state.Option == UnicastSendRouter.RouteOption.RouteToThisInstance;
            }

            return false;
        }

        /// <summary>
        /// Routes this message to a specific instance of a destination endpoint.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <param name="instanceId">ID of destination instance.</param>
        public static void RouteToSpecificInstance(this SendOptions options, string instanceId)
        {
            Guard.AgainstNull(nameof(options), options);
            Guard.AgainstNull(nameof(instanceId), instanceId);

            var state = options.Context.GetOrCreate<UnicastSendRouter.State>();
            state.Option = UnicastSendRouter.RouteOption.RouteToSpecificInstance;
            state.SpecificInstance = instanceId;
        }

        /// <summary>
        /// Returns the instance configured by <see cref="RouteToSpecificInstance" /> where the message should be routed to.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <returns>The configured instance ID or <c>null</c> when no instance was configured.</returns>
        public static string GetRouteToSpecificInstance(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            if (options.Context.TryGet(out UnicastSendRouter.State state) && state.Option == UnicastSendRouter.RouteOption.RouteToSpecificInstance)
            {
                return state.SpecificInstance;
            }

            return null;
        }

        /// <summary>
        /// Instructs the receiver to route the reply for this message to this instance.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static void RouteReplyToThisInstance(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>()
                .Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToThisInstance;
        }

        /// <summary>
        /// Indicates whether <see cref="RouteReplyToThisInstance(SendOptions)" /> has been called on this options.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static bool IsRoutingReplyToThisInstance(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            if (options.Context.TryGet(out ApplyReplyToAddressBehavior.State state))
            {
                return state.Option == ApplyReplyToAddressBehavior.RouteOption.RouteReplyToThisInstance;
            }

            return false;
        }

        /// <summary>
        /// Instructs the receiver to route the reply for this message to any instance of this endpoint.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static void RouteReplyToAnyInstance(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>()
                .Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToAnyInstanceOfThisEndpoint;
        }

        /// <summary>
        /// Indicates whether <see cref="RouteReplyToAnyInstance(SendOptions)" /> has been called on this options.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static bool IsRoutingReplyToAnyInstance(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            if (options.Context.TryGet(out ApplyReplyToAddressBehavior.State state))
            {
                return state.Option == ApplyReplyToAddressBehavior.RouteOption.RouteReplyToAnyInstanceOfThisEndpoint;
            }

            return false;
        }

        /// <summary>
        /// Instructs the receiver to route the reply for this message to this instance.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static void RouteReplyToThisInstance(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>()
                .Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToThisInstance;
        }

        /// <summary>
        /// Indicates whether <see cref="RouteReplyToThisInstance(ReplyOptions)" /> has been called on this options.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static bool IsRoutingReplyToThisInstance(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            if (options.Context.TryGet(out ApplyReplyToAddressBehavior.State state))
            {
                return state.Option == ApplyReplyToAddressBehavior.RouteOption.RouteReplyToThisInstance;
            }

            return false;
        }

        /// <summary>
        /// Instructs the receiver to route the reply for this message to any instance of this endpoint.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static void RouteReplyToAnyInstance(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>()
                .Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToAnyInstanceOfThisEndpoint;
        }

        /// <summary>
        /// Indicates whether <see cref="RouteReplyToAnyInstance(ReplyOptions)" /> has been called on this options.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static bool IsRoutingReplyToAnyInstance(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            if (options.Context.TryGet(out ApplyReplyToAddressBehavior.State state))
            {
                return state.Option == ApplyReplyToAddressBehavior.RouteOption.RouteReplyToAnyInstanceOfThisEndpoint;
            }

            return false;
        }

        /// <summary>
        /// Instructs the receiver to route the reply to specified address.
        /// </summary>
        /// <param name="options">Option being extended.</param>
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
        /// Returns the configured route by <see cref="RouteReplyTo(ReplyOptions,string)" />.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <returns>The configured reply to address or <c>null</c> when no address configured.</returns>
        public static string GetReplyToRoute(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            if (options.Context.TryGet(out ApplyReplyToAddressBehavior.State state) && state.Option == ApplyReplyToAddressBehavior.RouteOption.ExplicitReplyDestination)
            {
                return state.ExplicitDestination;
            }

            return null;
        }

        /// <summary>
        /// Instructs the receiver to route the reply to specified address.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <param name="address">Reply destination.</param>
        public static void RouteReplyTo(this SendOptions options, string address)
        {
            Guard.AgainstNull(nameof(options), options);
            Guard.AgainstNull(nameof(address), address);

            var state = options.Context.GetOrCreate<ApplyReplyToAddressBehavior.State>();
            state.Option = ApplyReplyToAddressBehavior.RouteOption.ExplicitReplyDestination;
            state.ExplicitDestination = address;
        }

        /// <summary>
        /// Returns the configured route by <see cref="RouteReplyTo(SendOptions,string)" />.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <returns>The configured reply to address or <c>null</c> when no address configured.</returns>
        public static string GetReplyToRoute(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            if (options.Context.TryGet(out ApplyReplyToAddressBehavior.State state) && state.Option == ApplyReplyToAddressBehavior.RouteOption.ExplicitReplyDestination)
            {
                return state.ExplicitDestination;
            }

            return null;
        }
    }
}