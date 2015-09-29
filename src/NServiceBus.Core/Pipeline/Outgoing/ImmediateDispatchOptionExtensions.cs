namespace NServiceBus
{
    using Extensibility;

    /// <summary>
    /// Provides ways for the end user to request immediate dispatch of their messages.
    /// </summary>
    public static class ImmediateDispatchOptionExtensions
    {
        /// <summary>
        /// Requests the messsage to be dispatched to the transport immediately. 
        /// This means that the message is acked by the transport as soon as the call to send returns. 
        /// The message will not be enlisted in any current receive transaction even if the transport support it.
        /// </summary>
        /// <param name="options">The options being extended.</param>
        public static void RequireImmediateDispatch(this ExtendableOptions options)
        {
            Guard.AgainstNull("options", options);

            options.GetExtensions().Set(new BatchOrImmediateDispatchConnector.State {ImmediateDispatch = true});
        }
    }
}