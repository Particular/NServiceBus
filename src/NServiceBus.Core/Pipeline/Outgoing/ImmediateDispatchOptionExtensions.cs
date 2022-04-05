namespace NServiceBus
{
    using Extensibility;

    /// <summary>
    /// Provides ways to request immediate dispatch of messages.
    /// </summary>
    public static class ImmediateDispatchOptionExtensions
    {
        /// <summary>
        /// Requests that the message be dispatched to the transport immediately.
        /// This means that the message is ACKed by the transport as soon as the call to send returns.
        /// The message will not be enlisted in any current receive transaction even if the transport support it.
        /// </summary>
        /// <param name="options">The options being extended.</param>
        public static void RequireImmediateDispatch(this ExtendableOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.MessageOperationContext.Set(new RoutingToDispatchConnector.State
            {
                ImmediateDispatch = true
            });
        }

        /// <summary>
        /// Returns whether immediate dispatch has been requested by <see cref="RequireImmediateDispatch" /> or not.
        /// </summary>
        /// <param name="options">The options being extended.</param>
        /// <returns><c>True</c> if immediate dispatch was requested, <c>False</c> otherwise.</returns>
        public static bool RequiredImmediateDispatch(this ExtendableOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            options.MessageOperationContext.TryGet(out RoutingToDispatchConnector.State state);

            return state?.ImmediateDispatch ?? false;
        }
    }
}