namespace NServiceBus
{
    using NServiceBus.Extensibility;

    /// <summary>
    /// Gives users fine grained control over routing via extension methods.
    /// </summary>
    public static class RoutingOptionExtensions
    {
        /// <summary>
        /// </summary>
        public static void RouteTo(this SendOptions options, Destination destination)
        {
            Guard.AgainstNull(nameof(options), options);
            Guard.AgainstNull(nameof(destination), destination);

            options.GetExtensions().Set(destination);
        }

        /// <summary>
        /// </summary>
        public static Destination GetRouteTo(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            Destination destination;
            options.GetExtensions().TryGet(out destination);

            return destination;
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
        /// Indicates whether <see cref="RouteReplyToThisInstance(NServiceBus.SendOptions)"/> has been called on this options.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static bool IsRoutingReplyToThisInstance(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            ApplyReplyToAddressBehavior.State state;
            if (options.Context.TryGet(out state))
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
        /// Indicates whether <see cref="RouteReplyToAnyInstance(NServiceBus.SendOptions)"/> has been called on this options.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static bool IsRoutingReplyToAnyInstance(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            ApplyReplyToAddressBehavior.State state;
            if (options.Context.TryGet(out state))
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
        /// Indicates whether <see cref="RouteReplyToThisInstance(NServiceBus.ReplyOptions)"/> has been called on this options.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static bool IsRoutingReplyToThisInstance(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            ApplyReplyToAddressBehavior.State state;
            if (options.Context.TryGet(out state))
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
        /// Indicates whether <see cref="RouteReplyToAnyInstance(NServiceBus.ReplyOptions)"/> has been called on this options.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        public static bool IsRoutingReplyToAnyInstance(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            ApplyReplyToAddressBehavior.State state;
            if (options.Context.TryGet(out state))
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
        /// Returns the configured route by <see cref="RouteReplyTo(NServiceBus.ReplyOptions,string)"/>.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <returns>The configured reply to address or <c>null</c> when no address configured.</returns>
        public static string GetReplyToRoute(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            ApplyReplyToAddressBehavior.State state;
            if (options.Context.TryGet(out state) && state.Option == ApplyReplyToAddressBehavior.RouteOption.ExplicitReplyDestination)
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
        /// Returns the configured route by <see cref="RouteReplyTo(NServiceBus.SendOptions,string)"/>.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <returns>The configured reply to address or <c>null</c> when no address configured.</returns>
        public static string GetReplyToRoute(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            ApplyReplyToAddressBehavior.State state;
            if (options.Context.TryGet(out state) && state.Option == ApplyReplyToAddressBehavior.RouteOption.ExplicitReplyDestination)
            {
                return state.ExplicitDestination;
            }

            return null;
        }
    }
}